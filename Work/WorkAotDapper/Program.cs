using System.Diagnostics;

namespace WorkAotDapper;

using System.Data.Common;

using Dapper;

using Microsoft.Data.Sqlite;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        File.Delete("Test.db");
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = "Test.db",
            Pooling = true,
            Cache = SqliteCacheMode.Shared
        };
        await using var db = new SqliteConnection(builder.ConnectionString);

        await db.ExecuteAsync("CREATE TABLE Data (Id INTEGER NOT NULL, Name TEXT NOT NULL, PRIMARY KEY(Id))");

        var repository = new Repository(builder.ConnectionString);
        await repository.InsertDataAsync(new DataEntity { Id = 1, Name = "Data-1" });
        await repository.InsertDataAsync(new DataEntity { Id = 2, Name = "Data-2" });

        var entity1 = await repository.QueryDataByIdAsync(1);
        var entity0 = await repository.QueryDataByIdAsync(0);

        Debug.Assert(entity1 is not null);
        Debug.Assert(entity0 is null);
    }
}

[DapperAot]
public sealed class Repository
{
    private readonly string connectionString;

    public Repository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    private DbConnection CreateConnection() => new SqliteConnection(connectionString);

    public async Task InsertDataAsync(DataEntity data)
    {
        await using var con = CreateConnection();
        await con.ExecuteAsync("INSERT INTO Data (Id, Name) VALUES (@Id, @Name)", data);
    }

    public async Task<DataEntity?> QueryDataByIdAsync(int id)
    {
        await using var con = CreateConnection();
        return await con.QueryFirstOrDefaultAsync<DataEntity>("SELECT Id, Name FROM Data WHERE Id = @Id", new { Id = id });
    }
}

public class DataEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}
