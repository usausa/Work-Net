using System.Text;

using Microsoft.Extensions.Options;

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

        metrics.AddMyExporter("My1", _ => { });
        metrics.AddMyExporter("My2", _ => { });
    });

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

//----------------------------------------

public static class MyExporterExtensions
{
    //private const int DefaultExportIntervalMilliseconds = 10000;
    private const int DefaultExportIntervalMilliseconds = 5000;
    private const int DefaultExportTimeoutMilliseconds = Timeout.Infinite;

    public static MeterProviderBuilder AddMyExporter(this MeterProviderBuilder builder)
        => AddMyExporter(builder, null, (Action<MyExporterOptions>?)null);

    public static MeterProviderBuilder AddMyExporter(this MeterProviderBuilder builder, Action<MyExporterOptions> configure)
        => AddMyExporter(builder, null, configure);

    public static MeterProviderBuilder AddMyExporter(
        this MeterProviderBuilder builder,
        string? name,
        Action<MyExporterOptions>? configure)
    {
        name ??= Options.DefaultName;

        if (configure != null)
        {
            builder.ConfigureServices(services => services.Configure(name, configure));
        }

        return builder.AddReader(sp => BuildMyExporterMetricReader(
            name,
            sp.GetRequiredService<IOptionsMonitor<MyExporterOptions>>().Get(name),
            sp.GetRequiredService<IOptionsMonitor<MetricReaderOptions>>().Get(name)));
    }

    public static MeterProviderBuilder AddMyExporter(
        this MeterProviderBuilder builder,
        Action<MyExporterOptions, MetricReaderOptions> configure)
        => AddMyExporter(builder, null, configure);

    public static MeterProviderBuilder AddMyExporter(
        this MeterProviderBuilder builder,
        string? name,
        Action<MyExporterOptions, MetricReaderOptions>? configure)
    {
        name ??= Options.DefaultName;

        return builder.AddReader(sp =>
        {
            var exporterOptions = sp.GetRequiredService<IOptionsMonitor<MyExporterOptions>>().Get(name);
            var metricReaderOptions = sp.GetRequiredService<IOptionsMonitor<MetricReaderOptions>>().Get(name);

            configure?.Invoke(exporterOptions, metricReaderOptions);

            return BuildMyExporterMetricReader(name, exporterOptions, metricReaderOptions);
        });
    }

    private static MetricReader BuildMyExporterMetricReader(
        string name,
        MyExporterOptions exporterOptions,
        MetricReaderOptions metricReaderOptions)
    {
        var exporter = new MyExporter(name, exporterOptions);

        var exportInterval = metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds ??
                             DefaultExportIntervalMilliseconds;
        var exportTimeout = metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportTimeoutMilliseconds ??
                            DefaultExportTimeoutMilliseconds;
        var metricReader = new PeriodicExportingMetricReader(exporter, exportInterval, exportTimeout)
        {
            TemporalityPreference = metricReaderOptions.TemporalityPreference
        };

        return metricReader;
    }
}

public sealed class MyExporterOptions
{
}

internal sealed class MyExporter : BaseExporter<Metric>
{
    private readonly string name;

    private MyExporterOptions options;

    public MyExporter(string name, MyExporterOptions options)
    {
        this.name = name;
        this.options = options;
    }

    protected override void Dispose(bool disposing)
    {
        Console.WriteLine($"Dispose({disposing})");
    }

    protected override bool OnShutdown(int timeoutMilliseconds)
    {
        Console.WriteLine($"OnShutdown(timeoutMilliseconds={timeoutMilliseconds})");
        return true;
    }

    protected override bool OnForceFlush(int timeoutMilliseconds)
    {
        Console.WriteLine($"OnForceFlush(timeoutMilliseconds={timeoutMilliseconds})");
        return true;
    }

    public override ExportResult Export(in Batch<Metric> batch)
    {
        using var scope = SuppressInstrumentationScope.Begin();

        foreach (var metric in batch)
        {
            var sb = new StringBuilder();
            sb.Append($"[{name}] ");
            sb.Append($"{metric.Name} ");

            if (String.IsNullOrEmpty(metric.Unit))
            {
                sb.Append($"{metric.Unit} ");
            }

            if (String.IsNullOrEmpty(metric.MeterName))
            {
                sb.Append($"{metric.MeterName} ");

                if (String.IsNullOrEmpty(metric.MeterVersion))
                {
                    sb.Append($"{metric.MeterVersion} ");
                }
            }

            foreach (ref readonly var metricPoint in metric.GetMetricPoints())
            {

                sb.Append($"{metricPoint.StartTime}");
                foreach (var metricPointTag in metricPoint.Tags)
                {
                    sb.Append($" {metricPointTag.Key} {metricPointTag.Value}");
                }

                sb.AppendLine();
            }

            Console.Write(sb);
        }

        return ExportResult.Success;
    }
}
