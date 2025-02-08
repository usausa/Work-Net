namespace WorkHost;

using System.Windows;

using Microsoft.Extensions.Hosting;

internal class WpfHostedService<TApp> : BackgroundService
    where TApp : Application
{
    private readonly TApp app;

    private readonly IHostApplicationLifetime hostApplicationLifetime;

    public WpfHostedService(TApp app, IHostApplicationLifetime hostApplicationLifetime)
    {
        this.app = app;
        this.hostApplicationLifetime = hostApplicationLifetime;

        app.Startup += AppOnStartup;
        app.Exit += AppOnExit;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // TODO with windows version
        app.Run();
        return Task.CompletedTask;
    }

    private void AppOnStartup(object sender, StartupEventArgs e)
    {
        // Nothing
    }

    private void AppOnExit(object sender, ExitEventArgs e)
    {
        hostApplicationLifetime.StopApplication();
    }
}

