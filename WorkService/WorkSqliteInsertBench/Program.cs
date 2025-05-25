using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;

using Microsoft.Data.Sqlite;

using Smart.Data.Mapper;

#pragma warning disable CA1812

var rootCommand = new RootCommand("SQLite benchmark");
rootCommand.AddOption(new Option<int>(["--thread", "-t"], () => 100, "thread"));
rootCommand.AddOption(new Option<int>(["--loop", "-l"], () => 60 * 24, "loop"));
rootCommand.AddOption(new Option<string>(["--directory", "-d"], Directory.GetCurrentDirectory, "directory"));
rootCommand.Handler = CommandHandler.Create((IConsole console, int thread, int loop, string directory) =>
{
    var tasks = new List<Task>();
    for (var i = 0; i < thread; i++)
    {
        var no = i + 1;
        tasks.Add(Task.Run(async () =>
        {
            var filename = Path.Combine(directory, $"data{no:D4}.db");
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

#pragma warning disable CA2007
            await using var con = new SqliteConnection("Data Source=" + filename);
#pragma warning restore CA2007
            await con.ExecuteAsync("CREATE TABLE Data (Id INTEGER NOT NULL, Name TEXT NOT NULL, PRIMARY KEY(Id))").ConfigureAwait(false);

            for (var j = 0; j < loop; j++)
            {
                await con.ExecuteAsync("INSERT INTO Data VALUES (@Id, @Name)", new Data { Id = j, Name = $"Data-{j}" }).ConfigureAwait(false);
            }
        }));
    }

    var watch = Stopwatch.StartNew();

    // 完了待ち
    Task.WaitAll(tasks.ToArray());

    var total = thread * loop;
    console.WriteLine($"TotalCount : {total}");
    console.WriteLine($"TotalTime : {watch.ElapsedMilliseconds}");
    console.WriteLine($"TPS : {(double)total / watch.ElapsedMilliseconds * 1000}");
});

return await rootCommand.InvokeAsync(args).ConfigureAwait(false);

#pragma warning disable CA1050
public class Data
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}
#pragma warning restore CA1050
