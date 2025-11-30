using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using OllamaSharp;

using WorkAeOllama;

var builder = Host.CreateApplicationBuilder();

builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));
builder.Services.AddChatClient(p =>
{
    var option = p.GetRequiredService<IOptions<OllamaOptions>>().Value;
    return new OllamaApiClient(new Uri(option.BaseUrl), option.Model);

});

var app = builder.Build();

var chatClient = app.Services.GetRequiredService<IChatClient>();

var response = await chatClient.GetResponseAsync("What is .NET? Reply in 50 words max.");

foreach (var message in response.Messages)
{
    Console.WriteLine(message.Text);
}

Console.ReadLine();
