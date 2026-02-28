using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

using WorkStorage.Models;

namespace WorkStorage.Controllers;

/// <summary>
/// Provides AWS S3-compatible REST API endpoints backed by local file system storage.
/// Authentication is not enforced.
/// </summary>
[ApiController]
public sealed class S3Controller : ControllerBase
{
    private static readonly XNamespace S3Ns = "http://s3.amazonaws.com/doc/2006-03-01/";
    private static readonly XNamespace XsiNs = "http://www.w3.org/2001/XMLSchema-instance";
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
        public string StorageClass { get; init; } = "STANDARD";
        public string Acl { get; init; } = "private";
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
    /// PUT /{bucket} – Create a bucket, or set bucket tagging/acl/cors.
    /// </summary>
    [HttpPut("/{bucket}")]
    public async Task<IActionResult> PutBucketAsync(
        string bucket, CancellationToken cancellationToken)
    {
        if (Request.Query.ContainsKey("tagging"))
            return await PutBucketTaggingAsync(bucket, cancellationToken);
        if (Request.Query.ContainsKey("acl"))
            return await PutBucketAclAsync(bucket, cancellationToken);
        if (Request.Query.ContainsKey("cors"))
            return await PutBucketCorsAsync(bucket, cancellationToken);

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
    /// DELETE /{bucket} – Delete a bucket, or delete bucket tagging/cors.
    /// </summary>
    [HttpDelete("/{bucket}")]
    public IActionResult DeleteBucket(string bucket)
    {
        if (Request.Query.ContainsKey("tagging"))
            return DeleteBucketTagging(bucket);
        if (Request.Query.ContainsKey("cors"))
            return DeleteBucketCors(bucket);

        ValidateBucketName(bucket);
        var bucketPath = ResolveBucketPath(bucket);
        if (!Directory.Exists(bucketPath))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        Directory.Delete(bucketPath, recursive: true);

        var metaBucketDir = Path.Combine(_metaBasePath, bucket);
        if (Directory.Exists(metaBucketDir))
            Directory.Delete(metaBucketDir, recursive: true);
        DeleteBucketTagsFile(bucket);
        DeleteBucketCorsFile(bucket);
        DeleteBucketAclFile(bucket);

        return NoContent();
    }

    /// <summary>
    /// GET /{bucket} – List objects, or get sub-resources
    /// (?location, ?tagging, ?acl, ?cors, ?uploads).
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
        if (Request.Query.ContainsKey("acl"))
            return GetBucketAcl(bucket);
        if (Request.Query.ContainsKey("cors"))
            return GetBucketCors(bucket);
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
            var meta = LoadMetadata(bucket, key);

            contents.Add(new XElement(S3Ns + "Contents",
                new XElement(S3Ns + "Key", key),
                new XElement(S3Ns + "LastModified",
                    info.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                new XElement(S3Ns + "ETag", $"\"{ComputeETag(info)}\""),
                new XElement(S3Ns + "Size", info.Length),
                new XElement(S3Ns + "StorageClass", meta?.StorageClass ?? "STANDARD")));
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
    /// PUT /{bucket}/{**key} – Upload object, set tagging/acl, copy, or upload part.
    /// </summary>
    [HttpPut("/{bucket}/{**key}")]
    public async Task<IActionResult> PutObjectAsync(
        string bucket, string key, CancellationToken cancellationToken)
    {
        ValidateBucketName(bucket);
        ValidateObjectKey(key);

        if (Request.Query.ContainsKey("tagging"))
            return await PutObjectTaggingAsync(bucket, key, cancellationToken);
        if (Request.Query.ContainsKey("acl"))
            return await PutObjectAclAsync(bucket, key, cancellationToken);

        if (Request.Query.TryGetValue("partNumber", out var partNumVal)
            && Request.Query.TryGetValue("uploadId", out var uploadIdVal))
        {
            return await UploadPartAsync(
                int.Parse(partNumVal.ToString()),
                uploadIdVal.ToString()!,
                cancellationToken);
        }

        if (Request.Headers.TryGetValue("x-amz-copy-source", out var copySource))
            return CopyObject(bucket, key, copySource.ToString());

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
            StorageClass = Request.Headers["x-amz-storage-class"].ToString()
                is { Length: > 0 } sc ? sc : "STANDARD",
            Acl = Request.Headers["x-amz-acl"].ToString()
                is { Length: > 0 } acl ? acl : "private",
            UserMetadata = ExtractUserMetadata(),
        });

        Response.Headers.ETag = $"\"{ComputeETag(new FileInfo(filePath))}\"";
        return Ok();
    }

