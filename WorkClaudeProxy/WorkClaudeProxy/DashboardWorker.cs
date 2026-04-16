using HidSharp;
using LcdDriver.TrofeoVision;

namespace WorkClaudeProxy;

internal sealed class DashboardWorker : BackgroundService
{
    private const int RetryDelaySeconds = 5;

    private readonly DashboardImageStore imageStore;
    private readonly ILogger<DashboardWorker> logger;

    public DashboardWorker(DashboardImageStore imageStore, ILogger<DashboardWorker> logger)
    {
        this.imageStore = imageStore;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
#pragma warning disable CA1031
            try
            {
                await RunDisplayLoopAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "LCD display error.");
            }
#pragma warning restore CA1031

            if (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task RunDisplayLoopAsync(CancellationToken stoppingToken)
    {
        var hidDevice = DeviceList.Local
            .GetHidDevices(ScreenDevice.VendorId, ScreenDevice.ProductId)
            .FirstOrDefault();
        if (hidDevice is null)
        {
            return;
        }

        using var screen = new ScreenDevice(hidDevice);

        DisplayState? lastRenderedState = null;
        byte[]? currentImage = null;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            var state = imageStore.GetState() ?? DisplayState.Empty;
            if (state != lastRenderedState)
            {
                try
                {
                    currentImage = DashboardRenderer.Render(state);
                    lastRenderedState = state;
                    await File.WriteAllBytesAsync(imageStore.OutputPath, currentImage, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogDebug("Dashboard render failed: {Error}", ex.Message);
                }
            }

            if (currentImage is not null)
            {
                screen.DrawJpeg(currentImage);
            }
        }
    }
}
