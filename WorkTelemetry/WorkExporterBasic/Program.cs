using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;

using WorkExporterBasic.Metrics;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddApplicationInstrumentation();

        // TODO name確認
        metrics.AddConsoleExporter("Console", config =>
        {
            config.Targets = ConsoleExporterOutputTargets.Console | ConsoleExporterOutputTargets.Debug;
        });

        //metrics.AddInMemoryExporter()
    });

var host = builder.Build();
host.Run();

// TODO IPullMetricExporter