    /// <summary>
    /// GET /{bucket}/{**key} – Download object, get tagging/acl, or list parts.
    /// </summary>
    [HttpGet("/{bucket}/{**key}")]
    public IActionResult GetObject(string bucket, string key)
    {
        ValidateBucketName(bucket);
        ValidateObjectKey(key);

        if (Request.Query.ContainsKey("tagging"))
            return GetObjectTagging(bucket, key);
        if (Request.Query.ContainsKey("acl"))
            return GetObjectAcl(bucket, key);
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
        if (metadata?.StorageClass is { Length: > 0 } sc && sc != "STANDARD")
            Response.Headers["x-amz-storage-class"] = sc;

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
        if (metadata?.StorageClass is { Length: > 0 } sc && sc != "STANDARD")
            Response.Headers["x-amz-storage-class"] = sc;

        Response.Headers.ETag = $"\"{etag}\"";
        Response.Headers["Last-Modified"] = lastModified.ToString("R");
        Response.Headers.ContentLength = info.Length;
        Response.Headers.ContentType = contentType;
        return Ok();
    }

    /// <summary>
    /// DELETE /{bucket}/{**key} – Delete object, tagging, or abort multipart.
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
                StorageClass = Request.Headers["x-amz-storage-class"].ToString()
                    is { Length: > 0 } sc ? sc : "STANDARD",
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

    private IActionResult GetObjectTagging(string bucket, string key)
    {
        if (!System.IO.File.Exists(ResolveObjectPath(bucket, key)))
            return S3Error("NoSuchKey", "The specified key does not exist.");

        var metadata = LoadMetadata(bucket, key);
        return TagSetXmlResponse(metadata?.Tags ?? []);
    }

    private async Task<IActionResult> PutObjectTaggingAsync(
        string bucket, string key, CancellationToken cancellationToken)
    {
        if (!System.IO.File.Exists(ResolveObjectPath(bucket, key)))
            return S3Error("NoSuchKey", "The specified key does not exist.");

        var tags = await ParseTagSetFromBodyAsync(cancellationToken);
        var metadata = LoadMetadata(bucket, key) ?? new ObjectMetadata
        {
            ContentType = ResolveContentType(key),
        };

        SaveMetadata(bucket, key, new ObjectMetadata
        {
            ContentType = metadata.ContentType,
            StorageClass = metadata.StorageClass,
            Acl = metadata.Acl,
            UserMetadata = metadata.UserMetadata,
            Tags = tags,
        });

        return Ok();
    }

    private IActionResult DeleteObjectTagging(string bucket, string key)
    {
        if (!System.IO.File.Exists(ResolveObjectPath(bucket, key)))
            return S3Error("NoSuchKey", "The specified key does not exist.");

        var metadata = LoadMetadata(bucket, key);
        if (metadata is not null && metadata.Tags.Count > 0)
        {
            SaveMetadata(bucket, key, new ObjectMetadata
            {
                ContentType = metadata.ContentType,
                StorageClass = metadata.StorageClass,
                Acl = metadata.Acl,
                UserMetadata = metadata.UserMetadata,
                Tags = [],
            });
        }

        return NoContent();
    }

    // ================================================================
    //  Bucket tagging
    // ================================================================

