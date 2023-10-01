using Microsoft.AspNetCore.Connections;

using WorkSocket;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(options =>
{
    options.ListenLocalhost(8007, config =>
    {
        config.UseConnectionHandler<EchoConnectionHandler>();
    });
});

var app = builder.Build();

app.Run();

// TODO Hosted版？
// TODO 終了の問題？
// TODO ログにhttpが出る問題
// TODO Line & XML