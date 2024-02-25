namespace WorkExporterCustom;

using OpenTelemetry.Metrics;

using WorkExporterCustom.Metrics;

public class Worker : BackgroundService
{
    private readonly ApplicationMetrics metrics;

    public Worker(ApplicationMetrics metrics)
    {
        this.metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            metrics.IncrementCounter("test");

            await Task.Delay(1000, stoppingToken);
        }
    }
}