    private IActionResult GetBucketTagging(string bucket)
    {
        ValidateBucketName(bucket);
        if (!Directory.Exists(ResolveBucketPath(bucket)))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        return TagSetXmlResponse(LoadBucketTags(bucket));
    }

    private async Task<IActionResult> PutBucketTaggingAsync(
        string bucket, CancellationToken cancellationToken)
    {
        ValidateBucketName(bucket);
        if (!Directory.Exists(ResolveBucketPath(bucket)))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        SaveBucketTags(bucket, await ParseTagSetFromBodyAsync(cancellationToken));
        return Ok();
    }

    private IActionResult DeleteBucketTagging(string bucket)
    {
        ValidateBucketName(bucket);
        if (!Directory.Exists(ResolveBucketPath(bucket)))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        DeleteBucketTagsFile(bucket);
        return NoContent();
    }

    // ================================================================
    //  ACL (bucket / object)
    // ================================================================

    private IActionResult GetBucketAcl(string bucket)
    {
        ValidateBucketName(bucket);
        if (!Directory.Exists(ResolveBucketPath(bucket)))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        var acl = LoadBucketAcl(bucket);
        return AclXmlResponse(acl);
    }

    private async Task<IActionResult> PutBucketAclAsync(
        string bucket, CancellationToken cancellationToken)
    {
        ValidateBucketName(bucket);
        if (!Directory.Exists(ResolveBucketPath(bucket)))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        // Accept canned ACL from header, or drain XML body
        var cannedAcl = Request.Headers["x-amz-acl"].ToString();
        if (string.IsNullOrEmpty(cannedAcl))
        {
            // Read and discard the XML body – extract the canned type if possible
            await Request.Body.CopyToAsync(Stream.Null, cancellationToken);
            cannedAcl = "private";
        }

        SaveBucketAcl(bucket, cannedAcl);
        return Ok();
    }

    private IActionResult GetObjectAcl(string bucket, string key)
    {
        if (!System.IO.File.Exists(ResolveObjectPath(bucket, key)))
            return S3Error("NoSuchKey", "The specified key does not exist.");

        var metadata = LoadMetadata(bucket, key);
        return AclXmlResponse(metadata?.Acl ?? "private");
    }

    private async Task<IActionResult> PutObjectAclAsync(
        string bucket, string key, CancellationToken cancellationToken)
    {
        if (!System.IO.File.Exists(ResolveObjectPath(bucket, key)))
            return S3Error("NoSuchKey", "The specified key does not exist.");

        var cannedAcl = Request.Headers["x-amz-acl"].ToString();
        if (string.IsNullOrEmpty(cannedAcl))
        {
            await Request.Body.CopyToAsync(Stream.Null, cancellationToken);
            cannedAcl = "private";
        }

        var metadata = LoadMetadata(bucket, key) ?? new ObjectMetadata
        {
            ContentType = ResolveContentType(key),
        };

        SaveMetadata(bucket, key, new ObjectMetadata
        {
            ContentType = metadata.ContentType,
            StorageClass = metadata.StorageClass,
            Acl = cannedAcl,
            UserMetadata = metadata.UserMetadata,
            Tags = metadata.Tags,
        });

        return Ok();
    }

    /// <summary>
    /// Generates an AccessControlPolicy XML response for a canned ACL.
    /// </summary>
    private static ContentResult AclXmlResponse(string cannedAcl)
    {
        var grants = new List<XElement>
        {
            OwnerGrant("FULL_CONTROL"),
        };

        if (cannedAcl is "public-read" or "public-read-write")
            grants.Add(AllUsersGrant("READ"));
        if (cannedAcl is "public-read-write")
            grants.Add(AllUsersGrant("WRITE"));
        if (cannedAcl is "authenticated-read")
            grants.Add(AuthenticatedUsersGrant("READ"));

        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(S3Ns + "AccessControlPolicy",
                new XElement(S3Ns + "Owner",
                    new XElement(S3Ns + "ID", "1"),
                    new XElement(S3Ns + "DisplayName", "owner")),
                new XElement(S3Ns + "AccessControlList", grants)));

