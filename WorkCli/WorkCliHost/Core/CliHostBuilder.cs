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
        // ジェネリックファクトリをそのまま保存
        _serviceProviderFactory = factory;
        
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
            // カスタムファクトリを使用（リフレクション経由）
            var factoryType = _serviceProviderFactory.GetType();
            var createBuilderMethod = factoryType.GetMethod(nameof(IServiceProviderFactory<object>.CreateBuilder));
            var createServiceProviderMethod = factoryType.GetMethod(nameof(IServiceProviderFactory<object>.CreateServiceProvider));
            
            if (createBuilderMethod == null || createServiceProviderMethod == null)
            {
                throw new InvalidOperationException("Invalid service provider factory.");
            }
            
            var containerBuilder = createBuilderMethod.Invoke(_serviceProviderFactory, new object[] { _services });
            _containerConfiguration?.Invoke(containerBuilder!);
            serviceProvider = (IServiceProvider)createServiceProviderMethod.Invoke(_serviceProviderFactory, new[] { containerBuilder })!;
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
        // 1. 基本的なCommand生成
        var attribute = registration.CommandType.GetCustomAttribute<CliCommandAttribute>()
            ?? throw new InvalidOperationException($"Type {registration.CommandType.Name} must have CliCommandAttribute");
        
        var command = new Command(attribute.Name, attribute.Description);
        
        // 2. 実行可能コマンドの場合
        if (typeof(ICommandDefinition).IsAssignableFrom(registration.CommandType))
        {
            // ビルダーコンテキストを作成
            var builderContext = new CommandActionBuilderContext
            {
                CommandType = registration.CommandType,
                Command = command,
                ServiceProvider = serviceProvider
            };

            // アクションビルダーを取得
            var actionBuilder = registration.ActionBuilder 
                ?? CommandBuilderHelpers.CreateReflectionBasedActionBuilder(registration.CommandType);

            // ビルダーを実行
            var (arguments, coreAction) = actionBuilder(builderContext);

            // 3. 引数をCommandに追加
            foreach (var argument in arguments)
            {
                command.Arguments.Add(argument);
            }

            // 4. SetActionでラッパーを設定
            var filterPipeline = serviceProvider.GetRequiredService<FilterPipeline>();
            
            command.SetAction(async parseResult =>
            {
                // a. CommandContextを生成（呼び出し側で統一的に生成）
                var commandContext = new CommandContext
                {
                    CommandType = registration.CommandType,
                    CancellationToken = CancellationToken.None
                };

                // b. コマンドインスタンスを生成（呼び出し側で統一的に生成）
                var commandInstance = (ICommandDefinition)ActivatorUtilities
                    .CreateInstance(serviceProvider, registration.CommandType);

                // c. CommandContextに設定
                commandContext.Command = commandInstance;

                // d. FilterPipelineでコアアクションをラップして実行
                await filterPipeline.ExecuteAsync(
                    registration.CommandType,
                    commandInstance,
                    async () =>
                    {
                        // コアアクションを実行
                        await coreAction(commandInstance, parseResult, commandContext);
                    },
                    commandContext.CancellationToken);

                return commandContext.ExitCode;
            });
        }
        
        // 5. サブコマンド追加
        foreach (var subRegistration in registration.SubCommands)
        {
            var subCommand = CreateCommandWithSubCommands(subRegistration, serviceProvider);
            command.Subcommands.Add(subCommand);
        }
        
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
