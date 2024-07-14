namespace WorkIntegrationTest.Tests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

using Newtonsoft.Json;

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkIntegrationTest.Web.Controllers;

using Xunit.Abstractions;
using Xunit.Sdk;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public ITestOutputHelper OutputHelper { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ILoggerProvider>(new XunitLoggerProvider(OutputHelper));
        });
    }
}

internal sealed class XunitLoggerProvider(ITestOutputHelper testOutputHelper) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new XunitLogger(testOutputHelper, categoryName);
    }

    public void Dispose()
    {
    }
}

public sealed class XunitLogger(ITestOutputHelper outputHelper, string categoryName) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NopScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            outputHelper.WriteLine($"{logLevel} {categoryName} {formatter(state, exception)}");
            //outputHelper.WriteLine($"{categoryName} [{eventId}] {formatter(state, exception)}");
            if (exception is not null)
            {
                outputHelper.WriteLine(exception.ToString());
            }
        }
        catch
        {
        }
    }

    private class NopScope : IDisposable
    {
        public static readonly NopScope Instance = new();

        public void Dispose()
        {
        }
    }
}

public class UnitTest1(CustomWebApplicationFactory factory, ITestOutputHelper outputHelper) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Test1()
    {
        // Arrange
        var requestObject = new Request
        {
            Value = "Test"
        };
        var jsonContent = new StringContent(JsonConvert.SerializeObject(requestObject), Encoding.UTF8, "application/json");

        // Act
        factory.OutputHelper = outputHelper;
        using var client = factory.CreateClient();
        var response = await client.PostAsync("/test/execute", jsonContent);

        // Assert
        response.EnsureSuccessStatusCode();

        var responseObject = JsonConvert.DeserializeObject<Response>(await response.Content.ReadAsStringAsync());

        Assert.Equal("Test", responseObject.Value);
    }
}
