using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;

namespace WorkCliHost.Core;

internal sealed class CliHostBuilder : ICliHostBuilder
{
    private readonly string[] _args;
    private readonly ServiceCollection _services = new();
    private readonly ConfigurationManager _configuration;
    private readonly HostEnvironment _environment;
    private readonly LoggingBuilder _loggingBuilder;
    private Action<ICommandConfigurator>? _commandConfiguration;
    private object? _serviceProviderFactory;
    private Action<object>? _containerConfiguration;

    public CliHostBuilder(string[] args, bool useDefaults = true)
    {
        _args = args;

        // Environmentの初期化（常に必要）
        var contentRootPath = AppContext.BaseDirectory;
        _environment = new HostEnvironment
        {
            ApplicationName = Assembly.GetEntryAssembly()?.GetName().Name ?? "CliApp",
            EnvironmentName = System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") 
                ?? System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                ?? "Production",
            ContentRootPath = contentRootPath,
            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath)
        };

        // Configurationの初期化（最小限）
        _configuration = new ConfigurationManager();

        if (useDefaults)
        {
            // デフォルト設定を適用
            this.UseDefaultConfiguration();
            this.UseDefaultLogging();
        }
        else
        {
            // 最小限のLogging設定（Consoleのみ）
            _services.AddLogging(builder =>
            {
                builder.AddConsole();
            });
        }

        _loggingBuilder = new LoggingBuilder(_services);

        // 基本サービスの登録
        _services.AddSingleton<IConfiguration>(_configuration);
        _services.AddSingleton<IHostEnvironment>(_environment);
        
        // フィルタパイプラインを登録
        _services.AddSingleton<FilterPipeline>();
        
