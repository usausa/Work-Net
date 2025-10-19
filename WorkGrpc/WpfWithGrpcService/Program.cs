using System.Windows;

using WpfWithGrpcService;
using WpfWithGrpcService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel((_, serverOptions) =>
{
    serverOptions.ListenAnyIP(5000, listenOptions =>
    {
        // listenOptions.UseHttps("certificate.pfx", "password");
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

builder.Services.AddSingleton<IHostLifetime, NoopConsoleLifetime>();

builder.Services.AddGrpc();

builder.Services.AddSingleton<App>();
builder.Services.AddHostedService<WpfHostingService>();

var app = builder.Build();

app.MapGrpcService<GreeterService>();

app.Run();

public class WpfHostingService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;

    private readonly IHostApplicationLifetime hostApplicationLifetime;

    private readonly TaskCompletionSource tcs = new();

    public WpfHostingService(IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime)
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
}

public class NoopConsoleLifetime : IHostLifetime
{
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
