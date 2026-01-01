namespace WorkPlugin.Service;

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using WorkPlugin.Abstraction;

[RequiresUnreferencedCode("動的にアセンブリを読み込みます")]
internal static class Program
{
    public static void Main()
    {
        var pluginManager = new PluginManager();

        // "WorkPlugin.Plugin1" のようにアセンブリ名（ファイル名ベース）で指定して処理する
        pluginManager.LoadPlugins("WorkPlugin.Plugin1");
        pluginManager.LoadPlugins("WorkPlugin.Plugin2");
        pluginManager.LoadPlugins("WorkPlugin.Plugin3");
        pluginManager.LoadPlugins("WorkPlugin.Plugin4");

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
}

// TODO initializeに戻すか？ × ↓の方式で？、いやロード順があるか？
// TODO IServiceCollectionの共用、プラグインマネージャーも追加して、各プラグインも情報として追加してか？ ×PluginLoadなのでこれはいらない
// TODO ServiceCollectionも外で渡す形か？
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

    public void LoadPlugins(string assemblySimpleName)
    {
        if (string.IsNullOrWhiteSpace(assemblySimpleName))
        {
            return;
        }

        // 既に処理済みなら何もしない（同名アセンブリ二重処理防止）
        if (!loadedAssemblySimpleNames.Add(assemblySimpleName))
        {
            return;
        }

        // 1) ファイルが存在すればそこから読み込む（通常 publish）
        var dllPath = Path.Combine(AppContext.BaseDirectory, assemblySimpleName + ".dll");
        if (File.Exists(dllPath))
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllPath);
                LoadPluginsFromAssembly(assembly);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load {Path.GetFileName(dllPath)}: {ex.Message}");
            }
            return;
        }

        try
        {
            var inProcess = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(assemblySimpleName));
            LoadPluginsFromAssembly(inProcess);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load {assemblySimpleName}");
        }
    }

    private void LoadPluginsFromAssembly(Assembly assembly)
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

        // LoadPlugins(string) 側で登録済みならここは素通り、直接呼ばれた場合の二重処理対策。
        loadedAssemblySimpleNames.Add(assemblyName);

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
