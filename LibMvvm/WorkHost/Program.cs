using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using WorkHost;

Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

// TODO Folder

var builder = Host.CreateApplicationBuilder(args);


// TODO Log, Config, ...
#if DEBUG
builder.Logging.AddDebug();
#endif

builder.Services.Configure<Settings>(builder.Configuration.GetSection("Settings"));

builder.Services.AddSingleton<App>();
builder.Services.AddSingleton<MainWindow>();

var host = builder.Build();

var app = host.Services.GetRequiredService<App>();
app.Run();
