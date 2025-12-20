namespace WorkCliHost;

public static class CliHost
{
    /// <summary>
    /// Creates a new instance of ICliHostBuilder with default configuration.
    /// Includes: appsettings.json, environment variables, and console logging.
    /// </summary>
    public static ICliHostBuilder CreateDefaultBuilder(string[] args)
    {
        return new CliHostBuilder(args, useDefaults: true);
    }

    /// <summary>
    /// Creates a new instance of ICliHostBuilder with minimal configuration.
    /// Only includes console logging. No configuration files or environment variables are loaded by default.
    /// Use extension methods like UseDefaultConfiguration() to add them if needed.
    /// </summary>
    public static ICliHostBuilder CreateBuilder(string[] args)
    {
        return new CliHostBuilder(args, useDefaults: false);
    }
}
