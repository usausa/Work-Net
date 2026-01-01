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
        var pluginManager = new PluginManager();

        // 2) アセンブリから（単一ファイル publish 等：既にロード済みのものを対象）
        var pluginAssembliesInProcess = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Where(a => a.GetName().Name?.StartsWith("WorkPlugin.Plugin", StringComparison.Ordinal) == true)
            .OrderBy(a => a.GetName().Name, StringComparer.Ordinal);
        foreach (var assembly in pluginAssembliesInProcess)
        {
            pluginManager.LoadPluginsFromAssembly(assembly);
        }

        // 1) ファイルから（通常 publish）
        var pluginAssembliesFromFiles = Directory
            .GetFiles(AppContext.BaseDirectory, "WorkPlugin.Plugin*.dll")
            .Select(path => (Path: path, Assembly: SafeLoadFrom(path)))
            .Where(x => x.Assembly is not null)
            .Select(x => (x.Path, Assembly: x.Assembly!));

        foreach (var (path, assembly) in pluginAssembliesFromFiles)
        {
            pluginManager.LoadPluginsFromAssembly(assembly);
            Console.WriteLine($"Loaded: {Path.GetFileName(path)}");
        }


        pluginManager.Build();

#if DEBUG
        Console.WriteLine("DEBUG");
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

    private static Assembly? SafeLoadFrom(string assemblyPath)
    {
        try
        {
            return Assembly.LoadFrom(assemblyPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load {Path.GetFileName(assemblyPath)}: {ex.Message}");
            return null;
        }
    }
}

[RequiresUnreferencedCode("動的にアセンブリを読み込みます")]
public sealed class PluginManager : IDisposable
{
    private readonly IServiceCollection services;
    private IServiceProvider? serviceProvider;

    private readonly HashSet<string> loadedAssemblySimpleNames = new(StringComparer.Ordinal);

    public PluginManager()
    {
        services = new ServiceCollection();
    }

    public void LoadPluginsFromAssembly(Assembly assembly)
    {
        if (serviceProvider is not null)
        {
            throw new InvalidOperationException("PluginManager is already built.");
        }

        var assemblyName = assembly.GetName().Name;
        if (string.IsNullOrEmpty(assemblyName))
        {
            return;
        }

        if (!loadedAssemblySimpleNames.Add(assemblyName))
        {
            // 同名アセンブリは二重に処理しない
            return;
        }

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

    public void Build()
    {
        if (serviceProvider is not null)
        {
            throw new InvalidOperationException("PluginManager is already built.");
        }

        serviceProvider = services.BuildServiceProvider();
    }

    public IEnumerable<IPlugin> GetPlugins()
    {
        if (serviceProvider is null)
        {
            throw new InvalidOperationException("PluginManager is not built. Call Build() first.");
        }

        return serviceProvider.GetServices<IPlugin>();
    }

    public void Dispose()
    {
        if (serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
