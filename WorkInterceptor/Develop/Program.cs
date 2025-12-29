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
    }
}

public static class Extensions
{
    public static void AddCommands(this IBuilder builder)
    {
        builder.Execute<AdvancedCommand>();
    }
}

[Command("test")]
public sealed class TestCommand
{
    [Option(1, "name")]
    public string? Name { get; set; }

    [Option(2, "count")]
    public int Count { get; set; }

    [Option("verbose")]
    public bool Verbose { get; set; }
}

[Command("sample")]
public sealed class SampleCommand
{
    [Option("input", Completions = new[] { "file1.txt", "file2.txt" })]
    public string? InputFile { get; set; }

    [Option("output")]
    public string? OutputFile { get; set; }
}

[Command("advanced")]
public sealed class AdvancedCommand
{
    [Option<int>(1, "port", Completions = new[] { 8080, 8443, 3000 })]
    public int Port { get; set; }

    [Option<string>("mode", Completions = ["debug", "release", "test"])]
    public string? Mode { get; set; }

    [Option<double>("threshold", Completions = new[] { 0.5, 0.75, 1.0 })]
    public double Threshold { get; set; }

    [Option<float>("score", Completions = new[] { 0.5f, 0.75f, 1.0f })]
    public float Score { get; set; }

    [Option("enabled")]
    public bool Enabled { get; set; }
}

[Command("data1")]
public sealed class Data1
{
    [Option("value")]
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
