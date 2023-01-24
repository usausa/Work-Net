namespace WorkBenchmarkInlineInterface;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

using System.Runtime.CompilerServices;

public static class Program
{
    public static void Main()
    {
        _ = BenchmarkRunner.Run<Benchmark>();
    }
}

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        _ = AddExporter(MarkdownExporter.GitHub);
        _ = AddColumn(
            StatisticColumn.Mean,
            StatisticColumn.Min,
            StatisticColumn.Max,
            StatisticColumn.P90,
            StatisticColumn.Error,
            StatisticColumn.StdDev);
        _ = AddDiagnoser(MemoryDiagnoser.Default);
        _ = AddJob(Job.MediumRun);
    }
}

[Config(typeof(BenchmarkConfig))]
public class Benchmark
{
    private const int N = 1000;

    private readonly Calculator inlineCalculator = new InlineCalculator();
    private readonly Calculator noInlineCalculator = new InlineCalculator();

    [Benchmark]
    public int Inline()
    {
        var ret = 0;
        for (var i = 0; i < N; i++)
        {
            ret = inlineCalculator.Calc(ret, i);
        }

        return ret;
    }

    [Benchmark]
    public int NoInline()
    {
        var ret = 0;
        for (var i = 0; i < N; i++)
        {
            ret = noInlineCalculator.Calc(ret, i);
        }

        return ret;
    }
}

public abstract class Calculator
{
    public abstract int Calc(int x, int y);
}

public sealed class InlineCalculator : Calculator
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Calc(int x, int y) => x + y;
}

public sealed class NoInlineCalculator : Calculator
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int Calc(int x, int y) => x + y;
}
