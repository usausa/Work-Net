using System.Data;
using MyPgsql.Binary;

namespace WorkPostgresClient;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("===== ADO.NET PostgreSQL Client Demo (Binary Protocol) =====\n");

        const string connectionString = "Host=192.168.100.73;Port=5432;Database=test;Username=test;Password=test";

        try
        {
            await using var connection = new PgBinaryConnection(connectionString);
            await connection.OpenAsync();
            Console.WriteLine("接続成功！\n");

            // === 1. INSERT ===
            Console.WriteLine("=== INSERT ===");
            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO users (id, name, email, created_at) VALUES (@id, @name, @email, @created_at)";
                cmd.Parameters.Add(new PgBinaryParameter("@id", DbType.Int32) { Value = 2001 });
                cmd.Parameters.Add(new PgBinaryParameter("@name", DbType.String) { Value = "ADO.NET User" });
                cmd.Parameters.Add(new PgBinaryParameter("@email", DbType.String) { Value = "adonet@example.com" });
                cmd.Parameters.Add(new PgBinaryParameter("@created_at", DbType.DateTime) { Value = DateTime.Now });

                var inserted = await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"挿入: {inserted} 行\n");
            }

            // === 2. SELECT (単一値) ===
            Console.WriteLine("=== SELECT (ExecuteScalar) ===");
            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM users";
                var count = await cmd.ExecuteScalarAsync();
                Console.WriteLine($"ユーザー数: {count}\n");
            }

            // === 3. SELECT (DataReader) ===
            Console.WriteLine("=== SELECT (DataReader) ===");
            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT id, name, email, created_at FROM users WHERE id = @id";
                cmd.Parameters.Add(new PgBinaryParameter("@id", DbType.Int32) { Value = 2001 });

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"ID: {reader.GetInt32(0)}, Name: {reader.GetString(1)}, Email: {reader.GetString(2)}, Created: {reader.GetDateTime(3)}\n");
                }
            }

            // === 4. UPDATE ===
            Console.WriteLine("=== UPDATE ===");
            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "UPDATE users SET name = @name, email = @email WHERE id = @id";
                cmd.Parameters.Add(new PgBinaryParameter("@id", DbType.Int32) { Value = 2001 });
                cmd.Parameters.Add(new PgBinaryParameter("@name", DbType.String) { Value = "Updated ADO.NET User" });
                cmd.Parameters.Add(new PgBinaryParameter("@email", DbType.String) { Value = "updated.adonet@example.com" });

                var updated = await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"更新: {updated} 行\n");
            }

            // === 5. Transaction Demo ===
            Console.WriteLine("=== TRANSACTION ===");
            await using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                    await using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = "INSERT INTO users (id, name, email, created_at) VALUES (@id, @name, @email, @created_at)";
                        cmd.Parameters.Add(new PgBinaryParameter("@id", DbType.Int32) { Value = 2002 });
                        cmd.Parameters.Add(new PgBinaryParameter("@name", DbType.String) { Value = "Transaction User" });
                        cmd.Parameters.Add(new PgBinaryParameter("@email", DbType.String) { Value = "tx@example.com" });
                        cmd.Parameters.Add(new PgBinaryParameter("@created_at", DbType.DateTime) { Value = DateTime.Now });
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();
                    Console.WriteLine("トランザクションコミット成功\n");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine("トランザクションロールバック\n");
                    throw;
                }
            }

            // === 6. SELECT (全件確認) ===
            Console.WriteLine("=== SELECT (全件確認) ===");
            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT id, name, email FROM users ORDER BY id";
                await using var reader = await cmd.ExecuteReaderAsync();

                Console.WriteLine($"{"ID",-10} {"Name",-25} {"Email",-30}");
                Console.WriteLine(new string('-', 65));

                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"{reader.GetInt32(0),-10} {reader.GetString(1),-25} {reader.GetString(2),-30}");
                }
                Console.WriteLine();
            }

            // === 7. DELETE (クリーンアップ) ===
            Console.WriteLine("=== DELETE (クリーンアップ) ===");
            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM users WHERE id IN (@id1, @id2)";
                cmd.Parameters.Add(new PgBinaryParameter("@id1", DbType.Int32) { Value = 2001 });
                cmd.Parameters.Add(new PgBinaryParameter("@id2", DbType.Int32) { Value = 2002 });

                var deleted = await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"削除: {deleted} 行\n");
            }

            Console.WriteLine("===== 完了 =====");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラー: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
