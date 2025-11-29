namespace WorkOllama;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

using OllamaSharp;
using OllamaSharp.Models;

public class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = default!;

    public string Model { get; set; } = default!;

    public string? ApiKey { get; set; }
}

public interface IOllamaService
{
    IAsyncEnumerable<string> GenerateStreamAsync(string prompt, CancellationToken cancellationToken = default);

    Task<IEnumerable<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
}

public class OllamaService : IOllamaService
{
    private readonly OllamaApiClient client;

    private readonly OllamaOptions options;

    public OllamaService(IOptions<OllamaOptions> options)
    {
        this.options = options.Value;

        var uri = new Uri(this.options.BaseUrl);
        client = new OllamaApiClient(uri)
        {
            SelectedModel = this.options.Model
        };
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new GenerateRequest
        {
            Prompt = prompt,
            Model = options.Model,
            Stream = true
        };

        await foreach (var response in client.GenerateAsync(request, cancellationToken))
        {
            if (!string.IsNullOrEmpty(response?.Response))
            {
                yield return response.Response;
            }

            if (response?.Done == true)
            {
                break;
            }
        }
    }

    public async Task<IEnumerable<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        var models = await client.ListLocalModelsAsync(cancellationToken);
        return models.Select(m => m.Name);
    }
}
