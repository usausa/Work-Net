using OpenTelemetry.Metrics;

using WorkBasicConsole;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("Worker.Metrics")
            .AddRuntimeInstrumentation();

        // http://localhost:9464/metrics
        metrics.AddPrometheusHttpListener();

        // > dotnet-counters monitor -n WorkBasicConsole Work.Basic
    });

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
