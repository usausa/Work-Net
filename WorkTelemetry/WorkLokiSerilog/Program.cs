using Serilog;
using Serilog.Debugging;
using Serilog.Sinks.Grafana.Loki;

const string OutputTemplate = "{Timestamp:dd-MM-yyyy HH:mm:ss} [{Level:u3}] [{ThreadId}] {Message}{NewLine}{Exception}";

SelfLog.Enable(Console.Error);

while (true)
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("meaning_of_life", 42)
        .WriteTo.Console(outputTemplate: OutputTemplate)
        .WriteTo.GrafanaLoki(
            "http://loki:3100",
            new List<LokiLabel> { new() { Key = "app", Value = "console" } },
            credentials: null)
        .CreateLogger();

    Log.Debug("This is a debug message");

    var person = new Person("Billy", 42);

    Log.Information("Person of the day: {@Person}", person);

    try
    {
        throw new AccessViolationException("Access denied");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occured");
    }

    Thread.Sleep(Random.Shared.Next(100 * 100));
}


Log.CloseAndFlush();

// Data
internal record Person(string Name, int Age);
