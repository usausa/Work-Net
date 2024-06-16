using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using WorkTraceZipkin;

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
        config.AddService("WorkTraceZipkin", serviceInstanceId: Environment.MachineName);
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddApiInstrumentation();

        // OTEL_EXPORTER_ZIPKIN_ENDPOINT
        // Zipkin
        tracing
            .AddZipkinExporter(o =>
            {
                o.Endpoint = new Uri("http://zipkin:9411/api/v2/spans");
                o.HttpClientFactory = () =>
                {
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("X-MyCustomHeader", "value");
                    return client;
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

app.Run();
