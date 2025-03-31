using Microsoft.Extensions.Configuration;
using WorkPlugin.Abstraction;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Plugin
var pluginAssemblies = builder.Configuration.GetSection("PluginAssemblies").Get<string[]>()!;
var pluginManager = new PluginManager(pluginAssemblies);
pluginManager.LoadPlugins(builder.Services);

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