        return XmlContent(xml);
    }

    private static XElement OwnerGrant(string permission) =>
        new(S3Ns + "Grant",
            new XElement(S3Ns + "Grantee",
                new XAttribute(XNamespace.Xmlns + "xsi", XsiNs.NamespaceName),
                new XAttribute(XsiNs + "type", "CanonicalUser"),
                new XElement(S3Ns + "ID", "1"),
                new XElement(S3Ns + "DisplayName", "owner")),
            new XElement(S3Ns + "Permission", permission));

    private static XElement AllUsersGrant(string permission) =>
        new(S3Ns + "Grant",
            new XElement(S3Ns + "Grantee",
                new XAttribute(XNamespace.Xmlns + "xsi", XsiNs.NamespaceName),
                new XAttribute(XsiNs + "type", "Group"),
                new XElement(S3Ns + "URI",
                    "http://acs.amazonaws.com/groups/global/AllUsers")),
            new XElement(S3Ns + "Permission", permission));

    private static XElement AuthenticatedUsersGrant(string permission) =>
        new(S3Ns + "Grant",
            new XElement(S3Ns + "Grantee",
                new XAttribute(XNamespace.Xmlns + "xsi", XsiNs.NamespaceName),
                new XAttribute(XsiNs + "type", "Group"),
                new XElement(S3Ns + "URI",
                    "http://acs.amazonaws.com/groups/global/AuthenticatedUsers")),
            new XElement(S3Ns + "Permission", permission));

    // ================================================================
    //  Bucket CORS
    // ================================================================

    private IActionResult GetBucketCors(string bucket)
    {
        ValidateBucketName(bucket);
        if (!Directory.Exists(ResolveBucketPath(bucket)))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        var rules = LoadBucketCors(bucket);
        if (rules.Count == 0)
            return S3Error("NoSuchCORSConfiguration",
                "The CORS configuration does not exist");

        var ruleElements = rules.Select(r =>
            new XElement(S3Ns + "CORSRule",
                r.AllowedOrigins.Select(o =>
                    new XElement(S3Ns + "AllowedOrigin", o)),
                r.AllowedMethods.Select(m =>
                    new XElement(S3Ns + "AllowedMethod", m)),
                r.AllowedHeaders.Select(h =>
                    new XElement(S3Ns + "AllowedHeader", h)),
                r.ExposeHeaders.Select(h =>
                    new XElement(S3Ns + "ExposeHeader", h)),
                r.MaxAgeSeconds > 0
                    ? new XElement(S3Ns + "MaxAgeSeconds", r.MaxAgeSeconds)
                    : null));

        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(S3Ns + "CORSConfiguration", ruleElements));

        return XmlContent(xml);
    }

    private async Task<IActionResult> PutBucketCorsAsync(
        string bucket, CancellationToken cancellationToken)
    {
        ValidateBucketName(bucket);
        if (!Directory.Exists(ResolveBucketPath(bucket)))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        var doc = await XDocument.LoadAsync(Request.Body, LoadOptions.None, cancellationToken);
        var rules = doc.Descendants()
            .Where(e => e.Name.LocalName == "CORSRule")
            .Select(r => new CorsRule
            {
                AllowedOrigins = r.Elements()
                    .Where(e => e.Name.LocalName == "AllowedOrigin")
                    .Select(e => e.Value).ToList(),
                AllowedMethods = r.Elements()
                    .Where(e => e.Name.LocalName == "AllowedMethod")
                    .Select(e => e.Value).ToList(),
                AllowedHeaders = r.Elements()
                    .Where(e => e.Name.LocalName == "AllowedHeader")
                    .Select(e => e.Value).ToList(),
                ExposeHeaders = r.Elements()
                    .Where(e => e.Name.LocalName == "ExposeHeader")
                    .Select(e => e.Value).ToList(),
                MaxAgeSeconds = int.TryParse(
                    r.Elements().FirstOrDefault(e => e.Name.LocalName == "MaxAgeSeconds")?.Value,
                    out var max) ? max : 0,
            })
            .ToList();

        SaveBucketCors(bucket, rules);
        return Ok();
    }

    private IActionResult DeleteBucketCors(string bucket)
    {
        ValidateBucketName(bucket);
        if (!Directory.Exists(ResolveBucketPath(bucket)))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        DeleteBucketCorsFile(bucket);
        return NoContent();
    }

    // ================================================================
    //  Multipart upload
    // ================================================================

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
            StorageClass = Request.Headers["x-amz-storage-class"].ToString()
                is { Length: > 0 } sc ? sc : "STANDARD",
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

                uploads.Add(new XElement(S3Ns + "Upload",
                    new XElement(S3Ns + "Key", lines[1]),
                    new XElement(S3Ns + "UploadId", Path.GetFileName(dir)),
                    new XElement(S3Ns + "Initiator",
                        new XElement(S3Ns + "ID", "1"),
                        new XElement(S3Ns + "DisplayName", "owner")),
                    new XElement(S3Ns + "Owner",
                        new XElement(S3Ns + "ID", "1"),
                        new XElement(S3Ns + "DisplayName", "owner")),
                    new XElement(S3Ns + "StorageClass", "STANDARD"),
                    new XElement(S3Ns + "Initiated",
                        new DirectoryInfo(dir).CreationTimeUtc
                            .ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))));
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
    //  Object metadata persistence
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
    //  Bucket-level metadata persistence (tags, acl, cors)
    // ================================================================

    private string ResolveBucketMetaPath(string bucket, string suffix) =>
        Path.Combine(_metaBasePath, ".buckets", bucket + suffix);

    // --- Tags ---
    private void SaveBucketTags(string bucket, Dictionary<string, string> tags)
    {
        var path = ResolveBucketMetaPath(bucket, "-tags.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllText(path, JsonSerializer.Serialize(tags));
    }

    private Dictionary<string, string> LoadBucketTags(string bucket)
    {
        var path = ResolveBucketMetaPath(bucket, "-tags.json");
        if (!System.IO.File.Exists(path))
            return [];
        return JsonSerializer.Deserialize<Dictionary<string, string>>(
            System.IO.File.ReadAllText(path)) ?? [];
    }

    private void DeleteBucketTagsFile(string bucket)
    {
        var path = ResolveBucketMetaPath(bucket, "-tags.json");
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
    }

    // --- ACL ---
    private void SaveBucketAcl(string bucket, string cannedAcl)
    {
        var path = ResolveBucketMetaPath(bucket, "-acl.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllText(path, JsonSerializer.Serialize(cannedAcl));
    }

    private string LoadBucketAcl(string bucket)
    {
        var path = ResolveBucketMetaPath(bucket, "-acl.json");
        if (!System.IO.File.Exists(path))
            return "private";
        return JsonSerializer.Deserialize<string>(
            System.IO.File.ReadAllText(path)) ?? "private";
    }

    private void DeleteBucketAclFile(string bucket)
    {
        var path = ResolveBucketMetaPath(bucket, "-acl.json");
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
    }

    // --- CORS ---
    private void SaveBucketCors(string bucket, List<CorsRule> rules)
    {
        var path = ResolveBucketMetaPath(bucket, "-cors.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllText(path, JsonSerializer.Serialize(rules));
    }

    private List<CorsRule> LoadBucketCors(string bucket)
    {
        var path = ResolveBucketMetaPath(bucket, "-cors.json");
        if (!System.IO.File.Exists(path))
            return [];
        return JsonSerializer.Deserialize<List<CorsRule>>(
            System.IO.File.ReadAllText(path)) ?? [];
    }

    private void DeleteBucketCorsFile(string bucket)
    {
        var path = ResolveBucketMetaPath(bucket, "-cors.json");
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
    }

    // ================================================================
    //  Tagging XML helpers
    // ================================================================

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
                "NoSuchCORSConfiguration" => StatusCodes.Status404NotFound,
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
