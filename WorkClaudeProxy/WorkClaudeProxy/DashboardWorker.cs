namespace WorkClaudeProxy;

internal sealed class DashboardWorker : BackgroundService
{
    private readonly DashboardImageStore imageStore;

    public DashboardWorker(DashboardImageStore imageStore)
    {
        this.imageStore = imageStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            var image = imageStore.GetImage();
            // TODO: send image to LCD display
        }
    }
}
