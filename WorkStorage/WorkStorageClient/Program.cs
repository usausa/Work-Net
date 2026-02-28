using System.Net;
using System.Text;

using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

const string serviceUrl = "http://localhost:5128";
const string bucketName = "test-bucket";

var config = new AmazonS3Config
{
    ServiceURL = serviceUrl,
    ForcePathStyle = true,
};

using var client = new AmazonS3Client(
    new BasicAWSCredentials("test", "test"), config);

// ── 1. Create bucket ────────────────────────────────────────────
Console.WriteLine("=== Create Bucket ===");
await client.PutBucketAsync(bucketName);
Console.WriteLine($"  Created: {bucketName}");

// ── 2. Upload objects with hierarchical keys and metadata ────────
Console.WriteLine("\n=== Upload Objects ===");
string[] keys =
[
    "readme.txt",
    "docs/guide.txt",
    "docs/api/reference.txt",
    "images/logo.png",
    "images/icons/favicon.ico",
];
foreach (var key in keys)
{
    var req = new PutObjectRequest
    {
        BucketName = bucketName,
        Key = key,
        ContentBody = $"Content of {key}",
        ContentType = "text/plain",
    };
    req.Metadata["author"] = "demo-user";
    await client.PutObjectAsync(req);
    Console.WriteLine($"  Uploaded: {key}");
}

// ── 3. List all objects (flat) ──────────────────────────────────
Console.WriteLine("\n=== List All Objects (flat) ===");
var flatList = await client.ListObjectsV2Async(
    new ListObjectsV2Request { BucketName = bucketName });
foreach (var obj in flatList.S3Objects)
    Console.WriteLine($"  {obj.Key} ({obj.Size} bytes)");

// ── 4. List with delimiter (hierarchy browsing) ─────────────────
Console.WriteLine("\n=== Root Level (delimiter='/') ===");
var rootList = await client.ListObjectsV2Async(
    new ListObjectsV2Request { BucketName = bucketName, Delimiter = "/" });
foreach (var cp in rootList.CommonPrefixes)
    Console.WriteLine($"  [DIR]  {cp}");
foreach (var obj in rootList.S3Objects)
    Console.WriteLine($"  [FILE] {obj.Key}");

// ── 5. Pagination (MaxKeys=2) ───────────────────────────────────
Console.WriteLine("\n=== Pagination (MaxKeys=2) ===");
string? continuationToken = null;
var page = 1;
do
{
    var resp = await client.ListObjectsV2Async(new ListObjectsV2Request
    {
        BucketName = bucketName,
        MaxKeys = 2,
        ContinuationToken = continuationToken,
    });
    Console.WriteLine($"  Page {page}: " +
        $"{string.Join(", ", resp.S3Objects.Select(o => o.Key))} " +
        $"(IsTruncated={resp.IsTruncated})");
    continuationToken = resp.IsTruncated == true ? resp.NextContinuationToken : null;
    page++;
} while (continuationToken is not null);

// ── 6. CopyObject ───────────────────────────────────────────────
Console.WriteLine("\n=== Copy Object ===");
await client.CopyObjectAsync(new CopyObjectRequest
{
    SourceBucket = bucketName,
    SourceKey = "readme.txt",
    DestinationBucket = bucketName,
    DestinationKey = "backup/readme-copy.txt",
});
Console.WriteLine("  Copied readme.txt -> backup/readme-copy.txt");
var copyGet = await client.GetObjectAsync(bucketName, "backup/readme-copy.txt");
using (var reader = new StreamReader(copyGet.ResponseStream))
    Console.WriteLine($"  Content: {await reader.ReadToEndAsync()}");

// ── 7. Content-Type and user-defined metadata ───────────────────
Console.WriteLine("\n=== Content-Type & User Metadata ===");

// Upload with explicit Content-Type and custom metadata
var metaReq = new PutObjectRequest
{
    BucketName = bucketName,
    Key = "data/config.json",
    ContentBody = """{"setting": true}""",
    ContentType = "application/json",
};
metaReq.Metadata["project"] = "work-storage";
metaReq.Metadata["version"] = "1.0";
await client.PutObjectAsync(metaReq);
Console.WriteLine("  Uploaded data/config.json with custom metadata");

