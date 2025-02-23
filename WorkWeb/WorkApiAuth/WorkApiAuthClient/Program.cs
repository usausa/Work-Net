using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Rester;

using System.Net.Http.Headers;

RestConfig.Default.UseJsonSerializer();

// builder
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<TokenAccessor>();
builder.Services.AddTransient<JwtAuthHandler>();
builder.Services
    .AddHttpClient("Api", client =>
    {
        client.BaseAddress = new Uri("http://127.0.0.1:5000");
    })
    .AddHttpMessageHandler<JwtAuthHandler>();

// host
var host = builder.Build();

var httpClientFactory = host.Services.GetRequiredService<IHttpClientFactory>();
using var client = httpClientFactory.CreateClient("Api");

var response = await client.PostAsync<LoginResponse>("/test/login", new LoginRequest { User = "usausa" });
if (response.RestResult != RestResult.Success)
{
    Console.WriteLine(response.RestResult);
    return;
}

var tokenAccessor = host.Services.GetRequiredService<TokenAccessor>();
tokenAccessor.Token = response.Content!.Token;

var response2 = await client.GetAsync<ExecuteResponse>("/test/execute");
Console.WriteLine(response2.RestResult);
Console.WriteLine(response2.Content?.Message);

#if DEBUG
Console.ReadLine();
#endif

public class TokenAccessor
{
    public string Token { get; set; } = default!;
}

public class JwtAuthHandler : DelegatingHandler
{
    private readonly TokenAccessor tokenAccessor;

    public JwtAuthHandler(TokenAccessor tokenAccessor)
    {
        this.tokenAccessor = tokenAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = tokenAccessor.Token;
        if (!String.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

public class LoginRequest
{
    public string User { get; set; } = default!;
}

public class LoginResponse
{
    public string Token { get; set; } = default!;
}

public class ExecuteResponse
{
    public string Message { get; set; } = default!;
}
