using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using WorkHost;

Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

var builder = Host.CreateApplicationBuilder(args);

#if DEBUG
builder.Logging.AddDebug();
#endif

builder.Services.Configure<Settings>(builder.Configuration.GetSection("Settings"));

builder.Services.AddSingleton<App>();
builder.Services.AddSingleton<MainWindow>();

var host = builder.Build();

var log = host.Services.GetRequiredService<ILogger<Program>>();
var environment = host.Services.GetRequiredService<IHostEnvironment>();
log.LogInformation($"Environment. application=[{environment.ApplicationName}], environment=[{environment.EnvironmentName}], rootPath=[{environment.ContentRootPath}]");

var app = host.Services.GetRequiredService<App>();
app.Run();
