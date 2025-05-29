using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;

using Serilog;

using WorkTcpServer.Handlers;
using WorkTcpServer.Handlers.Actions;
using WorkTcpServer.Services;

//--------------------------------------------------------------------------------
// Configure builder
//--------------------------------------------------------------------------------
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args });

builder.WebHost.UseKestrel(static options =>
{
    //options.ListenAnyIP(10080);

    options.ListenAnyIP(11520, static config =>
    {
        config.Protocols = HttpProtocols.None;
        config.UseConnectionHandler<SampleConnectionHandler>();
    });
});

// Log
builder.Logging.ClearProviders();
builder.Host
    .UseSerilog(static (hostingContext, loggerConfiguration) =>
    {
        loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
    });

// Reactor
builder.Services.AddSingleton<IActionFactory, GetActionFactory>();
builder.Services.AddSingleton<IActionFactory, SetActionFactory>();

// Service
builder.Services.AddSingleton<DataService>();

//--------------------------------------------------------------------------------
// Add services to the container.
//--------------------------------------------------------------------------------
var app = builder.Build();

//app.MapGet("/", () => "Test");

app.Run();
