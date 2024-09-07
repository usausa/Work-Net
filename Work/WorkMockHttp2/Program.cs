using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Polly;

var services = new ServiceCollection();
// Default setting
services.ConfigureHttpClientDefaults(builder =>
{
    builder.AddResilienceHandler("pipeline", config =>
    {
        config.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(500),
            BackoffType = DelayBackoffType.Exponential,
            OnRetry = message =>
            {
                Debug.WriteLine(message.RetryDelay);
                return ValueTask.CompletedTask;
            }
        });
    });
    builder.AddHttpMessageHandler(() => new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("{\"message\":\"Hello, World!\"}")
    }));

    //builder.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    //{
    //});
});

// Use
var provider = services.BuildServiceProvider();
var clientFactory = provider.GetRequiredService<IHttpClientFactory>();
using var client = clientFactory.CreateClient("Mock");

using var response = await client.GetAsync("https://example.com/api/test");
var content = await response.Content.ReadAsStringAsync();

Debug.WriteLine(content);

public class MockHttpMessageHandler : DelegatingHandler
{
    private readonly HttpResponseMessage response;

    public MockHttpMessageHandler(HttpResponseMessage response)
    {
        this.response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(response);
    }
}
