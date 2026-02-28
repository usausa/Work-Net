using System.Security.Cryptography;
using System.Xml.Linq;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace WorkStorage.Controllers;

/// <summary>
/// Provides AWS S3-compatible REST API endpoints backed by local file system storage.
/// Authentication is not enforced.
/// </summary>
[ApiController]
public sealed class S3Controller : ControllerBase
{
    private static readonly XNamespace S3Ns = "http://s3.amazonaws.com/doc/2006-03-01/";
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    private readonly string _basePath;

    public S3Controller(IConfiguration configuration)
    {
        var configured = configuration["Storage:BasePath"]
            ?? throw new InvalidOperationException("Storage:BasePath is not configured.");
        _basePath = Path.GetFullPath(configured);
    }

    // ----------------------------------------------------------------
    //  Bucket operations
    // ----------------------------------------------------------------

    /// <summary>
    /// GET / – List all buckets.
    /// </summary>
    [HttpGet("/")]
    public IActionResult ListBuckets()
    {
        EnsureBaseDirectory();

        var buckets = Directory.GetDirectories(_basePath)
            .Select(d => new DirectoryInfo(d))
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
    /// PUT /{bucket} – Create a bucket.
    /// </summary>
    [HttpPut("/{bucket}")]
    public IActionResult CreateBucket(string bucket)
    {
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
    /// DELETE /{bucket} – Delete a bucket and all its contents.
    /// </summary>
    [HttpDelete("/{bucket}")]
    public IActionResult DeleteBucket(string bucket)
    {
        ValidateBucketName(bucket);
        var bucketPath = ResolveBucketPath(bucket);
        if (!Directory.Exists(bucketPath))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        Directory.Delete(bucketPath, recursive: true);
        return NoContent();
    }

    /// <summary>
    /// GET /{bucket} – List objects in a bucket (supports ListObjectsV2 query parameters).
    /// </summary>
    [HttpGet("/{bucket}")]
    public IActionResult ListObjects(
        string bucket,
        [FromQuery] string? prefix,
        [FromQuery(Name = "max-keys")] int maxKeys = 1000)
    {
        ValidateBucketName(bucket);
        var bucketPath = ResolveBucketPath(bucket);
        if (!Directory.Exists(bucketPath))
            return S3Error("NoSuchBucket", "The specified bucket does not exist");

        prefix ??= string.Empty;

        var entries = Directory.GetFiles(bucketPath, "*", SearchOption.AllDirectories)
            .Select(f =>
            {
                var key = Path.GetRelativePath(bucketPath, f).Replace('\\', '/');
                return new { Key = key, Info = new FileInfo(f) };
            })
            .Where(x => x.Key.StartsWith(prefix, StringComparison.Ordinal))
            .Take(maxKeys)
            .Select(x => new XElement(S3Ns + "Contents",
                new XElement(S3Ns + "Key", x.Key),
                new XElement(S3Ns + "LastModified",
                    x.Info.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                new XElement(S3Ns + "ETag", $"\"{ComputeETag(x.Info)}\""),
                new XElement(S3Ns + "Size", x.Info.Length),
                new XElement(S3Ns + "StorageClass", "STANDARD")))
            .ToList();

        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(S3Ns + "ListBucketResult",
                new XElement(S3Ns + "Name", bucket),
                new XElement(S3Ns + "Prefix", prefix),
                new XElement(S3Ns + "KeyCount", entries.Count),
                new XElement(S3Ns + "MaxKeys", maxKeys),
                new XElement(S3Ns + "IsTruncated", "false"),
                entries));

        return XmlContent(xml);
    }

    // ----------------------------------------------------------------
    //  Object operations
    // ----------------------------------------------------------------

    /// <summary>
    /// PUT /{bucket}/{**key} – Upload an object.
    /// </summary>
    [HttpPut("/{bucket}/{**key}")]
    public async Task<IActionResult> PutObjectAsync(
        string bucket, string key, CancellationToken cancellationToken)
    {
        ValidateBucketName(bucket);
        ValidateObjectKey(key);
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

        Response.Headers.ETag = $"\"{ComputeETag(new FileInfo(filePath))}\"";
        return Ok();
    }

    /// <summary>
    /// GET /{bucket}/{**key} – Download an object.
    /// </summary>
    [HttpGet("/{bucket}/{**key}")]
    public IActionResult GetObject(string bucket, string key)
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
        Response.Headers.ETag = $"\"{ComputeETag(info)}\"";
        Response.Headers["Last-Modified"] = info.LastWriteTimeUtc.ToString("R");
        return PhysicalFile(filePath, ResolveContentType(key));
    }

    /// <summary>
    /// HEAD /{bucket}/{**key} – Retrieve object metadata.
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
        Response.Headers.ETag = $"\"{ComputeETag(info)}\"";
        Response.Headers["Last-Modified"] = info.LastWriteTimeUtc.ToString("R");
        Response.Headers.ContentLength = info.Length;
        Response.Headers.ContentType = ResolveContentType(key);
        return Ok();
    }

    /// <summary>
    /// DELETE /{bucket}/{**key} – Delete an object.
    /// </summary>
    [HttpDelete("/{bucket}/{**key}")]
    public IActionResult DeleteObject(string bucket, string key)
    {
        ValidateBucketName(bucket);
        ValidateObjectKey(key);

        var filePath = ResolveObjectPath(bucket, key);
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        return NoContent();
    }

    // ----------------------------------------------------------------
    //  Helpers
    // ----------------------------------------------------------------

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
        if (bucket.Contains("..") || bucket.Contains('/') || bucket.Contains('\\'))
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
