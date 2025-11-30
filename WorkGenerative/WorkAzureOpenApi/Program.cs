using System.Reflection;

using Azure;
using Azure.AI.OpenAI;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using OpenAI.Chat;

using WorkAzureOpenApi;

var builder = Host.CreateApplicationBuilder();

builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddSingleton(p =>
{
    var option = p.GetRequiredService<IOptions<OpenAIOptions>>().Value;
    return new AzureOpenAIClient(new Uri(option.EndPoint), new AzureKeyCredential(option.ApiKey));
});
builder.Services.AddSingleton(p =>
{
    var option = p.GetRequiredService<IOptions<OpenAIOptions>>().Value;
    var azureClient = p.GetRequiredService<AzureOpenAIClient>();
    return azureClient.GetChatClient(option.DeploymentName);

});

var app = builder.Build();

var chatClient = app.Services.GetRequiredService<ChatClient>();
var messages = new List<ChatMessage>()
{
    new SystemChatMessage("貴方はC#プログラマーです"),
    new UserChatMessage("Microsoft.Extensions.AI.OpenAIを使ったサンプルコードを書いてください"),
};

var response = chatClient.CompleteChat(messages);

Console.WriteLine(response.Value.Content[0].Text);
Console.ReadLine();
