using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

namespace WorkStorage.Controllers;

/// <summary>
/// Provides AWS S3-compatible REST API endpoints backed by local file system storage.
/// Authentication is not enforced.
/// Object metadata (Content-Type, x-amz-meta-*, tags) is persisted as JSON sidecar
/// files under {BasePath}/.meta/{bucket}/{key}.json.
/// Bucket tags are stored under {BasePath}/.meta/.buckets/{bucket}.json.
/// </summary>
[ApiController]
public sealed class S3Controller : ControllerBase
{
    private static readonly XNamespace S3Ns = "http://s3.amazonaws.com/doc/2006-03-01/";
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    private readonly string _basePath;
    private readonly string _multipartPath;
    private readonly string _metaBasePath;

    public S3Controller(IConfiguration configuration)
    {
        var configured = configuration["Storage:BasePath"]
            ?? throw new InvalidOperationException("Storage:BasePath is not configured.");
        _basePath = Path.GetFullPath(configured);
        _multipartPath = Path.Combine(_basePath, ".multipart");
        _metaBasePath = Path.Combine(_basePath, ".meta");
    }

    // ================================================================
    //  Metadata model
    // ================================================================

    private sealed class ObjectMetadata
    {
        public string ContentType { get; init; } = "application/octet-stream";
        public Dictionary<string, string> UserMetadata { get; init; } = [];
        public Dictionary<string, string> Tags { get; init; } = [];
    }

    // ================================================================
    //  Bucket operations
    // ================================================================