        // デフォルトのフィルタオプションを登録
        _services.AddOptions<CommandFilterOptions>();
    }

    public ConfigurationManager Configuration => _configuration;

    public IHostEnvironment Environment => _environment;

    public IServiceCollection Services => _services;

    public ILoggingBuilder Logging => _loggingBuilder;

    public void ConfigureContainer<TContainerBuilder>(
        IServiceProviderFactory<TContainerBuilder> factory,
        Action<TContainerBuilder>? configure = null)
        where TContainerBuilder : notnull
    {
        // 型安全なファクトリとして保存
        _serviceProviderFactory = new ServiceProviderFactoryAdapter<TContainerBuilder>(factory);
        
        // 設定デリゲートを保存（ビルド時に実行）
        if (configure != null)
        {
            _containerConfiguration = obj => configure((TContainerBuilder)obj);
        }
    }

    public ICliHostBuilder ConfigureCommands(Action<ICommandConfigurator> configureCommands)
    {
        _commandConfiguration = configureCommands;
        return this;
    }

    public ICliHost Build()
    {
        // コマンド設定を実行
        var commandConfigurator = new CommandConfigurator(_services);
        _commandConfiguration?.Invoke(commandConfigurator);

        // フィルタオプションを登録
        var filterOptions = commandConfigurator.GetFilterOptions();
        _services.Configure<CommandFilterOptions>(options =>
        {
            options.GlobalFilters.Clear();
            options.GlobalFilters.AddRange(filterOptions.GlobalFilters);
            options.IncludeBaseClassFilters = filterOptions.IncludeBaseClassFilters;
            options.DefaultFilterOrder = filterOptions.DefaultFilterOrder;
        });

        // コマンド登録からフィルタ型を収集してDI登録
        var commandRegistrations = commandConfigurator.GetCommandRegistrations();
        var filterTypes = new HashSet<Type>();
        
        // グローバルフィルタを追加
        foreach (var globalFilter in filterOptions.GlobalFilters)
        {
            filterTypes.Add(globalFilter.FilterType);
        }
        
        // コマンド属性からフィルタを収集
        foreach (var registration in commandRegistrations)
        {
            CollectFilterTypes(registration.CommandType, filterTypes);
        }
        
        // 収集したフィルタ型をDIに登録（まだ登録されていない場合）
        foreach (var filterType in filterTypes)
        {
            if (!_services.Any(sd => sd.ServiceType == filterType))
            {
                _services.AddTransient(filterType);
            }
        }
        
        // サービスプロバイダの構築
        IServiceProvider serviceProvider;
        if (_serviceProviderFactory != null)
        {
            // カスタムファクトリを使用
            var containerBuilder = _serviceProviderFactory.CreateBuilder(_services);
            _containerConfiguration?.Invoke(containerBuilder);
            serviceProvider = _serviceProviderFactory.CreateServiceProvider(containerBuilder);
        }
        else
        {
            // デフォルトのサービスプロバイダを使用
            serviceProvider = _services.BuildServiceProvider();
        }
        
        // RootCommandの作成と設定
        var customRootCommand = commandConfigurator.GetCustomRootCommand();
        var rootCommand = customRootCommand ?? new RootCommand();
        
        var rootCommandConfiguration = commandConfigurator.GetRootCommandConfiguration();
        rootCommandConfiguration?.Invoke(rootCommand);
        
        // コマンドを追加
        foreach (var registration in commandRegistrations)
        {
            var command = CreateCommandWithSubCommands(registration, serviceProvider);
            rootCommand.Subcommands.Add(command);
        }
        
        return new CliHostImplementation(_args, rootCommand, serviceProvider);
    }

    private Command CreateCommandWithSubCommands(CommandRegistration registration, IServiceProvider serviceProvider)
    {
        var command = CreateCommand(registration.CommandType, serviceProvider);
        
        foreach (var subRegistration in registration.SubCommands)
        {
            var subCommand = CreateCommandWithSubCommands(subRegistration, serviceProvider);
            command.Subcommands.Add(subCommand);
        }
        
        return command;
    }

    private Command CreateCommand(Type commandType, IServiceProvider serviceProvider)
    {
        var attribute = commandType.GetCustomAttribute<CliCommandAttribute>()
            ?? throw new InvalidOperationException($"Type {commandType.Name} must have CliCommandAttribute");

        var command = new Command(attribute.Name, attribute.Description);

        // ICommandDefinitionを実装していない場合はグループコマンド
        var isExecutableCommand = typeof(ICommandDefinition).IsAssignableFrom(commandType);
        
        if (isExecutableCommand)
        {
            // 実行可能コマンドの処理
            // プロパティと属性を収集（継承階層を考慮）
            var propertyInfos = CollectPropertiesWithArguments(commandType);

            var arguments = new List<(Argument Argument, PropertyInfo Property, Type ArgumentType)>();
            
            foreach (var (property, argAttr) in propertyInfos)
            {
                var argumentType = typeof(Argument<>).MakeGenericType(property.PropertyType);
                
                // Argument<T>のコンストラクタ: Argument(string name)
                var argument = (Argument)Activator.CreateInstance(argumentType, argAttr.Name)!;
                
                // Descriptionプロパティを設定
                var descriptionProperty = argumentType.GetProperty("Description");
                if (descriptionProperty != null && argAttr.Description != null)
                {
                    descriptionProperty.SetValue(argument, argAttr.Description);
                }

                // デフォルト値を設定
                var defaultValue = GetDefaultValue(property, argAttr);
                if (defaultValue.HasValue)
                {
                    var defaultValueFactoryProperty = argumentType.GetProperty("DefaultValueFactory");
                    if (defaultValueFactoryProperty != null)
                    {
                        // Func<ArgumentResult, T>のデリゲートを作成
                        var argumentResultType = typeof(ArgumentResult);
                        var funcType = typeof(Func<,>).MakeGenericType(argumentResultType, property.PropertyType);
                        
                        var capturedValue = defaultValue.Value;
                        var lambdaMethod = GetType().GetMethod(nameof(CreateDefaultValueFactory), BindingFlags.NonPublic | BindingFlags.Static)!
                            .MakeGenericMethod(property.PropertyType);
                        
                        var factoryDelegate = lambdaMethod.Invoke(null, [capturedValue]);
                        defaultValueFactoryProperty.SetValue(argument, factoryDelegate);
                    }
                }

                arguments.Add((argument, property, argumentType));
                command.Arguments.Add(argument);
            }

            command.SetAction(async parseResult =>
            {
                var instance = (ICommandDefinition)ActivatorUtilities.CreateInstance(serviceProvider, commandType);

                foreach (var (argument, property, argumentType) in arguments)
                {
                    // GetValueメソッドを呼び出す
                    var getValueMethod = typeof(ParseResult).GetMethod("GetValue", [argumentType])
                        ?? typeof(ParseResult).GetMethod("GetValue", 1, [argumentType]);
                    
                    if (getValueMethod == null)
                    {
                        // 汎用的なGetValueメソッドを取得
                        var methods = typeof(ParseResult).GetMethods()
                            .Where(m => m.Name == "GetValue" && m.IsGenericMethod && m.GetParameters().Length == 1);
                        
                        foreach (var method in methods)
                        {
                            var parameters = method.GetParameters();
                            if (parameters[0].ParameterType.IsGenericType && 
                                parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(Argument<>))
                            {
                                getValueMethod = method.MakeGenericMethod(property.PropertyType);
                                break;
                            }
                        }
                    }
                    
                    if (getValueMethod != null)
                    {
                        var value = getValueMethod.Invoke(parseResult, [argument]);
                        property.SetValue(instance, value);
                    }
                }

                // フィルタパイプラインを通してコマンドを実行
                var filterPipeline = serviceProvider.GetRequiredService<FilterPipeline>();
                var exitCode = await filterPipeline.ExecuteAsync(commandType, instance, CancellationToken.None);
                
                return exitCode;
            });
        }
        // ICommandGroupまたはサブコマンドのみの場合は、アクションを設定しない
        // System.CommandLineはアクションがない場合、自動的にヘルプを表示する

        return command;
    }

    private static void CollectFilterTypes(Type commandType, HashSet<Type> filterTypes)
    {
        // 継承階層を収集
        var currentType = commandType;
        while (currentType != null && currentType != typeof(object))
        {
            var attributes = currentType.GetCustomAttributes(typeof(CommandFilterAttribute), inherit: false)
                .Cast<CommandFilterAttribute>();
            
            foreach (var attr in attributes)
            {
                filterTypes.Add(attr.FilterType);
            }
            
            currentType = currentType.BaseType;
        }
    }

    /// <summary>
    /// 継承階層を考慮してプロパティと引数属性を収集し、適切な順序でソートします。
    /// </summary>
    private static List<(PropertyInfo Property, CliArgumentInfo Attribute)> CollectPropertiesWithArguments(Type commandType)
    {
        var typeHierarchy = new List<Type>();
        var currentType = commandType;
        
        // 継承階層を収集（派生→基底の順）
        while (currentType != null && currentType != typeof(object))
        {
            typeHierarchy.Add(currentType);
            currentType = currentType.BaseType;
        }
        
        // 基底→派生の順に反転
        typeHierarchy.Reverse();

        var allProperties = new List<(PropertyInfo Property, CliArgumentInfo Attribute, int TypeLevel, int PropertyIndex)>();
        
        for (int typeLevel = 0; typeLevel < typeHierarchy.Count; typeLevel++)
        {
            var type = typeHierarchy[typeLevel];
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
            for (int propIndex = 0; propIndex < properties.Length; propIndex++)
            {
                var property = properties[propIndex];
                var argAttr = GetCliArgumentAttribute(property);
                
                if (argAttr != null)
                {
                    allProperties.Add((property, argAttr, typeLevel, propIndex));
                }
            }
        }

        // ソート順序:
        // 1. Position指定がある場合は、そのPositionで優先
        // 2. Position指定がない場合（AutoPosition）は、TypeLevel（基底クラスが先）→ PropertyIndex
        var sortedProperties = allProperties
            .OrderBy(p =>
            {
                if (p.Attribute.Position != CliArgumentAttribute<object>.AutoPosition)
                {
                    // 明示的なPositionがある場合は、それを最優先（負でない値として扱う）
                    return (0, p.Attribute.Position, 0, 0);
                }
                else
                {
                    // AutoPositionの場合は、TypeLevel → PropertyIndexでソート
                    // 明示的なPositionより後になるように、第1キーを1にする
                    return (1, 0, p.TypeLevel, p.PropertyIndex);
                }
            })
            .Select(p => (p.Property, p.Attribute))
            .ToList();

        return sortedProperties;
    }

    private static Func<ArgumentResult, T> CreateDefaultValueFactory<T>(object? value)
    {
        return _ => (T)value!;
    }

    private static CliArgumentInfo? GetCliArgumentAttribute(PropertyInfo property)
    {
        // ジェネリック版の属性を検索
        var genericAttr = property.GetCustomAttributes(true)
            .FirstOrDefault(a => a.GetType().IsGenericType &&
                                 a.GetType().GetGenericTypeDefinition() == typeof(CliArgumentAttribute<>));

        if (genericAttr != null)
        {
            var attrType = genericAttr.GetType();
            var position = (int)attrType.GetProperty("Position")!.GetValue(genericAttr)!;
            var name = (string)attrType.GetProperty("Name")!.GetValue(genericAttr)!;
            var description = (string?)attrType.GetProperty("Description")?.GetValue(genericAttr);
            var isRequired = (bool)attrType.GetProperty("IsRequired")!.GetValue(genericAttr)!;
            var defaultValue = attrType.GetProperty("DefaultValue")?.GetValue(genericAttr);

            return new CliArgumentInfo(position, name, description, isRequired, defaultValue);
        }

        // 非ジェネリック版の属性を検索
        var attr = property.GetCustomAttribute<CliArgumentAttribute>();
        if (attr != null)
        {
            return new CliArgumentInfo(attr.Position, attr.Name, attr.Description, attr.IsRequired, null);
        }

        return null;
    }

    private static (bool HasValue, object? Value) GetDefaultValue(PropertyInfo property, CliArgumentInfo argInfo)
    {
        if (argInfo.DefaultValue != null)
        {
            return (true, argInfo.DefaultValue);
        }

        if (!argInfo.IsRequired)
        {
            var defaultValue = property.PropertyType.IsValueType
                ? Activator.CreateInstance(property.PropertyType)
                : null;
            return (true, defaultValue);
        }

        return (false, null);
    }
}

