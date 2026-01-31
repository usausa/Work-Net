using System.Data;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MyPgsql;
//using MyPgsql.Pipelines;
using Npgsql;
using RawPgsql;

namespace MyPgsqlBenchmark;

internal class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<PostgresBenchmarks>();
    }
}

[MemoryDiagnoser]
public class PostgresBenchmarks
{
    private const string ConnectionString = "Host=192.168.100.73;Port=5432;Database=test;Username=test;Password=test";

    private NpgsqlConnection _npgsqlConnection = null!;
    private PgConnection _myPgsqlConnection = null!;
    //private PgPipeConnection _myPgsqlPipeConnection = null!;
    private RawPgClient _rawPgsqlClient = null!;
    private int _insertId;

    [GlobalSetup]
    public async Task Setup()
    {
        // Npgsql接続
        _npgsqlConnection = new NpgsqlConnection(ConnectionString);
        await _npgsqlConnection.OpenAsync();

        // MyPgsql接続 (NetworkStream版)
        _myPgsqlConnection = new PgConnection(ConnectionString);
        await _myPgsqlConnection.OpenAsync();

        //// MyPgsql接続 (Pipelines版)
        //_myPgsqlPipeConnection = new PgPipeConnection(ConnectionString);
        //await _myPgsqlPipeConnection.OpenAsync();

        // RawPgsql接続 (DbConnection非実装版)
        _rawPgsqlClient = await RawPgClient.CreateAsync(ConnectionString);

        _insertId = 100000;
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _npgsqlConnection.DisposeAsync();
        await _myPgsqlConnection.DisposeAsync();
        //await _myPgsqlPipeConnection.DisposeAsync();
        await _rawPgsqlClient.DisposeAsync();
    }

    #region データ全件取得ベンチマーク

