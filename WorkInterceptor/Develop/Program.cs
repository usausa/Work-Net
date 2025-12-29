namespace Develop;

using WorkInterceptor.Library;

internal static class Program
{
    public static void Main()
    {
        var builder = new Builder();
        builder.Execute<TestCommand>();
        builder.AddCommands();
        builder.AddSub(sub =>
        {
            sub.ExecuteSub<TestCommand>();
        });
    }
}

public static class Extensions
{
    public static void AddCommands(this IBuilder builder)
    {
        builder.Execute<CompletionsTestCommand>();
    }
}

// Base command class
public abstract class BaseCommand
{
    [Option(10, "base-option", "b", Description = "Option from base class")]
    public string? BaseOption { get; set; }

    [Option(5, "priority", "p", Description = "High priority option")]
    public int Priority { get; set; }
}

[Command("test", "Test command for demonstration")]
public sealed class TestCommand : BaseCommand
{
    [Option(1, "name", "n", Description = "User name", Required = true)]
    public string? Name { get; set; }

    [Option(2, "count", "c", Description = "Number of items")]
    public int Count { get; set; }

    [Option("verbose", "v")]
    public bool Verbose { get; set; }
}

[Command("completions-test", "Test command for completions")]
public sealed class CompletionsTestCommand
{
    [Option("mode", "m", Description = "Operation mode", Completions = ["debug", "release", "test"])]
    public string? Mode { get; set; }

    [Option<int>("port", "p", Description = "Port number", Completions = [8080, 8443, 3000])]
    public int Port { get; set; }

    [Option<float>("score", "s", Completions = [0.5f, 0.75f, 1.0f])]
    public float Score { get; set; }
}