/// <summary>
/// Implementation of IHostEnvironment for CLI applications.
/// </summary>
internal sealed class HostEnvironment : IHostEnvironment
{
    public string ApplicationName { get; set; } = default!;
    public string EnvironmentName { get; set; } = default!;
    public string ContentRootPath { get; set; } = default!;
    public IFileProvider ContentRootFileProvider { get; set; } = default!;
}

/// <summary>
/// Implementation of ILoggingBuilder for CLI applications.
/// </summary>
internal sealed class LoggingBuilder : ILoggingBuilder
{
    public IServiceCollection Services { get; }

    public LoggingBuilder(IServiceCollection services)
    {
        Services = services;
    }
}

internal sealed class CliHostImplementation : ICliHost
{
    private readonly string[] _args;
    private readonly RootCommand _rootCommand;
    private readonly IServiceProvider _serviceProvider;

    public CliHostImplementation(string[] args, RootCommand rootCommand, IServiceProvider serviceProvider)
    {
        _args = args;
        _rootCommand = rootCommand;
        _serviceProvider = serviceProvider;
    }

    public async Task<int> RunAsync()
    {
        return _rootCommand.Parse(_args).Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

internal sealed record CliArgumentInfo(
    int Position,
    string Name,
    string? Description,
    bool IsRequired,
    object? DefaultValue);

/// <summary>
/// Adapter to wrap a typed IServiceProviderFactory into an untyped one.
/// </summary>
internal sealed class ServiceProviderFactoryAdapter<TContainerBuilder> : IServiceProviderFactory<IServiceCollection>
    where TContainerBuilder : notnull
{
    private readonly IServiceProviderFactory<TContainerBuilder> _factory;

    public ServiceProviderFactoryAdapter(IServiceProviderFactory<TContainerBuilder> factory)
    {
        _factory = factory;
    }

    public IServiceCollection CreateBuilder(IServiceCollection services)
    {
        // TContainerBuilderを作成し、IServiceCollectionとして返す
        // 実際のコンテナビルダーは内部で保持
        return services;
    }

    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
    {
        // IServiceCollectionからTContainerBuilderを作成し、サービスプロバイダを構築
        var typedBuilder = _factory.CreateBuilder(containerBuilder);
        return _factory.CreateServiceProvider(typedBuilder);
    }
}
