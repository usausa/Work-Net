using Microsoft.Extensions.DependencyInjection;

namespace WorkCliHost;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCliCommand<TCommand>(
        this IServiceCollection services,
        Action<ICommandConfigurator>? configure = null)
        where TCommand : class
    {
        // ICommandDefinitionを実装している場合のみ、DIコンテナに登録
        if (typeof(ICommandDefinition).IsAssignableFrom(typeof(TCommand)))
        {
            services.AddTransient<TCommand>();
        }
        
        var registration = new CommandRegistration(typeof(TCommand));
        
        if (configure != null)
        {
            var configurator = new CommandConfigurator(services);
            configure(configurator);
            registration.SubCommands.AddRange(configurator.GetRegistrations());
        }
        
        services.AddSingleton(registration);
        return services;
    }

    /// <summary>
    /// Adds a global filter that applies to all commands.
    /// </summary>
    public static IServiceCollection AddGlobalCommandFilter<TFilter>(this IServiceCollection services, int order = 0)
        where TFilter : class, ICommandFilter
    {
        services.Configure<CommandFilterOptions>(options =>
        {
            options.GlobalFilters.Add(new GlobalFilterDescriptor(typeof(TFilter), order));
        });

        // Register the filter in DI
        services.AddTransient<TFilter>();

        return services;
    }

    /// <summary>
    /// Adds a global filter with instance.
    /// </summary>
    public static IServiceCollection AddGlobalCommandFilter(this IServiceCollection services, Type filterType, int order = 0)
    {
        if (!typeof(ICommandFilter).IsAssignableFrom(filterType))
        {
            throw new ArgumentException($"Filter type must implement {nameof(ICommandFilter)}", nameof(filterType));
        }

        services.Configure<CommandFilterOptions>(options =>
        {
            options.GlobalFilters.Add(new GlobalFilterDescriptor(filterType, order));
        });

        // Register the filter in DI
        services.AddTransient(filterType);

        return services;
    }
}

internal sealed class CommandRegistration
{
    public Type CommandType { get; }
    public List<CommandRegistration> SubCommands { get; } = new();

    public CommandRegistration(Type commandType)
    {
        CommandType = commandType;
    }
}

internal sealed class CommandConfigurator : ICommandConfigurator
{
    private readonly IServiceCollection _services;
    private readonly List<CommandRegistration> _registrations = new();

    public CommandConfigurator(IServiceCollection services)
    {
        _services = services;
    }

    public ICommandConfigurator AddSubCommand<TCommand>(Action<ICommandConfigurator>? configure = null)
        where TCommand : class
    {
        // ICommandDefinitionを実装している場合のみ、DIコンテナに登録
        if (typeof(ICommandDefinition).IsAssignableFrom(typeof(TCommand)))
        {
            _services.AddTransient<TCommand>();
        }
        
        var registration = new CommandRegistration(typeof(TCommand));
        
        if (configure != null)
        {
            var subConfigurator = new CommandConfigurator(_services);
            configure(subConfigurator);
            registration.SubCommands.AddRange(subConfigurator.GetRegistrations());
        }
        
        _registrations.Add(registration);
        return this;
    }

    public List<CommandRegistration> GetRegistrations() => _registrations;
}
