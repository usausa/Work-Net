using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WorkAspire;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Metrics
builder.Services.AddOpenTelemetry()
    .ConfigureResource(config =>
    {
        config.AddService("WorkAspire", serviceInstanceId: Environment.MachineName);
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();

        metrics.AddPrometheusExporter();

        metrics.AddOtlpExporter(config =>
        {
            // gRPC
            config.Endpoint = new Uri("http://aspire:18889");
        });
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddApiInstrumentation();

        tracing.AddOtlpExporter(config =>
        {
            // gRPC
            config.Endpoint = new Uri("http://aspire:18889");
        });

        tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.Filter = context =>
                {
                    var url = context.Request.Path.ToUriComponent();
                    return !url.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) &&
                           !url.StartsWith("/_", StringComparison.OrdinalIgnoreCase);
                };
            });
    });
builder.Services.AddApiInstrument();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.MapPrometheusScrapingEndpoint();

app.Run();
