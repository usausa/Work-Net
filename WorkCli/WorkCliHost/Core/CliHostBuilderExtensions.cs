using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WorkCliHost;

/// <summary>
/// Extension methods for ICliHostBuilder to configure default settings.
/// </summary>
public static class CliHostBuilderExtensions
{
    /// <summary>
    /// Adds default configuration sources:
    /// - appsettings.json (optional)
    /// - appsettings.{Environment}.json (optional)
    /// - Environment variables
    /// </summary>
    public static ICliHostBuilder UseDefaultConfiguration(this ICliHostBuilder builder)
    {
        Microsoft.Extensions.Configuration.JsonConfigurationExtensions.AddJsonFile(
            builder.Configuration, "appsettings.json", optional: true, reloadOnChange: true);
        Microsoft.Extensions.Configuration.JsonConfigurationExtensions.AddJsonFile(
            builder.Configuration,
            $"appsettings.{builder.Environment.EnvironmentName}.json",
            optional: true,
            reloadOnChange: true);
        Microsoft.Extensions.Configuration.EnvironmentVariablesExtensions.AddEnvironmentVariables(
            builder.Configuration);

        return builder;
    }

    /// <summary>
    /// Adds default logging configuration:
    /// - Console logger
    /// - Configuration-based logging settings from "Logging" section
    /// </summary>
    public static ICliHostBuilder UseDefaultLogging(this ICliHostBuilder builder)
    {
        LoggingServiceCollectionExtensions.AddLogging(builder.Services, logging =>
        {
            // Add configuration section
            var loggingSection = builder.Configuration.GetSection("Logging");
            if (loggingSection.Exists())
            {
                logging.AddConfiguration(loggingSection);
            }
            ConsoleLoggerExtensions.AddConsole(logging);
        });

        return builder;
    }

    /// <summary>
    /// Configures the builder with all default settings.
    /// Equivalent to calling UseDefaultConfiguration() and UseDefaultLogging().
    /// </summary>
    public static ICliHostBuilder UseDefaults(this ICliHostBuilder builder)
    {
        return builder
            .UseDefaultConfiguration()
            .UseDefaultLogging();
    }

    /// <summary>
    /// Adds JSON configuration file.
    /// </summary>
    public static ICliHostBuilder AddJsonFile(
        this ICliHostBuilder builder,
        string path,
        bool optional = false,
        bool reloadOnChange = false)
    {
        Microsoft.Extensions.Configuration.JsonConfigurationExtensions.AddJsonFile(
            builder.Configuration, path, optional, reloadOnChange);
        return builder;
    }

    /// <summary>
    /// Adds environment variables to configuration.
    /// </summary>
    public static ICliHostBuilder AddEnvironmentVariables(
        this ICliHostBuilder builder,
        string? prefix = null)
    {
        if (prefix == null)
        {
            Microsoft.Extensions.Configuration.EnvironmentVariablesExtensions.AddEnvironmentVariables(
                builder.Configuration);
        }
        else
        {
            Microsoft.Extensions.Configuration.EnvironmentVariablesExtensions.AddEnvironmentVariables(
                builder.Configuration, prefix);
        }
        return builder;
    }

    /// <summary>
    /// Adds user secrets configuration for the specified assembly.
    /// </summary>
    public static ICliHostBuilder AddUserSecrets<T>(this ICliHostBuilder builder)
        where T : class
    {
        builder.Configuration.AddUserSecrets<T>();
        return builder;
    }

    /// <summary>
    /// Sets the minimum log level.
    /// </summary>
    public static ICliHostBuilder SetMinimumLogLevel(
        this ICliHostBuilder builder,
        LogLevel minimumLevel)
    {
        builder.Logging.SetMinimumLevel(minimumLevel);
        return builder;
    }

    /// <summary>
    /// Adds a filter to the logging configuration.
    /// </summary>
    public static ICliHostBuilder AddLoggingFilter(
        this ICliHostBuilder builder,
        string category,
        LogLevel level)
    {
        builder.Logging.AddFilter(category, level);
        return builder;
    }

    /// <summary>
    /// Adds Debug logging provider.
    /// </summary>
    public static ICliHostBuilder AddDebugLogging(this ICliHostBuilder builder)
    {
        Microsoft.Extensions.Logging.DebugLoggerFactoryExtensions.AddDebug(builder.Logging);
        return builder;
    }
}
