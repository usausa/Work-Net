using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = new HostBuilder()
    .ConfigureServices((hostContext, services) =>
    {
        services.Configure<HostOptions>(options =>
        {
            //options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
            // options.ServicesStartConcurrently = true;
            // options.ServicesStopConcurrently = true;
        });
        services.AddHostedService<Worker>();
    });
await builder.RunConsoleAsync();

public class Worker : IHostedService
{
    private readonly IHostApplicationLifetime appLifetime;

    public Worker(IHostApplicationLifetime appLifetime)
    {
        this.appLifetime = appLifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("StartAsync");

        appLifetime.ApplicationStarted.Register(OnStarted);
        appLifetime.ApplicationStopping.Register(OnStopping);
        appLifetime.ApplicationStopped.Register(OnStopped);

        return Task.CompletedTask;
    }

    public void OnStarted()
    {
        Console.WriteLine("OnStarted");
    }

    public void OnStopping()
    {
        Console.WriteLine("OnStopping");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("StopAsync");
        return Task.CompletedTask;
    }

    public void OnStopped()
    {
        Console.WriteLine("OnStopped");
    }
}
