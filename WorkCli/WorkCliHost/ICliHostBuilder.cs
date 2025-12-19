using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace WorkCliHost;

/// <summary>
/// Builder for CLI host configuration.
/// </summary>
public interface ICliHostBuilder
{
    /// <summary>
    /// Configures application services (excluding commands).
    /// Use this for registering services like database contexts, HTTP clients, etc.
    /// </summary>
    ICliHostBuilder ConfigureServices(Action<IServiceCollection> configureServices);

    /// <summary>
    /// Configures CLI commands and related settings.
    /// Use this for registering commands, filters, and root command configuration.
    /// </summary>
    ICliHostBuilder ConfigureCommands(Action<ICommandConfigurator> configureCommands);

    /// <summary>
    /// Builds the CLI host.
    /// </summary>
    ICliHost Build();
}

/// <summary>
/// Configurator for commands and command-related settings.
/// Provides methods for registering commands, filters, and configuring the root command.
/// </summary>
public interface ICommandConfigurator
{
    /// <summary>
    /// Adds a CLI command to the application.
    /// </summary>
    ICommandConfigurator AddCommand<TCommand>(Action<ISubCommandConfigurator>? configure = null)
        where TCommand : class;

    /// <summary>
    /// Adds a global filter that applies to all commands.
    /// </summary>
    ICommandConfigurator AddGlobalFilter<TFilter>(int order = 0)
        where TFilter : class, ICommandFilter;

    /// <summary>
    /// Adds a global filter with a specific type.
    /// </summary>
    ICommandConfigurator AddGlobalFilter(Type filterType, int order = 0);

    /// <summary>
    /// Configures the root command.
    /// </summary>
    ICommandConfigurator ConfigureRootCommand(Action<IRootCommandConfigurator> configure);

    /// <summary>
    /// Configures command filter options.
    /// </summary>
    ICommandConfigurator ConfigureFilterOptions(Action<CommandFilterOptions> configure);
}

/// <summary>
/// Configurator for sub-commands.
/// </summary>
public interface ISubCommandConfigurator
{
    /// <summary>
    /// Adds a sub-command.
    /// </summary>
    ISubCommandConfigurator AddSubCommand<TCommand>(Action<ISubCommandConfigurator>? configure = null)
        where TCommand : class;
}

/// <summary>
/// Configurator for the root command.
/// </summary>
public interface IRootCommandConfigurator
{
    /// <summary>
    /// Sets the description of the root command.
    /// </summary>
    IRootCommandConfigurator WithDescription(string description);

    /// <summary>
    /// Sets the name of the root command (default is assembly name).
    /// </summary>
    IRootCommandConfigurator WithName(string name);

    /// <summary>
    /// Uses a custom root command instance.
    /// </summary>
    IRootCommandConfigurator UseCustomRootCommand(RootCommand rootCommand);

    /// <summary>
    /// Configures additional root command settings.
    /// </summary>
    IRootCommandConfigurator Configure(Action<RootCommand> configure);
}
