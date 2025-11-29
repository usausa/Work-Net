using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using WorkOllama;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        config.AddUserSecrets<Program>();
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<OllamaOptions>(context.Configuration.GetSection(OllamaOptions.SectionName));
        services.AddSingleton<IOllamaService, OllamaService>();
    });

var host = builder.Build();

var ollamaService = host.Services.GetRequiredService<IOllamaService>();

// モデル
var models = await ollamaService.GetAvailableModelsAsync();
Console.WriteLine("モデル:");
foreach (var model in models)
{
    Console.WriteLine($"  {model}");
}
Console.WriteLine();

// プロンプト
Console.Write("プロンプトを入力してください: ");
var prompt = Console.ReadLine();
if (!String.IsNullOrEmpty(prompt))
{
    await foreach (var token in ollamaService.GenerateStreamAsync(prompt))
    {
        Console.Write(token);
    }

    Console.ReadLine();
}
