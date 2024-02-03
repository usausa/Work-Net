using OpenTelemetry.Metrics;

using System.Net;

using OpenTelemetry.Trace;

using WorkBasicWeb;

var builder = WebApplication.CreateBuilder(args);

// TODO
//builder.Logging.AddOpenTelemetry(logging =>
//{
//    logging.IncludeFormattedMessage = true;
//    logging.IncludeScopes = true;
//});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Metrics
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddProcessInstrumentation()
            .AddEventCountersInstrumentation()
            .AddApiInstrumentation(); // Custom

        metrics.AddPrometheusExporter();
    })
    .WithTracing(tracing =>
    {
        // TODO
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    });
builder.Services.AddApiInstrument();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

// [OpenTelemetry] Restrict
app.UseOpenTelemetryPrometheusScrapingEndpoint(context => context.Request.Path == "/metrics" &&
                                                          Equals(context.Connection.RemoteIpAddress, IPAddress.Loopback));

app.MapControllers();

// [OpenTelemetry] Prometheus
app.MapPrometheusScrapingEndpoint();

app.Run();
