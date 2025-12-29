namespace Develop;

using WorkInterceptor.Library;

internal static class Program
{
    public static void Main()
    {
        var builder = new Builder();
        builder.Execute<TestCommand>();
        builder.Execute<SampleCommand>();
        builder.AddCommands();

        // ISubBuilder test
        builder.AddSub(sub =>
        {
            sub.ExecuteSub<SubCommand>();
            sub.ExecuteSub<AnotherSubCommand>();
        });
    }
}

public static class Extensions
{
    public static void AddCommands(this IBuilder builder)
    {
        builder.Execute<AdvancedCommand>();
    }
}

[Command("test", "Test command for demonstration")]
public sealed class TestCommand
{
    [Option(1, "name", "n", Description = "User name", Required = true)]
    public string? Name { get; set; }

    [Option(2, "count", "c", Description = "Number of items")]
    public int Count { get; set; }

    [Option("verbose", "v")]
    public bool Verbose { get; set; }
}

[Command("sample")]
public sealed class SampleCommand
{
    [Option("input", "i", Description = "Input file path", Completions = ["file1.txt", "file2.txt", "file3.txt"])]
    public string? InputFile { get; set; }

    [Option("output", "o", Description = "Output file path")]
    public string? OutputFile { get; set; }
}

[Command("advanced", "Advanced command with typed options")]
public sealed class AdvancedCommand
{
    [Option<int>(1, "port", "p", Description = "Port number", Completions = [8080, 8443, 3000])]
    public int Port { get; set; }

    [Option<string>("mode", "m", Description = "Operation mode", Required = true, Completions = ["debug", "release", "test"])]
    public string? Mode { get; set; }

    [Option<double>("threshold", "t", Description = "Threshold value", Completions = [0.5, 0.75, 1.0])]
    public double Threshold { get; set; }

    [Option<float>("score", "s", Completions = [0.5f, 0.75f, 1.0f])]
    public float Score { get; set; }

    [Option("enabled", "e", Description = "Enable feature")]
    public bool Enabled { get; set; }
}

[Command("sub", "Sub command")]
public sealed class SubCommand
{
    [Option("path", "p", Description = "Path to file", Required = true)]
    public string? Path { get; set; }
}

[Command("another-sub", "Another sub command")]
public sealed class AnotherSubCommand
{
    [Option<int>("level", "l", Description = "Level value", Completions = [1, 2, 3])]
    public int Level { get; set; }
}

[Command("data1")]
public sealed class Data1
{
    [Option("value", Description = "Value")]
    public int Value { get; set; }
}

[Command("data2")]
public sealed class Data2
{
    [Option(0, "text")]
    public string Text { get; set; } = default!;
}

[Command("data3")]
public sealed class Data3
{
    [Option("id")]
    public int Id { get; set; }

    [Option(0, "text")]
    public string Name { get; set; } = default!;
}

public sealed class Data
{
}
