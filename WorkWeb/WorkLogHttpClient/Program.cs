using Microsoft.Extensions.Http.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<HttpClientLogger>();
builder.Services.AddHttpClient(
    "Ipify",
    c =>
    {
        c.BaseAddress = new Uri("https://api.ipify.org/");
    })
    .AddLogger<HttpClientLogger>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// TODO DelegatingHandler?

public class HttpClientLogger : IHttpClientLogger
{
    readonly ILogger<HttpClientLogger> logger;

    public HttpClientLogger(ILogger<HttpClientLogger> logger)
    {
        this.logger = logger;
    }

    public object? LogRequestStart(HttpRequestMessage request)
    {
        return null;
    }

    public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
    {
        logger.LogInformation($"{request.Method} {request.RequestUri?.AbsoluteUri} - {(int)response.StatusCode} {response.StatusCode} in {elapsed.TotalMilliseconds}ms");
    }

    public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed)
    {
        logger.LogWarning($"{request.Method} {request.RequestUri?.AbsoluteUri} - FAILED {exception.GetType().FullName}: {exception.Message}");
    }
}
