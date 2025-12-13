namespace WorkPlugin.Plugin4;

using Microsoft.Extensions.DependencyInjection;

using WorkPlugin.Abstraction;

public class Plugin4Loader : IPluginLoader
{
    public void Configure(IServiceCollection services)
    {
        services.AddSingleton<IPlugin, Plugin4>();
    }
}
