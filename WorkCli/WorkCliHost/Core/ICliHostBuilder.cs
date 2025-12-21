using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WorkCliHost.Core;

/// <summary>
/// Builder for CLI host configuration.
/// Similar to HostApplicationBuilder in Microsoft.Extensions.Hosting.
/// </summary>
public interface ICliHostBuilder
{
    /// <summary>
    /// Gets the application configuration.
    /// </summary>
    ConfigurationManager Configuration { get; }

    /// <summary>
    /// Gets information about the hosting environment.
    /// </summary>
    IHostEnvironment Environment { get; }

    /// <summary>
    /// Gets a collection of services for the application to compose.
    /// This is useful for adding services like database contexts, HTTP clients, etc.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets a collection of logging providers for the application to compose.
    /// </summary>
    ILoggingBuilder Logging { get; }

    /// <summary>
    /// Configures an alternate service provider.
    /// </summary>
    /// <typeparam name="TContainerBuilder">The type of the service provider builder.</typeparam>
    /// <param name="factory">The factory that will create the service provider.</param>
    /// <param name="configure">An optional delegate to configure the container builder.</param>
    void ConfigureContainer<TContainerBuilder>(
        IServiceProviderFactory<TContainerBuilder> factory,
        Action<TContainerBuilder>? configure = null)
        where TContainerBuilder : notnull;

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
    /// Adds a CLI command to the application with a custom action builder.
    /// This allows for AOT-friendly command construction.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="actionBuilder">Custom action builder function.</param>
    /// <param name="configure">Optional sub-command configuration.</param>
    ICommandConfigurator AddCommand<TCommand>(
        CommandActionBuilder actionBuilder,
        Action<ISubCommandConfigurator>? configure = null)
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

    /// <summary>
    /// Adds a sub-command with a custom action builder.
    /// This allows for AOT-friendly command construction.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="actionBuilder">Custom action builder function.</param>
    /// <param name="configure">Optional sub-command configuration.</param>
    ISubCommandConfigurator AddSubCommand<TCommand>(
        CommandActionBuilder actionBuilder,
        Action<ISubCommandConfigurator>? configure = null)
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