    [Benchmark(Description = "Npgsql: SELECT all from data")]
    public async Task<int> Npgsql_SelectAllData()
    {
        await using var cmd = new NpgsqlCommand("SELECT id, name, option, flag, create_at FROM data", _npgsqlConnection);
        //await using var cmd = new NpgsqlCommand("SELECT * FROM device", _npgsqlConnection);
        await using var reader = await cmd.ExecuteReaderAsync();

        var count = 0;
        while (await reader.ReadAsync())
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var option = reader.IsDBNull(2) ? null : reader.GetString(2);
            var flag = reader.GetBoolean(3);
            var createAt = reader.GetDateTime(4);
            count++;
        }
        return count;
    }

    [Benchmark(Description = "MyPgsql: SELECT all from data")]
    public async Task<int> MyPgsql_SelectAllData()
    {
        await using var cmd = _myPgsqlConnection.CreateCommand();
        cmd.CommandText = "SELECT id, name, option, flag, create_at FROM data";
        await using var reader = await cmd.ExecuteReaderAsync();

        var count = 0;
        while (await reader.ReadAsync())
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var option = reader.IsDBNull(2) ? null : reader.GetString(2);
            var flag = reader.GetBoolean(3);
            var createAt = reader.GetDateTime(4);
            count++;
        }
        return count;
    }

    [Benchmark(Description = "RawPgsql: SELECT all from data")]
    public async Task<int> RawPgsql_SelectAllData()
    {
        await using var reader = await _rawPgsqlClient.ExecuteQueryAsync("SELECT id, name, option, flag, create_at FROM data");

        var count = 0;
        while (await reader.ReadAsync())
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var option = reader.GetStringOrNull(2);
            var flag = reader.GetBoolean(3);
            var createAt = reader.GetDateTime(4);
            count++;
        }
        return count;
    }

    [Benchmark(Description = "RawPgsql-Binary: SELECT all from data")]
    public async Task<int> RawPgsqlBinary_SelectAllData()
    {
        await using var reader = await _rawPgsqlClient.ExecuteQueryBinaryAsync("SELECT id, name, option, flag, create_at FROM data");

        var count = 0;
        while (await reader.ReadAsync())
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var option = reader.GetStringOrNull(2);
            var flag = reader.GetBoolean(3);
            var createAt = reader.GetDateTime(4);
            count++;
        }
        return count;
    }

    //[Benchmark(Description = "MyPgsql-Pipe: SELECT all from data")]
    //public async Task<int> MyPgsqlPipe_SelectAllData()
    //{
    //    await using var cmd = _myPgsqlPipeConnection.CreateCommand();
    //    cmd.CommandText = "SELECT id, name, option, flag, create_at FROM data";
    //    await using var reader = await cmd.ExecuteReaderAsync();

    //    var count = 0;
    //    while (await reader.ReadAsync())
    //    {
    //        var id = reader.GetInt32(0);
    //        var name = reader.GetString(1);
    //        var option = reader.IsDBNull(2) ? null : reader.GetString(2);
    //        var flag = reader.GetBoolean(3);
    //        var createAt = reader.GetDateTime(4);
    //        count++;
    //    }
    //    return count;
    //}

    #endregion

    #region INSERT/DELETEベンチマーク

    //[Benchmark(Description = "Npgsql: INSERT and DELETE user")]
    //public async Task Npgsql_InsertDeleteUser()
    //{
    //    var id = Interlocked.Increment(ref _insertId);

    //    // INSERT
    //    await using (var cmd = new NpgsqlCommand(
    //        "INSERT INTO users (id, name, email, created_at) VALUES (@id, @name, @email, @created_at)",
    //        _npgsqlConnection))
    //    {
    //        cmd.Parameters.AddWithValue("@id", id);
    //        cmd.Parameters.AddWithValue("@name", "Benchmark User");
    //        cmd.Parameters.AddWithValue("@email", "benchmark@example.com");
    //        cmd.Parameters.AddWithValue("@created_at", DateTime.UtcNow);
    //        await cmd.ExecuteNonQueryAsync();
    //    }

    //    // DELETE
    //    await using (var cmd = new NpgsqlCommand("DELETE FROM users WHERE id = @id", _npgsqlConnection))
    //    {
    //        cmd.Parameters.AddWithValue("@id", id);
    //        await cmd.ExecuteNonQueryAsync();
    //    }
    //}

    //[Benchmark(Description = "MyPgsql: INSERT and DELETE user")]
    //public async Task MyPgsql_InsertDeleteUser()
    //{
    //    var id = Interlocked.Increment(ref _insertId);

    //    // INSERT
    //    await using (var cmd = _myPgsqlConnection.CreateCommand())
    //    {
    //        cmd.CommandText = "INSERT INTO users (id, name, email, created_at) VALUES (@id, @name, @email, @created_at)";
    //        cmd.Parameters.Add(new PgParameter("@id", DbType.Int32) { Value = id });
    //        cmd.Parameters.Add(new PgParameter("@name", DbType.String) { Value = "Benchmark User" });
    //        cmd.Parameters.Add(new PgParameter("@email", DbType.String) { Value = "benchmark@example.com" });
    //        cmd.Parameters.Add(new PgParameter("@created_at", DbType.DateTime) { Value = DateTime.UtcNow });
    //        await cmd.ExecuteNonQueryAsync();
    //    }

    //    // DELETE
    //    await using (var cmd = _myPgsqlConnection.CreateCommand())
    //    {
    //        cmd.CommandText = "DELETE FROM users WHERE id = @id";
    //        cmd.Parameters.Add(new PgParameter("@id", DbType.Int32) { Value = id });
    //        await cmd.ExecuteNonQueryAsync();
    //    }
    //}

    //[Benchmark(Description = "MyPgsql-Pipe: INSERT and DELETE user")]
    //public async Task MyPgsqlPipe_InsertDeleteUser()
    //{
    //    var id = Interlocked.Increment(ref _insertId);

    //    // INSERT
    //    await using (var cmd = _myPgsqlPipeConnection.CreateCommand())
    //    {
    //        cmd.CommandText = "INSERT INTO users (id, name, email, created_at) VALUES (@id, @name, @email, @created_at)";
    //        cmd.Parameters.Add(new PgParameter("@id", DbType.Int32) { Value = id });
    //        cmd.Parameters.Add(new PgParameter("@name", DbType.String) { Value = "Benchmark User" });
    //        cmd.Parameters.Add(new PgParameter("@email", DbType.String) { Value = "benchmark@example.com" });
    //        cmd.Parameters.Add(new PgParameter("@created_at", DbType.DateTime) { Value = DateTime.UtcNow });
    //        await cmd.ExecuteNonQueryAsync();
    //    }

    //    // DELETE
    //    await using (var cmd = _myPgsqlPipeConnection.CreateCommand())
    //    {
    //        cmd.CommandText = "DELETE FROM users WHERE id = @id";
    //        cmd.Parameters.Add(new PgParameter("@id", DbType.Int32) { Value = id });
    //        await cmd.ExecuteNonQueryAsync();
    //    }
    //}

    #endregion
}
