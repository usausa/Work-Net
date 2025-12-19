using Microsoft.Extensions.DependencyInjection;

namespace WorkCliHost;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCliCommand<TCommand>(
        this IServiceCollection services,
        Action<ICommandConfigurator>? configure = null)
        where TCommand : class, ICommandDefinition
    {
        services.AddTransient<TCommand>();
        
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
        where TCommand : class, ICommandDefinition
    {
        _services.AddTransient<TCommand>();
        
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
