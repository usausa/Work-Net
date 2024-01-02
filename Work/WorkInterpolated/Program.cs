namespace WorkInterpolated;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

public static class Program
{
    public static void Main()
    {
        Logger.Log($"test={1}");
    }
}

public class Logger
{
    public static void Log([InterpolatedStringHandlerArgument] MyHandler builder)
    {
        Debug.WriteLine(builder.GetFormattedText());
    }
}

[InterpolatedStringHandler]
public readonly ref struct MyHandler
{
    private readonly StringBuilder builder;

    // ReSharper disable once UnusedParameter.Local
#pragma warning disable IDE0060
    public MyHandler(int literalLength, int formattedCount)
    {
        builder = new StringBuilder(literalLength);
    }
#pragma warning restore IDE0060

    public void AppendLiteral(string s)
    {
        builder.Append(s);
    }

    public void AppendFormatted<T>(T t)
    {
        builder.Append(t);
    }

    internal string GetFormattedText() => builder.ToString();
}
