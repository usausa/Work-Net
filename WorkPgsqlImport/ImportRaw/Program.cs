namespace ImportRaw;

using System.Diagnostics;

using Npgsql;

public static class Program
{
    private const string ConnectionString = "Host=postgres-server;Database=test;Username=test;Password=test";

    public static async Task Main()
    {
        var users = new List<User>
        {
            new() { Id = 1, Name = "Name-1", Email = "name1@example.com", CreatedAt = DateTime.Now },
            new() { Id = 2, Name = "Name-2", Email = "name2@example.com", CreatedAt = DateTime.Now },
            new() { Id = 3, Name = "Name-3", Email = "name3@example.com", CreatedAt = DateTime.Now }
        };

        try
        {
            _ = await BulkInsertUsers(users);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private static async Task<ulong> BulkInsertUsers(List<User> users)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var writer = await connection.BeginBinaryImportAsync("COPY users (id, name, email, created_at) FROM STDIN (FORMAT BINARY)");

        foreach (var user in users)
        {
            await writer.StartRowAsync();

            await writer.WriteAsync(user.Id, NpgsqlTypes.NpgsqlDbType.Integer);
            await writer.WriteAsync(user.Name, NpgsqlTypes.NpgsqlDbType.Varchar);
            await writer.WriteAsync(user.Email, NpgsqlTypes.NpgsqlDbType.Varchar);
            await writer.WriteAsync(user.CreatedAt, NpgsqlTypes.NpgsqlDbType.Timestamp);
        }

        return await writer.CompleteAsync();
    }
}

public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public string Email { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
}
