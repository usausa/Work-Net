namespace WorkCliHost;

public static class CliHost
{
    public static ICliHostBuilder CreateDefaultBuilder(string[] args)
        => new CliHostBuilder(args);
}