    /// <summary>
    /// GET / – List all buckets.
    /// </summary>
    [HttpGet("/")]
    public IActionResult ListBuckets()
    {
        EnsureBaseDirectory();

        var buckets = Directory.GetDirectories(_basePath)
            .Select(d => new DirectoryInfo(d))
            .Where(info => !info.Name.StartsWith('.'))
            .Select(info => new XElement(S3Ns + "Bucket",
                new XElement(S3Ns + "Name", info.Name),
                new XElement(S3Ns + "CreationDate",
                    info.CreationTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))));

        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(S3Ns + "ListAllMyBucketsResult",
                new XElement(S3Ns + "Owner",
                    new XElement(S3Ns + "ID", "1"),
                    new XElement(S3Ns + "DisplayName", "owner")),
                new XElement(S3Ns + "Buckets", buckets)));

        return XmlContent(xml);
    }

    /// <summary>
    /// PUT /{bucket} – Create a bucket, or set bucket tagging (?tagging).
    /// </summary>
    [HttpPut("/{bucket}")]
    public async Task<IActionResult> PutBucketAsync(
        string bucket, CancellationToken cancellationToken)
    {
        if (Request.Query.ContainsKey("tagging"))
            return await PutBucketTaggingAsync(bucket, cancellationToken);

        ValidateBucketName(bucket);
        Directory.CreateDirectory(ResolveBucketPath(bucket));
        Response.Headers["Location"] = $"/{bucket}";
        return Ok();
    }

    /// <summary>
    /// HEAD /{bucket} – Check if a bucket exists.
    /// </summary>
    [HttpHead("/{bucket}")]
    public IActionResult HeadBucket(string bucket)
    {
        ValidateBucketName(bucket);
        return Directory.Exists(ResolveBucketPath(bucket))
            ? Ok()
            : S3Error("NoSuchBucket", "The specified bucket does not exist");
    }

    /// <summary>
    /// DELETE /{bucket} – Delete a bucket and all its contents,
    /// or delete bucket tagging (?tagging).
    /// </summary>
    [HttpDelete("/{bucket}")]
    public IActionResult DeleteBucket(string bucket)
    {
        if (Request.Query.ContainsKey("tagging"))
            return DeleteBucketTagging(bucket);

        ValidateBucketName(bucket);
        var bucketPath = ResolveBucketPath(bucket);
        if (!Directory.Exists(bucketPath))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        Directory.Delete(bucketPath, recursive: true);

        // Also remove metadata and tags for this bucket
        var metaBucketDir = Path.Combine(_metaBasePath, bucket);
        if (Directory.Exists(metaBucketDir))
            Directory.Delete(metaBucketDir, recursive: true);
        DeleteBucketTagsFile(bucket);

        return NoContent();
    }

    /// <summary>
    /// GET /{bucket} – List objects, get bucket location (?location),
    /// get bucket tagging (?tagging), or list multipart uploads (?uploads).
    /// </summary>
    [HttpGet("/{bucket}")]
    public IActionResult GetBucket(
        string bucket,
        [FromQuery] string? prefix,
        [FromQuery] string? delimiter,
        [FromQuery(Name = "max-keys")] int maxKeys = 1000,
        [FromQuery(Name = "start-after")] string? startAfter = null,
        [FromQuery(Name = "continuation-token")] string? continuationToken = null)
    {
        if (Request.Query.ContainsKey("location"))
            return GetBucketLocation(bucket);
        if (Request.Query.ContainsKey("tagging"))
            return GetBucketTagging(bucket);
        if (Request.Query.ContainsKey("uploads"))
            return ListMultipartUploads(bucket);

        ValidateBucketName(bucket);
        var bucketPath = ResolveBucketPath(bucket);
        if (!Directory.Exists(bucketPath))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        prefix ??= string.Empty;

        if (!string.IsNullOrEmpty(continuationToken))
            startAfter = Encoding.UTF8.GetString(Convert.FromBase64String(continuationToken));

        IEnumerable<string> allKeys = Directory.GetFiles(bucketPath, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(bucketPath, f).Replace('\\', '/'))
            .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(k => k, StringComparer.Ordinal);

        if (!string.IsNullOrEmpty(startAfter))
            allKeys = allKeys.Where(k => string.Compare(k, startAfter, StringComparison.Ordinal) > 0);

        var contents = new List<XElement>();
        var commonPrefixes = new SortedSet<string>(StringComparer.Ordinal);
        string? lastKey = null;
        var truncated = false;

        foreach (var key in allKeys)
        {
            if (contents.Count + commonPrefixes.Count >= maxKeys)
            {
                truncated = true;
                break;
            }

            if (!string.IsNullOrEmpty(delimiter))
            {
                var remaining = key[prefix.Length..];
                var delimiterIndex = remaining.IndexOf(delimiter, StringComparison.Ordinal);
                if (delimiterIndex >= 0)
                {
                    commonPrefixes.Add(prefix + remaining[..(delimiterIndex + delimiter.Length)]);
                    continue;
                }
            }

            var filePath = Path.Combine(bucketPath, key.Replace('/', Path.DirectorySeparatorChar));
            var info = new FileInfo(filePath);
            contents.Add(new XElement(S3Ns + "Contents",
                new XElement(S3Ns + "Key", key),
                new XElement(S3Ns + "LastModified",
                    info.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                new XElement(S3Ns + "ETag", $"\"{ComputeETag(info)}\""),
                new XElement(S3Ns + "Size", info.Length),
                new XElement(S3Ns + "StorageClass", "STANDARD")));
            lastKey = key;
        }

        var elements = new List<object>
        {
            new XElement(S3Ns + "Name", bucket),
            new XElement(S3Ns + "Prefix", prefix),
            new XElement(S3Ns + "KeyCount", contents.Count + commonPrefixes.Count),
            new XElement(S3Ns + "MaxKeys", maxKeys),
            new XElement(S3Ns + "IsTruncated", truncated.ToString().ToLowerInvariant()),
        };

        if (!string.IsNullOrEmpty(continuationToken))
            elements.Add(new XElement(S3Ns + "ContinuationToken", continuationToken));

        if (truncated && lastKey is not null)
            elements.Add(new XElement(S3Ns + "NextContinuationToken",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(lastKey))));

        if (!string.IsNullOrEmpty(delimiter))
            elements.Add(new XElement(S3Ns + "Delimiter", delimiter));

        elements.AddRange(contents);
        elements.AddRange(commonPrefixes.Select(p =>
            new XElement(S3Ns + "CommonPrefixes",
                new XElement(S3Ns + "Prefix", p))));

        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(S3Ns + "ListBucketResult", elements));

        return XmlContent(xml);
    }

    /// <summary>
    /// POST /{bucket} – Handles DeleteObjects (?delete).
    /// </summary>
    [HttpPost("/{bucket}")]
    public async Task<IActionResult> PostBucketAsync(
        string bucket, CancellationToken cancellationToken)
    {
        if (Request.Query.ContainsKey("delete"))
            return await DeleteObjectsAsync(bucket, cancellationToken);

        return S3Error("InvalidRequest", "Unsupported POST operation on bucket");
    }

    // ================================================================
    //  Object operations
    // ================================================================

    /// <summary>
    /// PUT /{bucket}/{**key} – Upload an object, set object tagging (?tagging),
    /// copy an object (x-amz-copy-source), or upload a multipart part.
    /// </summary>
    [HttpPut("/{bucket}/{**key}")]
    public async Task<IActionResult> PutObjectAsync(
        string bucket, string key, CancellationToken cancellationToken)
    {
        ValidateBucketName(bucket);
        ValidateObjectKey(key);

        // Object tagging
        if (Request.Query.ContainsKey("tagging"))
            return await PutObjectTaggingAsync(bucket, key, cancellationToken);

        // Multipart part upload
        if (Request.Query.TryGetValue("partNumber", out var partNumVal)
            && Request.Query.TryGetValue("uploadId", out var uploadIdVal))
        {
            return await UploadPartAsync(
                int.Parse(partNumVal.ToString()),
                uploadIdVal.ToString()!,
                cancellationToken);
        }

        // CopyObject
        if (Request.Headers.TryGetValue("x-amz-copy-source", out var copySource))
            return CopyObject(bucket, key, copySource.ToString());

        // Regular PutObject
        var bucketPath = ResolveBucketPath(bucket);
        if (!Directory.Exists(bucketPath))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        var filePath = ResolveObjectPath(bucket, key);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        await using (var fs = new FileStream(
            filePath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 81920, useAsync: true))
        {
            await Request.Body.CopyToAsync(fs, cancellationToken);
        }

        SaveMetadata(bucket, key, new ObjectMetadata
        {
            ContentType = Request.ContentType ?? ResolveContentType(key),
            UserMetadata = ExtractUserMetadata(),
        });

        Response.Headers.ETag = $"\"{ComputeETag(new FileInfo(filePath))}\"";
        return Ok();
    }

    /// <summary>
    /// GET /{bucket}/{**key} – Download an object, get object tagging (?tagging),
    /// or list parts (?uploadId).
    /// </summary>
    [HttpGet("/{bucket}/{**key}")]
    public IActionResult GetObject(string bucket, string key)
    {
        ValidateBucketName(bucket);
        ValidateObjectKey(key);

        if (Request.Query.ContainsKey("tagging"))
            return GetObjectTagging(bucket, key);

        if (Request.Query.TryGetValue("uploadId", out var uploadIdVal))
            return ListParts(bucket, key, uploadIdVal.ToString()!);

        var bucketPath = ResolveBucketPath(bucket);
        if (!Directory.Exists(bucketPath))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        var filePath = ResolveObjectPath(bucket, key);
        if (!System.IO.File.Exists(filePath))
            return S3Error("NoSuchKey", "The specified key does not exist.");

        var info = new FileInfo(filePath);
        var etag = ComputeETag(info);
        var lastModified = new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero);
        var entityTag = new EntityTagHeaderValue($"\"{etag}\"");

        var metadata = LoadMetadata(bucket, key);
        var contentType = metadata?.ContentType ?? ResolveContentType(key);

        SetUserMetadataHeaders(metadata);

        return PhysicalFile(
            filePath, contentType, lastModified, entityTag,
            enableRangeProcessing: true);
    }

    /// <summary>
    /// HEAD /{bucket}/{**key} – Retrieve object metadata with conditional support.
    /// </summary>
    [HttpHead("/{bucket}/{**key}")]
    public IActionResult HeadObject(string bucket, string key)
    {
        ValidateBucketName(bucket);
        ValidateObjectKey(key);
        var bucketPath = ResolveBucketPath(bucket);
        if (!Directory.Exists(bucketPath))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        var filePath = ResolveObjectPath(bucket, key);
        if (!System.IO.File.Exists(filePath))
            return S3Error("NoSuchKey", "The specified key does not exist.");

        var info = new FileInfo(filePath);
        var etag = ComputeETag(info);
        var lastModified = new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero);

        var conditionalStatus = EvaluateConditionalHeaders(etag, lastModified);
        if (conditionalStatus is not null)
            return StatusCode(conditionalStatus.Value);

        var metadata = LoadMetadata(bucket, key);
        var contentType = metadata?.ContentType ?? ResolveContentType(key);

        SetUserMetadataHeaders(metadata);

        Response.Headers.ETag = $"\"{etag}\"";
        Response.Headers["Last-Modified"] = lastModified.ToString("R");
        Response.Headers.ContentLength = info.Length;
        Response.Headers.ContentType = contentType;
        return Ok();
    }

    /// <summary>
    /// DELETE /{bucket}/{**key} – Delete an object (and its metadata),
    /// delete object tagging (?tagging), or abort a multipart upload (?uploadId).
    /// </summary>
    [HttpDelete("/{bucket}/{**key}")]
    public IActionResult DeleteObject(string bucket, string key)
    {
        ValidateBucketName(bucket);
        ValidateObjectKey(key);

        if (Request.Query.ContainsKey("tagging"))
            return DeleteObjectTagging(bucket, key);

        if (Request.Query.TryGetValue("uploadId", out var uploadIdVal))
            return AbortMultipartUpload(uploadIdVal.ToString()!);

        var bucketPath = ResolveBucketPath(bucket);
        var filePath = ResolveObjectPath(bucket, key);
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            CleanEmptyDirectories(filePath, bucketPath);
        }

        DeleteMetadataFile(bucket, key);

        return NoContent();
    }

    /// <summary>
    /// POST /{bucket}/{**key} – Create or complete a multipart upload.
    /// </summary>
    [HttpPost("/{bucket}/{**key}")]
    public async Task<IActionResult> PostObjectAsync(
        string bucket, string key, CancellationToken cancellationToken)
    {
        ValidateBucketName(bucket);
        ValidateObjectKey(key);

        if (Request.Query.ContainsKey("uploads"))
            return CreateMultipartUpload(bucket, key);

        if (Request.Query.TryGetValue("uploadId", out var uploadIdVal))
            return await CompleteMultipartUploadAsync(
                bucket, key, uploadIdVal.ToString()!, cancellationToken);

        return S3Error("InvalidRequest", "Unsupported POST operation on object");
    }

    // ================================================================
    //  CopyObject
    // ================================================================

    /// <summary>
    /// Copies an object from a source bucket/key to a destination bucket/key.
    /// Supports x-amz-metadata-directive: COPY (default) or REPLACE.
    /// </summary>
    private IActionResult CopyObject(string destBucket, string destKey, string copySourceRaw)
    {
        var decoded = Uri.UnescapeDataString(copySourceRaw).TrimStart('/');
        var slashIndex = decoded.IndexOf('/');
        if (slashIndex <= 0)
            return S3Error("InvalidArgument", "Invalid x-amz-copy-source header");

        var srcBucket = decoded[..slashIndex];
        var srcKey = decoded[(slashIndex + 1)..];

        ValidateBucketName(srcBucket);
        ValidateObjectKey(srcKey);

        var srcFilePath = ResolveObjectPath(srcBucket, srcKey);
        if (!System.IO.File.Exists(srcFilePath))
            return S3Error("NoSuchKey", "The specified source key does not exist.");

        var destBucketPath = ResolveBucketPath(destBucket);
        if (!Directory.Exists(destBucketPath))
            return S3Error("NoSuchBucket", "The destination bucket does not exist");

        var destFilePath = ResolveObjectPath(destBucket, destKey);
        Directory.CreateDirectory(Path.GetDirectoryName(destFilePath)!);
        System.IO.File.Copy(srcFilePath, destFilePath, overwrite: true);

        var directive = Request.Headers["x-amz-metadata-directive"].ToString();
        ObjectMetadata destMeta;
        if (string.Equals(directive, "REPLACE", StringComparison.OrdinalIgnoreCase))
        {
            destMeta = new ObjectMetadata
            {
                ContentType = Request.ContentType ?? ResolveContentType(destKey),
                UserMetadata = ExtractUserMetadata(),
            };
        }
        else
        {
            destMeta = LoadMetadata(srcBucket, srcKey) ?? new ObjectMetadata
            {
                ContentType = ResolveContentType(srcKey),
            };
        }

        SaveMetadata(destBucket, destKey, destMeta);

        var info = new FileInfo(destFilePath);
        var etag = ComputeETag(info);

        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(S3Ns + "CopyObjectResult",
                new XElement(S3Ns + "LastModified",
                    info.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                new XElement(S3Ns + "ETag", $"\"{etag}\"")));

        return XmlContent(xml);
    }

    // ================================================================
    //  DeleteObjects (bulk)
    // ================================================================

    /// <summary>
    /// Deletes multiple objects (and their metadata) in a single request.
    /// </summary>
    private async Task<IActionResult> DeleteObjectsAsync(
        string bucket, CancellationToken cancellationToken)
    {
        ValidateBucketName(bucket);
        var bucketPath = ResolveBucketPath(bucket);
        if (!Directory.Exists(bucketPath))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        var doc = await XDocument.LoadAsync(Request.Body, LoadOptions.None, cancellationToken);
        var quiet = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "Quiet")?.Value == "true";

        var deleted = new List<XElement>();
        var errors = new List<XElement>();

        foreach (var obj in doc.Descendants().Where(e => e.Name.LocalName == "Object"))
        {
            var key = obj.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "Key")?.Value;
            if (string.IsNullOrEmpty(key))
                continue;

            try
            {
                ValidateObjectKey(key);
                var filePath = ResolveObjectPath(bucket, key);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    CleanEmptyDirectories(filePath, bucketPath);
                }

                DeleteMetadataFile(bucket, key);

                if (!quiet)
                    deleted.Add(new XElement(S3Ns + "Deleted",
                        new XElement(S3Ns + "Key", key)));
            }
            catch (Exception ex)
            {
                errors.Add(new XElement(S3Ns + "Error",
                    new XElement(S3Ns + "Key", key),
                    new XElement(S3Ns + "Code", "InternalError"),
                    new XElement(S3Ns + "Message", ex.Message)));
            }
        }

        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(S3Ns + "DeleteResult",
                deleted.Concat<XElement>(errors)));

        return XmlContent(xml);
    }

    // ================================================================
    //  Object tagging
    // ================================================================

    /// <summary>
    /// GET /{bucket}/{key}?tagging – Returns the tag set for an object.
    /// </summary>
    private IActionResult GetObjectTagging(string bucket, string key)
    {
        var filePath = ResolveObjectPath(bucket, key);
        if (!System.IO.File.Exists(filePath))
            return S3Error("NoSuchKey", "The specified key does not exist.");

        var metadata = LoadMetadata(bucket, key);
        return TagSetXmlResponse(metadata?.Tags ?? []);
    }

    /// <summary>
    /// PUT /{bucket}/{key}?tagging – Sets the tag set for an object.
    /// </summary>
    private async Task<IActionResult> PutObjectTaggingAsync(
        string bucket, string key, CancellationToken cancellationToken)
    {
        var filePath = ResolveObjectPath(bucket, key);
        if (!System.IO.File.Exists(filePath))
            return S3Error("NoSuchKey", "The specified key does not exist.");

        var tags = await ParseTagSetFromBodyAsync(cancellationToken);

        var metadata = LoadMetadata(bucket, key) ?? new ObjectMetadata
        {
            ContentType = ResolveContentType(key),
        };

        // Preserve existing metadata, update only tags
        SaveMetadata(bucket, key, new ObjectMetadata
        {
            ContentType = metadata.ContentType,
            UserMetadata = metadata.UserMetadata,
            Tags = tags,
        });

        return Ok();
    }

    /// <summary>
    /// DELETE /{bucket}/{key}?tagging – Removes the tag set from an object.
    /// </summary>
    private IActionResult DeleteObjectTagging(string bucket, string key)
    {
        var filePath = ResolveObjectPath(bucket, key);
        if (!System.IO.File.Exists(filePath))
            return S3Error("NoSuchKey", "The specified key does not exist.");

        var metadata = LoadMetadata(bucket, key);
        if (metadata is not null && metadata.Tags.Count > 0)
        {
            SaveMetadata(bucket, key, new ObjectMetadata
            {
                ContentType = metadata.ContentType,
                UserMetadata = metadata.UserMetadata,
                Tags = [],
            });
        }

        return NoContent();
    }

    // ================================================================
    //  Bucket tagging
    // ================================================================

    /// <summary>
    /// GET /{bucket}?tagging – Returns the tag set for a bucket.
    /// </summary>
    private IActionResult GetBucketTagging(string bucket)
    {
        ValidateBucketName(bucket);
        if (!Directory.Exists(ResolveBucketPath(bucket)))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        var tags = LoadBucketTags(bucket);
        return TagSetXmlResponse(tags);
    }

    /// <summary>
    /// PUT /{bucket}?tagging – Sets the tag set for a bucket.
    /// </summary>
    private async Task<IActionResult> PutBucketTaggingAsync(
        string bucket, CancellationToken cancellationToken)
    {
        ValidateBucketName(bucket);
        if (!Directory.Exists(ResolveBucketPath(bucket)))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        var tags = await ParseTagSetFromBodyAsync(cancellationToken);
        SaveBucketTags(bucket, tags);
        return Ok();
    }

    /// <summary>
    /// DELETE /{bucket}?tagging – Removes the tag set from a bucket.
    /// </summary>
    private IActionResult DeleteBucketTagging(string bucket)
    {
        ValidateBucketName(bucket);
        if (!Directory.Exists(ResolveBucketPath(bucket)))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        DeleteBucketTagsFile(bucket);
        return NoContent();
    }

    // ================================================================
    //  Multipart upload
    // ================================================================

    /// <summary>
    /// Initiates a multipart upload and returns an UploadId.
    /// </summary>
    private IActionResult CreateMultipartUpload(string bucket, string key)
    {
        var bucketPath = ResolveBucketPath(bucket);
        if (!Directory.Exists(bucketPath))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        var uploadId = Guid.NewGuid().ToString("N");
        var uploadDir = Path.Combine(_multipartPath, uploadId);
        Directory.CreateDirectory(uploadDir);

        System.IO.File.WriteAllText(
            Path.Combine(uploadDir, ".info"),
            $"{bucket}\n{key}");

        var meta = new ObjectMetadata
        {
            ContentType = Request.ContentType ?? ResolveContentType(key),
            UserMetadata = ExtractUserMetadata(),
        };
        System.IO.File.WriteAllText(
            Path.Combine(uploadDir, ".meta.json"),
            JsonSerializer.Serialize(meta));

        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(S3Ns + "InitiateMultipartUploadResult",
                new XElement(S3Ns + "Bucket", bucket),
                new XElement(S3Ns + "Key", key),
                new XElement(S3Ns + "UploadId", uploadId)));

        return XmlContent(xml);
    }

    /// <summary>
    /// Uploads a single part of a multipart upload.
    /// </summary>
    private async Task<IActionResult> UploadPartAsync(
        int partNumber, string uploadId, CancellationToken cancellationToken)
    {
        var uploadDir = Path.Combine(_multipartPath, uploadId);
        if (!Directory.Exists(uploadDir))
            return S3Error("NoSuchUpload", "The specified multipart upload does not exist.");

        var partPath = Path.Combine(uploadDir, partNumber.ToString());

        await using (var fs = new FileStream(
            partPath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 81920, useAsync: true))
        {
            await Request.Body.CopyToAsync(fs, cancellationToken);
        }

        var etag = ComputeETag(new FileInfo(partPath));
        Response.Headers.ETag = $"\"{etag}\"";
        return Ok();
    }

    /// <summary>
    /// Concatenates uploaded parts into the final object and persists metadata.
    /// </summary>
    private async Task<IActionResult> CompleteMultipartUploadAsync(
        string bucket, string key, string uploadId,
        CancellationToken cancellationToken)
    {
        var uploadDir = Path.Combine(_multipartPath, uploadId);
        if (!Directory.Exists(uploadDir))
            return S3Error("NoSuchUpload", "The specified multipart upload does not exist.");

        var bucketPath = ResolveBucketPath(bucket);
        if (!Directory.Exists(bucketPath))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        var doc = await XDocument.LoadAsync(Request.Body, LoadOptions.None, cancellationToken);
        var parts = doc.Descendants()
            .Where(e => e.Name.LocalName == "Part")
            .Select(p => int.Parse(
                p.Elements().First(e => e.Name.LocalName == "PartNumber").Value))
            .OrderBy(n => n)
            .ToList();

        var filePath = ResolveObjectPath(bucket, key);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        await using (var output = new FileStream(
            filePath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 81920, useAsync: true))
        {
            foreach (var partNumber in parts)
            {
                var partPath = Path.Combine(uploadDir, partNumber.ToString());
                if (!System.IO.File.Exists(partPath))
                    return S3Error("InvalidPart", $"Part {partNumber} not found.");

                await using var partStream = new FileStream(
                    partPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                    bufferSize: 81920, useAsync: true);
                await partStream.CopyToAsync(output, cancellationToken);
            }
        }

        var uploadMetaPath = Path.Combine(uploadDir, ".meta.json");
        ObjectMetadata? objMeta = null;
        if (System.IO.File.Exists(uploadMetaPath))
        {
            var json = await System.IO.File.ReadAllTextAsync(uploadMetaPath, cancellationToken);
            objMeta = JsonSerializer.Deserialize<ObjectMetadata>(json);
        }
        objMeta ??= new ObjectMetadata { ContentType = ResolveContentType(key) };
        SaveMetadata(bucket, key, objMeta);

        Directory.Delete(uploadDir, recursive: true);

        var info = new FileInfo(filePath);
        var etag = ComputeETag(info);

        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(S3Ns + "CompleteMultipartUploadResult",
                new XElement(S3Ns + "Location",
                    $"{Request.Scheme}://{Request.Host}/{bucket}/{key}"),
                new XElement(S3Ns + "Bucket", bucket),
                new XElement(S3Ns + "Key", key),
                new XElement(S3Ns + "ETag", $"\"{etag}\"")));

        return XmlContent(xml);
    }

    /// <summary>
    /// Aborts a multipart upload and discards all uploaded parts.
    /// </summary>
    private IActionResult AbortMultipartUpload(string uploadId)
    {
        var uploadDir = Path.Combine(_multipartPath, uploadId);
        if (Directory.Exists(uploadDir))
            Directory.Delete(uploadDir, recursive: true);

        return NoContent();
    }

    // ================================================================
    //  ListMultipartUploads / ListParts
    // ================================================================

    /// <summary>
    /// GET /{bucket}?uploads – Lists in-progress multipart uploads for a bucket.
    /// </summary>
    private IActionResult ListMultipartUploads(string bucket)
    {
        ValidateBucketName(bucket);
        if (!Directory.Exists(ResolveBucketPath(bucket)))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        var uploads = new List<XElement>();

        if (Directory.Exists(_multipartPath))
        {
            foreach (var dir in Directory.GetDirectories(_multipartPath))
            {
                var infoPath = Path.Combine(dir, ".info");
                if (!System.IO.File.Exists(infoPath))
                    continue;

                var lines = System.IO.File.ReadAllLines(infoPath);
                if (lines.Length < 2 || lines[0] != bucket)
                    continue;

                var uploadId = Path.GetFileName(dir);
                var key = lines[1];
                var initiated = new DirectoryInfo(dir).CreationTimeUtc;

                uploads.Add(new XElement(S3Ns + "Upload",
                    new XElement(S3Ns + "Key", key),
                    new XElement(S3Ns + "UploadId", uploadId),
                    new XElement(S3Ns + "Initiator",
                        new XElement(S3Ns + "ID", "1"),
                        new XElement(S3Ns + "DisplayName", "owner")),
                    new XElement(S3Ns + "Owner",
                        new XElement(S3Ns + "ID", "1"),
                        new XElement(S3Ns + "DisplayName", "owner")),
                    new XElement(S3Ns + "StorageClass", "STANDARD"),
                    new XElement(S3Ns + "Initiated",
                        initiated.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))));
            }
        }

        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(S3Ns + "ListMultipartUploadsResult",
                new XElement(S3Ns + "Bucket", bucket),
                new XElement(S3Ns + "MaxUploads", 1000),
                new XElement(S3Ns + "IsTruncated", "false"),
                uploads));

        return XmlContent(xml);
    }

    /// <summary>
    /// GET /{bucket}/{key}?uploadId=ID – Lists uploaded parts for a multipart upload.
    /// </summary>
    private IActionResult ListParts(string bucket, string key, string uploadId)
    {
        var uploadDir = Path.Combine(_multipartPath, uploadId);
        if (!Directory.Exists(uploadDir))
            return S3Error("NoSuchUpload", "The specified multipart upload does not exist.");

        var parts = Directory.GetFiles(uploadDir)
            .Select(f => Path.GetFileName(f))
            .Where(name => int.TryParse(name, out _))
            .Select(name => int.Parse(name))
            .OrderBy(n => n)
            .Select(n =>
            {
                var info = new FileInfo(Path.Combine(uploadDir, n.ToString()));
                return new XElement(S3Ns + "Part",
                    new XElement(S3Ns + "PartNumber", n),
                    new XElement(S3Ns + "LastModified",
                        info.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                    new XElement(S3Ns + "ETag", $"\"{ComputeETag(info)}\""),
                    new XElement(S3Ns + "Size", info.Length));
            })
            .ToList();

        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(S3Ns + "ListPartsResult",
                new XElement(S3Ns + "Bucket", bucket),
                new XElement(S3Ns + "Key", key),
                new XElement(S3Ns + "UploadId", uploadId),
                new XElement(S3Ns + "MaxParts", 1000),
                new XElement(S3Ns + "IsTruncated", "false"),
                parts));

        return XmlContent(xml);
    }

    // ================================================================
    //  Bucket sub-resources
    // ================================================================

    /// <summary>
    /// Returns the location constraint for a bucket.
    /// </summary>
    private IActionResult GetBucketLocation(string bucket)
    {
        ValidateBucketName(bucket);
        if (!Directory.Exists(ResolveBucketPath(bucket)))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(S3Ns + "LocationConstraint", "us-east-1"));

        return XmlContent(xml);
    }

    // ================================================================
    //  Metadata persistence helpers
    // ================================================================

    private string ResolveMetaPath(string bucket, string key)
    {
        var normalizedKey = key.Replace('/', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(
            Path.Combine(_metaBasePath, bucket, normalizedKey + ".json"));
        if (!path.StartsWith(Path.GetFullPath(_metaBasePath), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid metadata path.");
        return path;
    }

    private void SaveMetadata(string bucket, string key, ObjectMetadata metadata)
    {
        var metaPath = ResolveMetaPath(bucket, key);
        Directory.CreateDirectory(Path.GetDirectoryName(metaPath)!);
        System.IO.File.WriteAllText(metaPath, JsonSerializer.Serialize(metadata));
    }

    private ObjectMetadata? LoadMetadata(string bucket, string key)
    {
        var metaPath = ResolveMetaPath(bucket, key);
        if (!System.IO.File.Exists(metaPath))
            return null;
        var json = System.IO.File.ReadAllText(metaPath);
        return JsonSerializer.Deserialize<ObjectMetadata>(json);
    }

    private void DeleteMetadataFile(string bucket, string key)
    {
        var metaPath = ResolveMetaPath(bucket, key);
        if (!System.IO.File.Exists(metaPath))
            return;

        System.IO.File.Delete(metaPath);
        var metaBucketDir = Path.Combine(_metaBasePath, bucket);
        CleanEmptyDirectories(metaPath, metaBucketDir);
    }

    // ================================================================
    //  Bucket tags persistence helpers
    // ================================================================

    private string ResolveBucketTagsPath(string bucket) =>
        Path.Combine(_metaBasePath, ".buckets", bucket + ".json");

    private void SaveBucketTags(string bucket, Dictionary<string, string> tags)
    {
        var path = ResolveBucketTagsPath(bucket);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllText(path, JsonSerializer.Serialize(tags));
    }

    private Dictionary<string, string> LoadBucketTags(string bucket)
    {
        var path = ResolveBucketTagsPath(bucket);
        if (!System.IO.File.Exists(path))
            return [];
        var json = System.IO.File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
    }

    private void DeleteBucketTagsFile(string bucket)
    {
        var path = ResolveBucketTagsPath(bucket);
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
    }

    // ================================================================
    //  Tagging XML helpers
    // ================================================================

    /// <summary>
    /// Parses a Tagging XML request body into a dictionary.
    /// </summary>
    private async Task<Dictionary<string, string>> ParseTagSetFromBodyAsync(
        CancellationToken cancellationToken)
    {
        var doc = await XDocument.LoadAsync(Request.Body, LoadOptions.None, cancellationToken);
        return doc.Descendants()
            .Where(e => e.Name.LocalName == "Tag")
            .ToDictionary(
                t => t.Elements().First(e => e.Name.LocalName == "Key").Value,
                t => t.Elements().First(e => e.Name.LocalName == "Value").Value);
    }

    /// <summary>
    /// Builds a Tagging XML response from a tag dictionary.
    /// </summary>
    private static ContentResult TagSetXmlResponse(Dictionary<string, string> tags)
    {
        var tagElements = tags.Select(t =>
            new XElement(S3Ns + "Tag",
                new XElement(S3Ns + "Key", t.Key),
                new XElement(S3Ns + "Value", t.Value)));

        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(S3Ns + "Tagging",
                new XElement(S3Ns + "TagSet", tagElements)));

        return XmlContent(xml);
    }

    // ================================================================
    //  Request header helpers
    // ================================================================

    /// <summary>
    /// Extracts x-amz-meta-* headers from the current request into a dictionary.
    /// </summary>
    private Dictionary<string, string> ExtractUserMetadata()
    {
        const string prefix = "x-amz-meta-";
        var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in Request.Headers)
        {
            if (header.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                meta[header.Key[prefix.Length..]] = header.Value.ToString();
        }
        return meta;
    }

    /// <summary>
    /// Writes x-amz-meta-* response headers from loaded metadata.
    /// </summary>
    private void SetUserMetadataHeaders(ObjectMetadata? metadata)
    {
        if (metadata?.UserMetadata is not { Count: > 0 })
            return;

        foreach (var (k, v) in metadata.UserMetadata)
            Response.Headers[$"x-amz-meta-{k}"] = v;
    }

    // ================================================================
    //  General helpers
    // ================================================================

    private void EnsureBaseDirectory() => Directory.CreateDirectory(_basePath);

    private string ResolveBucketPath(string bucket)
    {
        EnsureBaseDirectory();
        var path = Path.GetFullPath(Path.Combine(_basePath, bucket));
        if (!path.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid bucket name.", nameof(bucket));
        return path;
    }

    private string ResolveObjectPath(string bucket, string key)
    {
        var bucketPath = ResolveBucketPath(bucket);
        var normalizedKey = key.Replace('/', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(bucketPath, normalizedKey));
        if (!path.StartsWith(bucketPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid object key.", nameof(key));
        return path;
    }

    private static void ValidateBucketName(string bucket)
    {
        ArgumentNullException.ThrowIfNull(bucket);
        if (bucket.StartsWith('.')
            || bucket.Contains("..")
            || bucket.Contains('/')
            || bucket.Contains('\\'))
            throw new ArgumentException("Invalid bucket name.", nameof(bucket));
    }

    private static void ValidateObjectKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Object key cannot be empty.", nameof(key));
        if (key.Contains(".."))
            throw new ArgumentException("Invalid object key.", nameof(key));
    }

    private static string ComputeETag(FileInfo info)
    {
        using var stream = info.OpenRead();
        var hash = MD5.HashData(stream);
        return Convert.ToHexStringLower(hash);
    }

    private static void CleanEmptyDirectories(string filePath, string bucketPath)
    {
        var dir = Path.GetDirectoryName(filePath);
        while (dir is not null
            && dir.Length > bucketPath.Length
            && dir.StartsWith(bucketPath, StringComparison.OrdinalIgnoreCase)
            && !Directory.EnumerateFileSystemEntries(dir).Any())
        {
            Directory.Delete(dir);
            dir = Path.GetDirectoryName(dir);
        }
    }

    /// <summary>
    /// Evaluates S3 conditional request headers.
    /// Returns the appropriate HTTP status code, or null if the request should proceed.
    /// </summary>
    private int? EvaluateConditionalHeaders(string etag, DateTimeOffset lastModified)
    {
        var quotedEtag = $"\"{etag}\"";

        var ifMatch = Request.Headers.IfMatch.ToString();
        if (!string.IsNullOrEmpty(ifMatch) && ifMatch != "*" && ifMatch != quotedEtag)
            return StatusCodes.Status412PreconditionFailed;

        if (Request.Headers.TryGetValue("If-Unmodified-Since", out var ifUnmodified)
            && DateTimeOffset.TryParse(ifUnmodified, out var ifUnmodifiedSince)
            && lastModified > ifUnmodifiedSince)
            return StatusCodes.Status412PreconditionFailed;

        var ifNoneMatch = Request.Headers.IfNoneMatch.ToString();
        if (!string.IsNullOrEmpty(ifNoneMatch)
            && (ifNoneMatch == "*" || ifNoneMatch == quotedEtag))
            return StatusCodes.Status304NotModified;

        if (Request.Headers.TryGetValue("If-Modified-Since", out var ifModified)
            && DateTimeOffset.TryParse(ifModified, out var ifModifiedSince)
            && lastModified <= ifModifiedSince)
            return StatusCodes.Status304NotModified;

        return null;
    }

    private static string ResolveContentType(string key) =>
        ContentTypeProvider.TryGetContentType(key, out var ct) ? ct : "application/octet-stream";

    private static ContentResult S3Error(string code, string message) =>
        new()
        {
            Content = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("Error",
                    new XElement("Code", code),
                    new XElement("Message", message),
                    new XElement("RequestId", "1"))).ToString(),
            ContentType = "application/xml",
            StatusCode = code switch
            {
                "NoSuchBucket" or "NoSuchKey" => StatusCodes.Status404NotFound,
                "NoSuchUpload" or "InvalidPart" => StatusCodes.Status404NotFound,
                "BucketNotEmpty" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            }
        };

    private static ContentResult XmlContent(XDocument xml) =>
        new()
        {
            Content = xml.Declaration + "\n" + xml.ToString(),
            ContentType = "application/xml",
            StatusCode = StatusCodes.Status200OK
        };
}