// HEAD to verify metadata is returned
var metaHead = await client.GetObjectMetadataAsync(bucketName, "data/config.json");
Console.WriteLine($"  ContentType : {metaHead.Headers.ContentType}");
Console.WriteLine($"  x-amz-meta-project: {metaHead.Metadata["project"]}");
Console.WriteLine($"  x-amz-meta-version: {metaHead.Metadata["version"]}");

// Copy with COPY directive (metadata preserved)
await client.CopyObjectAsync(new CopyObjectRequest
{
    SourceBucket = bucketName,
    SourceKey = "data/config.json",
    DestinationBucket = bucketName,
    DestinationKey = "data/config-copy.json",
});
var copyMeta = await client.GetObjectMetadataAsync(bucketName, "data/config-copy.json");
Console.WriteLine($"  COPY directive  -> ContentType: {copyMeta.Headers.ContentType}" +
    $", project={copyMeta.Metadata["project"]}");

// Copy with REPLACE directive (new metadata)
var replaceReq = new CopyObjectRequest
{
    SourceBucket = bucketName,
    SourceKey = "data/config.json",
    DestinationBucket = bucketName,
    DestinationKey = "data/config-replaced.json",
    MetadataDirective = S3MetadataDirective.REPLACE,
    ContentType = "text/yaml",
};
replaceReq.Metadata["project"] = "new-project";
await client.CopyObjectAsync(replaceReq);
var replacedMeta = await client.GetObjectMetadataAsync(bucketName, "data/config-replaced.json");
Console.WriteLine($"  REPLACE directive -> ContentType: {replacedMeta.Headers.ContentType}" +
    $", project={replacedMeta.Metadata["project"]}");

// ── 8. Range request (partial download) ─────────────────────────
Console.WriteLine("\n=== Range Request ===");
var rangeResp = await client.GetObjectAsync(new GetObjectRequest
{
    BucketName = bucketName,
    Key = "readme.txt",
    ByteRange = new ByteRange(0, 6),
});
using (var reader = new StreamReader(rangeResp.ResponseStream))
    Console.WriteLine($"  Bytes 0-6: \"{await reader.ReadToEndAsync()}\"");

// ── 9. Conditional request (If-None-Match → 304) ────────────────
Console.WriteLine("\n=== Conditional Request ===");
var headResp = await client.GetObjectMetadataAsync(bucketName, "readme.txt");
Console.WriteLine($"  Current ETag: {headResp.ETag}");
try
{
    await client.GetObjectAsync(new GetObjectRequest
    {
        BucketName = bucketName,
        Key = "readme.txt",
        EtagToNotMatch = headResp.ETag,
    });
    Console.WriteLine("  Unexpected: should have returned 304");
}
catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotModified)
{
    Console.WriteLine("  304 Not Modified (as expected)");
}

// ── 10. Multipart upload ────────────────────────────────────────
Console.WriteLine("\n=== Multipart Upload ===");
var initResp = await client.InitiateMultipartUploadAsync(
    new InitiateMultipartUploadRequest
    {
        BucketName = bucketName,
        Key = "large-file.dat",
    });
var uploadId = initResp.UploadId;
Console.WriteLine($"  UploadId: {uploadId}");

var partETags = new List<PartETag>();
for (var i = 1; i <= 3; i++)
{
    var partData = Encoding.UTF8.GetBytes($"[Part-{i} data with some padding...]");
    using var ms = new MemoryStream(partData);
    var partResp = await client.UploadPartAsync(new UploadPartRequest
    {
        BucketName = bucketName,
        Key = "large-file.dat",
        UploadId = uploadId,
        PartNumber = i,
        InputStream = ms,
    });
    partETags.Add(new PartETag(i, partResp.ETag));
    Console.WriteLine($"  Part {i} uploaded, ETag: {partResp.ETag}");
}

await client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
{
    BucketName = bucketName,
    Key = "large-file.dat",
    UploadId = uploadId,
    PartETags = partETags,
});
Console.WriteLine("  Multipart upload completed");

var mpGet = await client.GetObjectAsync(bucketName, "large-file.dat");
using (var reader = new StreamReader(mpGet.ResponseStream))
    Console.WriteLine($"  Content: {await reader.ReadToEndAsync()}");

