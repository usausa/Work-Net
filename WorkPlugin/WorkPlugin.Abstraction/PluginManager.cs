namespace WorkPlugin.Abstraction;

using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

public class PluginManager
{
    private readonly string[] modules;

    public PluginManager(string[] modules)
    {
        this.modules = modules;
    }

    public void LoadPlugins(IServiceCollection services)
    {
        foreach (var module in modules)
        {
            var assembly = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, module));
            var pluginLoaderTypes = assembly.GetTypes()
                .Where(t => typeof(IPluginLoader).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });
            foreach (var pluginLoaderType in pluginLoaderTypes)
            {
                var pluginLoader = (IPluginLoader)Activator.CreateInstance(pluginLoaderType)!;
                pluginLoader.Configure(services);
            }
        }
    }
}
