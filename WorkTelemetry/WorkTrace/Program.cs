using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using WorkTrace;

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
        config.AddService("WorkTrace", serviceInstanceId: Environment.MachineName);
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
            config.Endpoint = new Uri("http://jaeger:4317");
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

app.Run();
