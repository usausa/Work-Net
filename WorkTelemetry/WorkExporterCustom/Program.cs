using System.Text;

using OpenTelemetry.Metrics;
using OpenTelemetry;

using WorkExporterCustom;
using WorkExporterCustom.Metrics;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddApplicationInstrumentation();
builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddApplicationInstrumentation();

        metrics.AddMyExporter(5000);
        //metrics.AddMyExporter();
    });

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

internal static class MyExporterExtensions
{
    public static MeterProviderBuilder AddMyExporter(this MeterProviderBuilder builder, int exportIntervalMilliSeconds = Timeout.Infinite)
    {
        if (exportIntervalMilliSeconds == Timeout.Infinite)
        {
            // Export triggered manually only.
            return builder.AddReader(new BaseExportingMetricReader(new MyExporter()));
        }
        else
        {
            // Export is triggered periodically.
            return builder.AddReader(new PeriodicExportingMetricReader(new MyExporter(), exportIntervalMilliSeconds));
        }
    }
}

internal class MyExporter : BaseExporter<Metric>
{
    public override ExportResult Export(in Batch<Metric> batch)
    {
        using var scope = SuppressInstrumentationScope.Begin();

        foreach (var metric in batch)
        {
            foreach (ref readonly var metricPoint in metric.GetMetricPoints())
            {
                var sb = new StringBuilder();
                sb.Append($"{metric.Name} ");
                sb.Append($"{metricPoint.StartTime}");
                foreach (var metricPointTag in metricPoint.Tags)
                {
                    sb.Append($" {metricPointTag.Key} {metricPointTag.Value}");
                }

                Console.WriteLine(sb);
            }
        }

        return ExportResult.Success;
    }

    protected override bool OnShutdown(int timeoutMilliseconds)
    {
        Console.WriteLine($"OnShutdown(timeoutMilliseconds={timeoutMilliseconds})");
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        Console.WriteLine($"Dispose({disposing})");
    }
}
