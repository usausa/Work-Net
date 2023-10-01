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

// TODO Hosted�ŁH
// TODO �I���̖��H
// TODO ���O��http���o����
// TODO Line & XML