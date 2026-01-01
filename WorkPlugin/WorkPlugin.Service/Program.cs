namespace WorkPlugin.Service;

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using WorkPlugin.Abstraction;

[RequiresUnreferencedCode("動的にアセンブリを読み込みます")]
internal static class Program
{
    public static void Main()
    {
        var pluginFiles = Directory.GetFiles(AppContext.BaseDirectory, "WorkPlugin.Plugin*.dll");
        var pluginManager = new PluginManager(pluginFiles);
        pluginManager.Initialize();

#if DEBUG
        Console.WriteLine("DEBUG");
#endif
#if WINDOWS_TFM
        Console.WriteLine("WINDOWS_TFM");
#endif
#if ENABLE_WINDOWS
        Console.WriteLine("ENABLE_WINDOWS");
#endif
#if ENABLE_MACOS
        Console.WriteLine("ENABLE_MACOS");
#endif
#if ENABLE_LINUX
        Console.WriteLine("ENABLE_LINUX");
#endif

        var plugins = pluginManager.GetPlugins().ToList();
        Console.WriteLine($"Total plugins loaded: {plugins.Count}");
        Console.WriteLine();

        foreach (var plugin in plugins.OrderBy(p => p.Name))
        {
            plugin.Execute();
        }
    }
}

[RequiresUnreferencedCode("動的にアセンブリを読み込みます")]
public class PluginManager : IDisposable
{
    private readonly IServiceCollection services;
    private IServiceProvider? serviceProvider;
    private readonly List<string> pluginAssemblyPaths;

    public PluginManager(IEnumerable<string> pluginAssemblyPaths)
    {
        services = new ServiceCollection();
        this.pluginAssemblyPaths = pluginAssemblyPaths.ToList();
    }

    public PluginManager(string pluginAssemblyPath)
        : this([pluginAssemblyPath])
    {
    }

    public void Initialize()
    {
        if (serviceProvider is not null)
        {
            throw new InvalidOperationException("PluginManager is already initialized.");
        }

        foreach (var assemblyPath in pluginAssemblyPaths)
        {
            LoadPluginFromPath(assemblyPath);
        }

        serviceProvider = services.BuildServiceProvider();
    }

    private void LoadPluginFromPath(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
        {
            return;
        }

        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            LoadPluginsFromAssembly(assembly);
            Console.WriteLine($"Loaded: {Path.GetFileName(assemblyPath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load {Path.GetFileName(assemblyPath)}: {ex.Message}");
        }
    }

    private void LoadPluginsFromAssembly(Assembly assembly)
    {
        var loaderTypes = assembly.GetTypes()
            .Where(t => typeof(IPluginLoader).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

        foreach (var loaderType in loaderTypes)
        {
            try
            {
                if (Activator.CreateInstance(loaderType) is IPluginLoader loader)
                {
                    loader.Configure(services);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Failed to execute loader {loaderType.Name}: {ex.Message}");
            }
        }
    }

    public IEnumerable<IPlugin> GetPlugins()
    {
        if (serviceProvider is null)
        {
            throw new InvalidOperationException("PluginManager is not initialized. Call Initialize() first.");
        }

        return serviceProvider!.GetServices<IPlugin>();
    }

    public void Dispose()
    {
        if (serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
