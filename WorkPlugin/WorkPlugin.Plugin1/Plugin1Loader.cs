using Microsoft.Extensions.DependencyInjection;

namespace WorkPlugin.Plugin1;

using WorkPlugin.Abstraction;

public class Plugin1Loader : IPluginLoader
{
    public void Configure(IServiceCollection services)
    {
        services.AddSingleton<IPlugin, Plugin1>();
    }
}
