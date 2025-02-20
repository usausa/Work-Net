namespace WorkHost2;

using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal class WpfHostedService<TApp> : BackgroundService
    where TApp : Application
{
    private readonly IServiceProvider serviceProvider;

    private readonly IHostApplicationLifetime hostApplicationLifetime;

    private readonly TaskCompletionSource tcs = new();

    public WpfHostedService(IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime)
    {
        this.serviceProvider = serviceProvider;
        this.hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var thread = new Thread(() =>
        {
            var app = serviceProvider.GetRequiredService<App>();
            app.Run();
            tcs.SetResult();
            hostApplicationLifetime.StopApplication();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return tcs.Task;
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