// ── 11. Object tagging ──────────────────────────────────────────
Console.WriteLine("\n=== Object Tagging ===");
await client.PutObjectTaggingAsync(new PutObjectTaggingRequest
{
    BucketName = bucketName,
    Key = "readme.txt",
    Tagging = new Tagging
    {
        TagSet =
        [
            new Tag { Key = "environment", Value = "dev" },
            new Tag { Key = "team", Value = "backend" },
        ],
    },
});
Console.WriteLine("  Tags set on readme.txt");

var objTags = await client.GetObjectTaggingAsync(
    new GetObjectTaggingRequest { BucketName = bucketName, Key = "readme.txt" });
foreach (var tag in objTags.Tagging)
    Console.WriteLine($"  {tag.Key} = {tag.Value}");

await client.DeleteObjectTaggingAsync(
    new DeleteObjectTaggingRequest { BucketName = bucketName, Key = "readme.txt" });
Console.WriteLine("  Tags deleted from readme.txt");

// ── 12. Bucket tagging ──────────────────────────────────────────
Console.WriteLine("\n=== Bucket Tagging ===");
await client.PutBucketTaggingAsync(new PutBucketTaggingRequest
{
    BucketName = bucketName,
    TagSet =
    [
        new Tag { Key = "project", Value = "work-storage" },
        new Tag { Key = "cost-center", Value = "engineering" },
    ],
});
Console.WriteLine("  Tags set on bucket");

var bucketTags = await client.GetBucketTaggingAsync(
    new GetBucketTaggingRequest { BucketName = bucketName });
foreach (var tag in bucketTags.TagSet)
    Console.WriteLine($"  {tag.Key} = {tag.Value}");

await client.DeleteBucketTaggingAsync(bucketName);
Console.WriteLine("  Tags deleted from bucket");

// ── 13. ListMultipartUploads / ListParts ────────────────────────
Console.WriteLine("\n=== ListMultipartUploads / ListParts ===");
var mpInit = await client.InitiateMultipartUploadAsync(
    new InitiateMultipartUploadRequest
    {
        BucketName = bucketName,
        Key = "pending-upload.dat",
    });

var partBytes = Encoding.UTF8.GetBytes("test-part-data");
using (var ms = new MemoryStream(partBytes))
{
    await client.UploadPartAsync(new UploadPartRequest
    {
        BucketName = bucketName,
        Key = "pending-upload.dat",
        UploadId = mpInit.UploadId,
        PartNumber = 1,
        InputStream = ms,
    });
}

var uploads = await client.ListMultipartUploadsAsync(
    new ListMultipartUploadsRequest { BucketName = bucketName });
foreach (var u in uploads.MultipartUploads)
    Console.WriteLine($"  Upload: key={u.Key}, uploadId={u.UploadId}");

var partsResp = await client.ListPartsAsync(new ListPartsRequest
{
    BucketName = bucketName,
    Key = "pending-upload.dat",
    UploadId = mpInit.UploadId,
});
foreach (var p in partsResp.Parts)
    Console.WriteLine($"  Part {p.PartNumber}: {p.Size} bytes, ETag={p.ETag}");

await client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
{
    BucketName = bucketName,
    Key = "pending-upload.dat",
    UploadId = mpInit.UploadId,
});
Console.WriteLine("  Multipart upload aborted");

// ── 14. Storage Class ───────────────────────────────────────────
Console.WriteLine("\n=== Storage Class ===");
await client.PutObjectAsync(new PutObjectRequest
{
    BucketName = bucketName,
    Key = "archive/old-data.txt",
    ContentBody = "archived content",
    StorageClass = S3StorageClass.StandardInfrequentAccess,
});
Console.WriteLine("  Uploaded archive/old-data.txt with STANDARD_IA");
var scHead = await client.GetObjectMetadataAsync(bucketName, "archive/old-data.txt");
Console.WriteLine($"  StorageClass from HEAD: {scHead.StorageClass}");

var scList = await client.ListObjectsV2Async(new ListObjectsV2Request
{
    BucketName = bucketName,
    Prefix = "archive/",
});
foreach (var o in scList.S3Objects)
    Console.WriteLine($"  {o.Key} -> StorageClass: {o.StorageClass}");

// ── 15. ACL ─────────────────────────────────────────────────────
Console.WriteLine("\n=== ACL ===");
var bucketAcl = await client.GetACLAsync(new GetACLRequest
{
    BucketName = bucketName,
});
Console.WriteLine("  Bucket ACL:");
foreach (var grant in bucketAcl.AccessControlList.Grants)
    Console.WriteLine($"    {grant.Grantee.Type}: {grant.Permission}");

