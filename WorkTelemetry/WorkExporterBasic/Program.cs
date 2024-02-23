using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;

using WorkExporterBasic.Metrics;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddApplicationInstrumentation();

        metrics.AddConsoleExporter("Console1", config =>
        {
            config.Targets = ConsoleExporterOutputTargets.Console;
        });
        metrics.AddConsoleExporter("Console2", config =>
        {
            config.Targets = ConsoleExporterOutputTargets.Debug;
        });

        //metrics.AddInMemoryExporter()
    });

var host = builder.Build();
host.Run();

// TODO IPullMetricExporter
