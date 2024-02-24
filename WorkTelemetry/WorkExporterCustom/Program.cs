using System.Text;

using OpenTelemetry.Metrics;
using OpenTelemetry;

using WorkExporterCustom.Metrics;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddApplicationInstrumentation();

        metrics.AddMyExporter(5000);
    });

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
    private readonly string name;

    public MyExporter(string name = "MyExporter")
    {
        this.name = name;
    }

    public override ExportResult Export(in Batch<Metric> batch)
    {
        // SuppressInstrumentationScope should be used to prevent exporter
        // code from generating telemetry and causing live-loop.
        using var scope = SuppressInstrumentationScope.Begin();

        var sb = new StringBuilder();
        foreach (var metric in batch)
        {
            if (sb.Length > 0)
            {
                sb.Append(", ");
            }

            sb.Append($"{metric.Name}");

            foreach (ref readonly var metricPoint in metric.GetMetricPoints())
            {
                sb.Append($"{metricPoint.StartTime}");
                foreach (var metricPointTag in metricPoint.Tags)
                {
                    sb.Append($"{metricPointTag.Key} {metricPointTag.Value}");
                }
            }
        }

        Console.WriteLine($"{name}.Export([{sb}])");
        return ExportResult.Success;
    }

    protected override bool OnShutdown(int timeoutMilliseconds)
    {
        Console.WriteLine($"{name}.OnShutdown(timeoutMilliseconds={timeoutMilliseconds})");
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        Console.WriteLine($"{name}.Dispose({disposing})");
    }
}
