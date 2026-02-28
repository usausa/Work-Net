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

// 1. Create bucket
Console.WriteLine($"Creating bucket: {bucketName}");
await client.PutBucketAsync(bucketName);
Console.WriteLine("  -> Done");

// 2. Upload objects with hierarchical keys
string[] keys = ["readme.txt", "docs/guide.txt", "docs/api/reference.txt", "images/logo.png", "images/icons/favicon.ico"];
foreach (var key in keys)
{
    Console.WriteLine($"Uploading: {key}");
    await client.PutObjectAsync(new PutObjectRequest
    {
        BucketName = bucketName,
        Key = key,
        ContentBody = $"Content of {key}",
    });
}
Console.WriteLine("  -> All uploads done");

// 3. List all objects (flat)
Console.WriteLine("\n--- All objects (flat) ---");
var flatList = await client.ListObjectsV2Async(new ListObjectsV2Request
{
    BucketName = bucketName,
});
foreach (var obj in flatList.S3Objects)
{
    Console.WriteLine($"  {obj.Key} ({obj.Size} bytes)");
}

// 4. List root level with delimiter (directory browsing)
Console.WriteLine("\n--- Root level (delimiter='/') ---");
var rootList = await client.ListObjectsV2Async(new ListObjectsV2Request
{
    BucketName = bucketName,
    Delimiter = "/",
});
foreach (var cp in rootList.CommonPrefixes)
    Console.WriteLine($"  [DIR]  {cp}");
foreach (var obj in rootList.S3Objects)
    Console.WriteLine($"  [FILE] {obj.Key} ({obj.Size} bytes)");

// 5. Browse into docs/ subdirectory
Console.WriteLine("\n--- docs/ (delimiter='/') ---");
var docsList = await client.ListObjectsV2Async(new ListObjectsV2Request
{
    BucketName = bucketName,
    Prefix = "docs/",
    Delimiter = "/",
});
foreach (var cp in docsList.CommonPrefixes)
    Console.WriteLine($"  [DIR]  {cp}");
foreach (var obj in docsList.S3Objects)
    Console.WriteLine($"  [FILE] {obj.Key} ({obj.Size} bytes)");

// 6. Browse into docs/api/ subdirectory
Console.WriteLine("\n--- docs/api/ (delimiter='/') ---");
var apiList = await client.ListObjectsV2Async(new ListObjectsV2Request
{
    BucketName = bucketName,
    Prefix = "docs/api/",
    Delimiter = "/",
});
foreach (var obj in apiList.S3Objects)
    Console.WriteLine($"  [FILE] {obj.Key} ({obj.Size} bytes)");

// 7. Download a nested object
Console.WriteLine("\n--- Download docs/api/reference.txt ---");
var getResponse = await client.GetObjectAsync(bucketName, "docs/api/reference.txt");
using (var reader = new StreamReader(getResponse.ResponseStream))
{
    Console.WriteLine($"  -> Content: {await reader.ReadToEndAsync()}");
}

// 8. Delete all objects then bucket
Console.WriteLine("\n--- Cleanup ---");
foreach (var key in keys)
{
    Console.WriteLine($"Deleting: {key}");
    await client.DeleteObjectAsync(bucketName, key);
}
await client.DeleteBucketAsync(bucketName);
Console.WriteLine("  -> Bucket deleted");

Console.WriteLine("\nAll operations completed successfully.");
