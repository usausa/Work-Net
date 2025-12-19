using Microsoft.Extensions.DependencyInjection;

namespace WorkCliHost;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCliCommand<TCommand>(this IServiceCollection services)
        where TCommand : class, ICommandDefinition
    {
        services.AddTransient<TCommand>();
        services.AddSingleton(new CommandRegistration(typeof(TCommand)));
        return services;
    }
}

internal sealed class CommandRegistration
{
    public Type CommandType { get; }

    public CommandRegistration(Type commandType)
    {
        CommandType = commandType;
    }
}