await client.PutACLAsync(new PutACLRequest
{
    BucketName = bucketName,
    Key = "readme.txt",
    CannedACL = S3CannedACL.PublicRead,
});
Console.WriteLine("  Set readme.txt ACL to public-read");

var objAcl = await client.GetACLAsync(new GetACLRequest
{
    BucketName = bucketName,
    Key = "readme.txt",
});
Console.WriteLine("  Object ACL:");
foreach (var grant in objAcl.AccessControlList.Grants)
    Console.WriteLine($"    {grant.Grantee.Type}: {grant.Permission}");

// ── 16. Bucket CORS ─────────────────────────────────────────────
Console.WriteLine("\n=== Bucket CORS ===");
await client.PutCORSConfigurationAsync(new PutCORSConfigurationRequest
{
    BucketName = bucketName,
    Configuration = new CORSConfiguration
    {
        Rules =
        [
            new CORSRule
            {
                AllowedOrigins = ["*"],
                AllowedMethods = ["GET", "PUT", "POST", "DELETE"],
                AllowedHeaders = ["*"],
                ExposeHeaders = ["ETag", "x-amz-meta-*"],
                MaxAgeSeconds = 3600,
            },
        ],
    },
});
Console.WriteLine("  CORS configuration set");

var corsResp = await client.GetCORSConfigurationAsync(
    new GetCORSConfigurationRequest { BucketName = bucketName });
foreach (var rule in corsResp.Configuration.Rules)
{
    Console.WriteLine($"  Origins: {string.Join(", ", rule.AllowedOrigins)}");
    Console.WriteLine($"  Methods: {string.Join(", ", rule.AllowedMethods)}");
}

await client.DeleteCORSConfigurationAsync(
    new DeleteCORSConfigurationRequest { BucketName = bucketName });
Console.WriteLine("  CORS configuration deleted");

// ── 17. DeleteObjects (bulk delete) ─────────────────────────────
Console.WriteLine("\n=== Bulk Delete ===");
var allObjs = await client.ListObjectsV2Async(
    new ListObjectsV2Request { BucketName = bucketName });
var delResp = await client.DeleteObjectsAsync(new DeleteObjectsRequest
{
    BucketName = bucketName,
    Objects = allObjs.S3Objects.Select(o => new KeyVersion { Key = o.Key }).ToList(),
});
Console.WriteLine($"  Deleted {delResp.DeletedObjects.Count} objects");

// ── 18. Cleanup ─────────────────────────────────────────────────
await client.DeleteBucketAsync(bucketName);
Console.WriteLine($"  Bucket deleted: {bucketName}");

// ── 19. Virtual-hosted style access ─────────────────────────────
// Requires *.s3.localhost to resolve to 127.0.0.1.
// Most modern browsers and OS resolvers handle *.localhost automatically.
Console.WriteLine("\n=== Virtual-Hosted Style ===");
var vhConfig = new AmazonS3Config
{
    ServiceURL = "http://s3.localhost:5128",
    ForcePathStyle = false,
};
using var vhClient = new AmazonS3Client(
    new BasicAWSCredentials("test", "test"), vhConfig);

const string vhBucket = "vh-demo";
await vhClient.PutBucketAsync(vhBucket);
Console.WriteLine($"  Created bucket: {vhBucket}");

await vhClient.PutObjectAsync(new PutObjectRequest
{
    BucketName = vhBucket,
    Key = "hello.txt",
    ContentBody = "Hello from virtual-hosted style!",
});
Console.WriteLine("  Uploaded hello.txt via virtual-hosted style");

var vhGet = await vhClient.GetObjectAsync(vhBucket, "hello.txt");
using (var reader = new StreamReader(vhGet.ResponseStream))
    Console.WriteLine($"  Content: {await reader.ReadToEndAsync()}");

var vhList = await vhClient.ListObjectsV2Async(
    new ListObjectsV2Request { BucketName = vhBucket });
foreach (var obj in vhList.S3Objects)
    Console.WriteLine($"  Listed: {obj.Key} ({obj.Size} bytes)");

await vhClient.DeleteObjectAsync(vhBucket, "hello.txt");
await vhClient.DeleteBucketAsync(vhBucket);
Console.WriteLine($"  Bucket deleted: {vhBucket}");

Console.WriteLine("\n=== All operations completed successfully ===");
