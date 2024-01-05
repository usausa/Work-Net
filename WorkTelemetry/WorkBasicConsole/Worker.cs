namespace WorkBasicConsole;

using System.Diagnostics.Metrics;

public class Worker : BackgroundService
{
    // counter
    private readonly Counter<int> counter;
    // gauge
    private readonly UpDownCounter<int> updown;
    // histogram
    private readonly Histogram<int> histogram;

    public Worker(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Work.Basic", "1.0.0");
        counter = meter.CreateCounter<int>("worker.counter", "point");
        updown = meter.CreateUpDownCounter<int>("worker.updown");
        histogram = meter.CreateHistogram<int>("worker.histogram");

        meter.CreateObservableCounter("worker.observable.counter", () => DateTime.Now.Ticks);
        meter.CreateObservableGauge("worker.observable.gauge", () => DateTime.Now.Second);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);

            counter.Add(1);
            updown.Add(DateTime.Now.Minute % 2 == 0 ? 1 : -1);
            histogram.Record(DateTime.Now.Second);
        }
    }
}
