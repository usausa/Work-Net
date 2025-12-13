namespace WorkPlugin.Plugin3;

using Microsoft.Extensions.DependencyInjection;

using WorkPlugin.Abstraction;

public class Plugin3Loader : IPluginLoader
{
    public void Configure(IServiceCollection services)
    {
        services.AddSingleton<IPlugin, Plugin3>();
    }
}
