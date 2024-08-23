using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = new HostBuilder()
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true);
        config.AddEnvironmentVariables();
        config.AddCommandLine(args);
    })
    .ConfigureLogging((hostingContext, logging) =>
    {
        logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
        logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
        });
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddOptions();
        services.Configure<AppSettings>(hostContext.Configuration.GetSection("App"));

        services.Configure<HostOptions>(options =>
        {
            //options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
            // options.ServicesStartConcurrently = true;
            // options.ServicesStopConcurrently = true;
        });
        services.AddHostedService<Worker>();
    });
await builder.RunConsoleAsync();

public class AppSettings
{
    public string Message { get; set; } = default!;
}

public class Worker : IHostedService
{
    private readonly ILogger<Worker> log;

    private readonly IHostApplicationLifetime appLifetime;

    private readonly AppSettings settings;

    public Worker(
        ILogger<Worker> log,
        IHostApplicationLifetime appLifetime,
        IOptions<AppSettings> settings)
    {
        this.log = log;
        this.appLifetime = appLifetime;
        this.settings = settings.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        log.LogInformation("StartAsync. {Message}", settings.Message);

        appLifetime.ApplicationStarted.Register(OnStarted);
        appLifetime.ApplicationStopping.Register(OnStopping);
        appLifetime.ApplicationStopped.Register(OnStopped);

        return Task.CompletedTask;
    }

    public void OnStarted()
    {
        log.LogInformation("OnStarted.");
    }

    public void OnStopping()
    {
        log.LogInformation("OnStopping.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        log.LogInformation("StopAsync.");
        return Task.CompletedTask;
    }

    public void OnStopped()
    {
        log.LogInformation("OnStopped.");
    }
}
