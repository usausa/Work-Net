using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddResourceMonitoring();
builder.Services.AddHealthChecks()
    .AddResourceUtilizationHealthCheck(o =>
    {
        o.CpuThresholds = new ResourceUsageThresholds
        {
            DegradedUtilizationPercentage = 10,
            UnhealthyUtilizationPercentage = 20,
        };
        o.MemoryThresholds = new ResourceUsageThresholds
        {
            DegradedUtilizationPercentage = 10,
            UnhealthyUtilizationPercentage = 20,
        };
        o.SamplingWindow = TimeSpan.FromSeconds(5);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.UseHealthChecks("/health");

var healthCheckService = app.Services.GetRequiredService<HealthCheckService>();
var result = await healthCheckService.CheckHealthAsync();
Debug.WriteLine($"{result.Status} {result.TotalDuration}");

app.Run();
