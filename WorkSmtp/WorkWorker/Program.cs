using SmtpServer.Storage;
using WorkWorker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton<IMessageStore, CustomMessageStore>();

var host = builder.Build();
host.Run();
