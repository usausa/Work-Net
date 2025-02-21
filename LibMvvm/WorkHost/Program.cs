using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using WorkHost;

Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

// TODO Folder

var builder = Host.CreateApplicationBuilder(args);

// TODO Log, Config, ...

builder.Services.AddSingleton<App>();
builder.Services.AddSingleton<MainWindow>();

var host = builder.Build();

var app = host.Services.GetRequiredService<App>();
app.Run();
