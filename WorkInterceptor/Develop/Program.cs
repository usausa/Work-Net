namespace Develop;

using WorkInterceptor.Library;

internal static class Program
{
    public static void Main()
    {
        var builder = new Builder();
        builder.Execute<Data1>();

        var builder2 = new Builder();
        builder2.Execute<Data2>();

        // Extension method test
        builder.AddExecutes();
    }
}

public static class Extensions
{
    public static void AddExecutes(this IBuilder builder)
    {
        builder.Execute<Data3>();
    }
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
