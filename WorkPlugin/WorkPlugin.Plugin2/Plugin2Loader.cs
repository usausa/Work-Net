using Microsoft.Extensions.DependencyInjection;

namespace WorkPlugin.Plugin2;

using WorkPlugin.Abstraction;

public class Plugin2Loader : IPluginLoader
{
    public void Configure(IServiceCollection services)
    {
        services.AddSingleton<IPlugin, Plugin2>();
    }
}
