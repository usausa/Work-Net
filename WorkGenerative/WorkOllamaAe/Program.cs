using Microsoft.Extensions.AI;

using OllamaSharp;

var chatClient = new OllamaApiClient(new Uri("http://ollama:11434/"), "gpt-oss:20b");

while (true)
{
    Console.Write("Prompt: ");
    var prompt = Console.ReadLine();

    if (String.IsNullOrWhiteSpace(prompt))
    {
        break;
    }

    // GetStreamingResponseAsync is namespace Microsoft.Extensions.AI
    await foreach (var update in chatClient.GetStreamingResponseAsync(prompt))
    {
        Console.Write(update.Text);
    }

    Console.WriteLine();
}
