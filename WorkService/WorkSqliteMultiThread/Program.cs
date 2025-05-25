using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;

using Microsoft.Data.Sqlite;

using Smart.Data.Mapper;

#pragma warning disable CA1812

var rootCommand = new RootCommand("SQLite benchmark");
rootCommand.AddOption(new Option<int>(["--thread", "-t"], () => 100, "thread"));
rootCommand.AddOption(new Option<int>(["--loop", "-l"], () => 1000, "loop"));
rootCommand.AddOption(new Option<bool>(["--wal", "-w"], () => true, "wal"));
rootCommand.AddOption(new Option<bool>(["--pool", "-p"], () => true, "pool"));
rootCommand.AddOption(new Option<bool>(["--shared", "-s"], () => true, "shared"));
rootCommand.Handler = CommandHandler.Create(async (IConsole console, int thread, int loop, bool wal, bool pool, bool shared) =>
{
    File.Delete("Test.db");
    var builder = new SqliteConnectionStringBuilder
    {
        DataSource = "Test.db",
        Pooling = pool
    };
    if (shared)
    {
        builder.Cache = SqliteCacheMode.Shared;
    }
    var connectionString = builder.ConnectionString;

#pragma warning disable CA2007
    await using var db = new SqliteConnection(connectionString);
#pragma warning restore CA2007

    await db.ExecuteAsync("CREATE TABLE Data (Id INTEGER NOT NULL, Name TEXT NOT NULL, PRIMARY KEY(Id))").ConfigureAwait(false);

    for (var i = 0; i < thread / 2; i++)
    {
        await db.ExecuteAsync("INSERT INTO Data VALUES (@Id, @Name)", new Data { Id = i, Name = $"Data-{i}" }).ConfigureAwait(false);
    }

    var select1 = new Counter();
    var select2 = new Counter();
    var insert1 = new Counter();
    var insert2 = new Counter();
    var update1 = new Counter();
    var update2 = new Counter();
    var delete1 = new Counter();
    var delete2 = new Counter();

    var tasks = new List<Task>();
    for (var i = 0; i < thread; i++)
    {
        tasks.Add(Task.Run(async () =>
        {
            var rand = new Random();

            for (var j = 0; j < loop; j++)
            {
#pragma warning disable CA2007
                await using var con = new SqliteConnection(connectionString);
#pragma warning restore CA2007
                await con.OpenAsync().ConfigureAwait(false);
                if (wal)
                {
                    await con.ExecuteAsync("PRAGMA journal_mode=WAL").ConfigureAwait(false);
                }

#pragma warning disable CA5394
                var id = rand.Next(100);
                switch (rand.Next(4))
                {
                    case 0:
                        var entity = await con.QueryFirstOrDefaultAsync<Data>("SELECT * FROM Data WHERE Id = @Id", new { Id = id }).ConfigureAwait(false);
                        if (entity is not null)
                        {
                            select1.Increment();
                        }
                        else
                        {
                            select2.Increment();
                        }
                        break;
                    case 1:
                        try
                        {
                            await con.ExecuteAsync("INSERT INTO Data VALUES (@Id, @Name)", new Data { Id = id, Name = $"Data-{id}" }).ConfigureAwait(false);
                            insert1.Increment();
                        }
                        catch (SqliteException e)
                        {
                            if (e.SqliteErrorCode != 19)
                            {
                                throw;
                            }
                            insert2.Increment();
                        }
                        break;
                    case 2:
                        var updated = await con.ExecuteAsync("UPDATE Data SET Name = @Name WHERE Id = @Id", new { Id = id, Name = $"Updated-{id}" }).ConfigureAwait(false);
                        if (updated == 1)
                        {
                            update1.Increment();
                        }
                        else
                        {
                            update2.Increment();
                        }
                        break;
                    case 3:
                        var deleted = await con.ExecuteAsync("DELETE FROM Data WHERE Id = @Id", new { Id = id }).ConfigureAwait(false);
                        if (deleted == 1)
                        {
                            delete1.Increment();
                        }
                        else
                        {
                            delete2.Increment();
                        }
                        break;
                }
#pragma warning restore CA5394
            }
        }));
    }

    var watch = Stopwatch.StartNew();

    // 完了待ち
#pragma warning disable CA1031
    try
    {
#pragma warning disable CA1849
        Task.WaitAll(tasks.ToArray());
#pragma warning restore CA1849
    }
    catch (Exception e)
    {
        console.WriteLine(e.ToString());
    }
#pragma warning restore CA1031

    var total = thread * loop;
    console.WriteLine($"TotalCount : {total}");
    console.WriteLine($"TotalTime : {watch.ElapsedMilliseconds}");
    console.WriteLine($"TPS : {(double)total / watch.ElapsedMilliseconds * 1000}");
    console.WriteLine($"Select1 : {select1.Value}");
    console.WriteLine($"Select2 : {select2.Value}");
    console.WriteLine($"Insert1 : {insert1.Value}");
    console.WriteLine($"Insert2 : {insert2.Value}");
    console.WriteLine($"Update1 : {update1.Value}");
    console.WriteLine($"Update2 : {update2.Value}");
    console.WriteLine($"Delete1 : {delete1.Value}");
    console.WriteLine($"Delete2 : {delete2.Value}");
    console.WriteLine($"Total : {select1.Value + select2.Value + insert1.Value + insert2.Value + update1.Value + update2.Value + delete1.Value + delete2.Value}");
});

return await rootCommand.InvokeAsync(args).ConfigureAwait(false);

#pragma warning disable CA1050
public class Data
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

public class Counter
{
    private readonly object sync = new();

    private int value;

    public int Value
    {
        get
        {
            lock (sync)
            {
                return value;
            }
        }
    }

    public void Increment()
    {
        lock (sync)
        {
            value++;
        }
    }
}
#pragma warning restore CA1050
