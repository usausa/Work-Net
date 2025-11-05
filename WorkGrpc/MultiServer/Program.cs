using MultiServer.Api;

var builder = WebApplication.CreateBuilder(args);

// Kestrelの設定
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP/2のKeepAliveタイムアウト設定
    options.Limits.Http2.KeepAlivePingDelay = TimeSpan.FromSeconds(30);
    options.Limits.Http2.KeepAlivePingTimeout = TimeSpan.FromSeconds(60);

    // アイドル接続のタイムアウト
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);

    // リクエストヘッダーの読み取りタイムアウト
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<MultiApi>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
