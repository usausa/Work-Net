using System.Diagnostics;
using System.IO.Pipelines;
using System.Security.Cryptography;

var client = new HttpClient();
var response = await client.GetAsync("https://jsonplaceholder.typicode.com/todos/1");
var input = PipeReader.Create(await response.Content.ReadAsStreamAsync());

await using var stdout = Console.OpenStandardOutput();

using var incrementalSha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

while (true)
{
    var result = await input.ReadAsync();

    if (result.IsCompleted)
    {
        Debug.WriteLine("* Completed");
        break;
    }

    var buffer = result.Buffer;

    foreach (var memory in buffer)
    {
        await stdout.WriteAsync(memory);
        incrementalSha256.AppendData(memory.Span);
    }

    Debug.WriteLine($"* AdvanceTo {buffer.Length}");
    input.AdvanceTo(buffer.End);
}

await input.CompleteAsync();

var hash = GetBase64Hash(incrementalSha256);

Console.WriteLine();
Console.WriteLine($"Hash: {hash}");
Console.ReadKey();

static string GetBase64Hash(IncrementalHash incrementalHash)
{
    Span<byte> bytes = stackalloc byte[32];
    incrementalHash.GetHashAndReset(bytes);
    return Convert.ToBase64String(bytes);
}
