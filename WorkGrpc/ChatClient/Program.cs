using ChatServer;

using Grpc.Core;
using Grpc.Net.Client;

Console.Write("Enter name: ");
var name = Console.ReadLine() ?? "Anonymous";

using var channel = GrpcChannel.ForAddress("http://localhost:5000");
var client = new ChatApi.ChatApiClient(channel);

using var chat = client.SendMessage();

// Receiver
var readerTask = Task.Run(async () =>
{
    try
    {
        // ReSharper disable once AccessToDisposedClosure
        await foreach (var message in chat.ResponseStream.ReadAllAsync())
        {
            var timestamp = message.Timestamp.ToDateTime().ToLocalTime();
            Console.WriteLine($"[{timestamp:HH:mm:ss}] {message.Name}: {message.Message}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unknown exception: {ex.Message}");
    }
});

// Sender
try
{
    while (true)
    {
        var input = Console.ReadLine();

        if (String.IsNullOrEmpty(input))
        {
            continue;
        }

        if (input.ToLower() == "exit")
        {
            break;
        }

        await chat.RequestStream.WriteAsync(new ChatMessage { Name = name, Message = input });
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Unknown exception: {ex.Message}");
}
finally
{
    await chat.RequestStream.CompleteAsync();
}

await readerTask;
