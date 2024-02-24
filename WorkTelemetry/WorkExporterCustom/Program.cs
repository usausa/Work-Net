using WorkExporterCustom.Metrics;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddApplicationInstrumentation();

        // TODO
        //metrics.AddConsoleExporter("Console1", config =>
        //{
        //    config.Targets = ConsoleExporterOutputTargets.Console;
        //});
    });

var host = builder.Build();
host.Run();
