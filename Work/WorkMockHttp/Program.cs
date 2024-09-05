using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net;

var services = new ServiceCollection();
services.AddTransient(_ => new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
{
    Content = new StringContent("{\"message\":\"Hello, World!\"}")
}));
services.AddHttpClient("Mock").AddHttpMessageHandler<MockHttpMessageHandler>();

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
