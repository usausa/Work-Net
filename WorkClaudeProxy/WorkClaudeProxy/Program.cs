using Yarp.ReverseProxy.Transforms;
using WorkClaudeProxy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new DashboardImageStore("dashboard.jpg"));
builder.Services.AddHostedService<DashboardWorker>();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(transformBuilderContext =>
    {
        transformBuilderContext.AddResponseTransform(transformContext =>
        {
            if (transformContext.ProxyResponse is { } proxyResponse)
            {
                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (key, values) in proxyResponse.Headers)
                {
                    if (key.StartsWith("anthropic-", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("retry-after", StringComparison.OrdinalIgnoreCase))
                    {
                        headers[key] = string.Join(", ", values);
                    }
                }
                transformContext.HttpContext.Items[ClaudeProxyMiddleware.UpstreamHeadersKey] = headers;
            }
            return ValueTask.CompletedTask;
        });
    });

var app = builder.Build();

app.UseMiddleware<ClaudeProxyMiddleware>();
app.MapReverseProxy();

app.Run();
