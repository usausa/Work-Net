using Npgsql;

namespace ImportAuto;

internal static class Program
{
    private const string ConnectionString = "Host=postgres-server;Database=test;Username=test;Password=test";

    public static async Task Main()
    {
        await using var con = new NpgsqlConnection(ConnectionString);
        // TODO get metadata
    }
}
