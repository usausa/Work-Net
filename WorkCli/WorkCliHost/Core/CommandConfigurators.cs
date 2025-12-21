using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace WorkCliHost.Core;

/// <summary>
/// Delegate for building a command from a command type.
/// This allows for custom command construction logic, including Source Generator support.
/// </summary>
/// <param name="commandType">The type of the command to build.</param>
/// <param name="serviceProvider">The service provider for dependency resolution.</param>
/// <returns>A configured System.CommandLine.Command instance.</returns>
public delegate Command CommandBuilder(Type commandType, IServiceProvider serviceProvider);

/// <summary>
/// Implementation of ICommandConfigurator for configuring commands and filters.
/// </summary>
internal sealed class CommandConfigurator : ICommandConfigurator
{
    private readonly IServiceCollection _services;
    private readonly List<CommandRegistration> _commandRegistrations = new();
    private readonly CommandFilterOptions _filterOptions = new();
    private Action<RootCommand>? _rootCommandConfiguration;
    private RootCommand? _customRootCommand;

    public CommandConfigurator(IServiceCollection services)
    {
        _services = services;
    }

    public ICommandConfigurator AddCommand<TCommand>(Action<ISubCommandConfigurator>? configure = null)
        where TCommand : class
    {
        return AddCommand<TCommand>(builder: null, configure);
    }

    public ICommandConfigurator AddCommand<TCommand>(
        CommandBuilder? builder,
        Action<ISubCommandConfigurator>? configure = null)
        where TCommand : class
    {
        // ICommandDefinitionを実装している場合のみ、DIコンテナに登録
        if (typeof(ICommandDefinition).IsAssignableFrom(typeof(TCommand)))
        {
            _services.AddTransient<TCommand>();
        }

        var registration = new CommandRegistration(typeof(TCommand), builder);

        if (configure != null)
        {
            var subConfigurator = new SubCommandConfigurator(_services);
            configure(subConfigurator);
            registration.SubCommands.AddRange(subConfigurator.GetRegistrations());
        }

        _commandRegistrations.Add(registration);
        return this;
    }

    public ICommandConfigurator AddGlobalFilter<TFilter>(int order = 0)
        where TFilter : class, ICommandFilter
    {
        _filterOptions.GlobalFilters.Add(new GlobalFilterDescriptor(typeof(TFilter), order));
        _services.AddTransient<TFilter>();
        return this;
    }

    public ICommandConfigurator AddGlobalFilter(Type filterType, int order = 0)
    {
        if (!typeof(ICommandFilter).IsAssignableFrom(filterType))
        {
            throw new ArgumentException($"Filter type must implement {nameof(ICommandFilter)}", nameof(filterType));
        }

        _filterOptions.GlobalFilters.Add(new GlobalFilterDescriptor(filterType, order));
        _services.AddTransient(filterType);
        return this;
    }

    public ICommandConfigurator ConfigureRootCommand(Action<IRootCommandConfigurator> configure)
    {
        var rootConfigurator = new RootCommandConfigurator();
        configure(rootConfigurator);
        
        _rootCommandConfiguration = rootConfigurator.GetConfiguration();
        _customRootCommand = rootConfigurator.GetCustomRootCommand();
        
        return this;
    }

    public ICommandConfigurator ConfigureFilterOptions(Action<CommandFilterOptions> configure)
    {
        configure(_filterOptions);
        return this;
    }

    internal List<CommandRegistration> GetCommandRegistrations() => _commandRegistrations;
    internal CommandFilterOptions GetFilterOptions() => _filterOptions;
    internal Action<RootCommand>? GetRootCommandConfiguration() => _rootCommandConfiguration;
    internal RootCommand? GetCustomRootCommand() => _customRootCommand;
}

/// <summary>
/// Implementation of ISubCommandConfigurator for configuring sub-commands.
/// </summary>
internal sealed class SubCommandConfigurator : ISubCommandConfigurator
{
    private readonly IServiceCollection _services;
    private readonly List<CommandRegistration> _registrations = new();

    public SubCommandConfigurator(IServiceCollection services)
    {
        _services = services;
    }

    public ISubCommandConfigurator AddSubCommand<TCommand>(Action<ISubCommandConfigurator>? configure = null)
        where TCommand : class
    {
        return AddSubCommand<TCommand>(builder: null, configure);
    }

    public ISubCommandConfigurator AddSubCommand<TCommand>(
        CommandBuilder? builder,
        Action<ISubCommandConfigurator>? configure = null)
        where TCommand : class
    {
        // ICommandDefinitionを実装している場合のみ、DIコンテナに登録
        if (typeof(ICommandDefinition).IsAssignableFrom(typeof(TCommand)))
        {
            _services.AddTransient<TCommand>();
        }

        var registration = new CommandRegistration(typeof(TCommand), builder);

        if (configure != null)
        {
            var subConfigurator = new SubCommandConfigurator(_services);
            configure(subConfigurator);
            registration.SubCommands.AddRange(subConfigurator.GetRegistrations());
        }

        _registrations.Add(registration);
        return this;
    }

    internal List<CommandRegistration> GetRegistrations() => _registrations;
}

/// <summary>
/// Implementation of IRootCommandConfigurator for configuring the root command.
/// </summary>
internal sealed class RootCommandConfigurator : IRootCommandConfigurator
{
    private string? _description;
    private string? _name;
    private RootCommand? _customRootCommand;
    private readonly List<Action<RootCommand>> _configurations = new();

    public IRootCommandConfigurator WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public IRootCommandConfigurator WithName(string name)
    {
        _name = name;
        return this;
    }

    public IRootCommandConfigurator UseCustomRootCommand(RootCommand rootCommand)
    {
        _customRootCommand = rootCommand;
        return this;
    }

    public IRootCommandConfigurator Configure(Action<RootCommand> configure)
    {
        _configurations.Add(configure);
        return this;
    }

    internal Action<RootCommand>? GetConfiguration()
    {
        if (_description == null && _configurations.Count == 0)
        {
            return null;
        }

        return rootCommand =>
        {
            if (_description != null)
            {
                rootCommand.Description = _description;
            }

            foreach (var config in _configurations)
            {
                config(rootCommand);
            }
        };
    }

    internal RootCommand? GetCustomRootCommand()
    {
        // カスタムRootCommandが指定されている場合はそれを使用
        if (_customRootCommand != null)
        {
            return _customRootCommand;
        }

        // 名前が指定されている場合は、その名前でRootCommandを作成
        if (_name != null)
        {
            return new RootCommand(_name);
        }

        return null;
    }
}

/// <summary>
/// Registration information for a command.
/// </summary>
internal sealed class CommandRegistration
{
    public Type CommandType { get; }
    public List<CommandRegistration> SubCommands { get; } = new();
    
    /// <summary>
    /// Custom builder for creating the command.
    /// If null, reflection-based builder will be used.
    /// </summary>
    public CommandBuilder? Builder { get; }

    public CommandRegistration(Type commandType, CommandBuilder? builder = null)
    {
        CommandType = commandType;
        Builder = builder;
    }
}
