using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

const string serviceUrl = "http://localhost:5128";
const string bucketName = "test-bucket";
const string objectKey = "hello.txt";
const string objectContent = "Hello from S3-compatible client!";

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

// 2. Upload object
Console.WriteLine($"Uploading object: {objectKey}");
await client.PutObjectAsync(new PutObjectRequest
{
    BucketName = bucketName,
    Key = objectKey,
    ContentBody = objectContent,
});
Console.WriteLine("  -> Done");

// 3. List objects
Console.WriteLine($"Listing objects in {bucketName}:");
var listResponse = await client.ListObjectsV2Async(new ListObjectsV2Request
{
    BucketName = bucketName,
});
foreach (var obj in listResponse.S3Objects)
{
    Console.WriteLine($"  - {obj.Key} ({obj.Size} bytes)");
}

// 4. Download object
Console.WriteLine($"Downloading object: {objectKey}");
var getResponse = await client.GetObjectAsync(bucketName, objectKey);
using (var reader = new StreamReader(getResponse.ResponseStream))
{
    var content = await reader.ReadToEndAsync();
    Console.WriteLine($"  -> Content: {content}");
}

// 5. Delete object
Console.WriteLine($"Deleting object: {objectKey}");
await client.DeleteObjectAsync(bucketName, objectKey);
Console.WriteLine("  -> Done");

// 6. Delete bucket
Console.WriteLine($"Deleting bucket: {bucketName}");
await client.DeleteBucketAsync(bucketName);
Console.WriteLine("  -> Done");

Console.WriteLine("All operations completed successfully.");
