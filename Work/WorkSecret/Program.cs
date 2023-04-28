using System.Diagnostics;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

using WorkSecret;

// dotnet user-secrets init
// dotnet user-secrets set "Name" "うさうさ"
// %APPDATA%\Microsoft\UserSecrets\

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging((_, logging) =>
    {
        logging.ClearProviders();
    })
    .UseSerilog((hostingContext, loggerConfiguration) =>
    {
        loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
    })
    .ConfigureAppConfiguration((context, builder) =>
    {
        if (context.HostingEnvironment.IsDevelopment())
        {
            builder.AddUserSecrets<Program>();
        }
    })
    .ConfigureHostOptions((context, option) =>
    {
        var config = context.Configuration;
        var name = config.GetSection("Name").Value;
        Debug.WriteLine(name);

        var connectionString = config.GetConnectionString("Default");
        Debug.WriteLine(connectionString);

        var settings = config.GetSection("Settings").Get<Settings>()!;
        Debug.WriteLine($"{settings.Address} {settings.Port}");
    })
    .Build();

host.Run();