using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace CheckBenchmark;

public static class Program
{
    public static void Main()
    {
        BenchmarkRunner.Run<Benchmark>();
    }
}

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddExporter(MarkdownExporter.Default, MarkdownExporter.GitHub);
        AddColumn(
            StatisticColumn.Mean,
            StatisticColumn.Min,
            StatisticColumn.Max,
            StatisticColumn.P90,
            StatisticColumn.Error,
            StatisticColumn.StdDev);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddJob(Job.MediumRun);
    }
}

[Config(typeof(BenchmarkConfig))]
public class Benchmark
{
    // 0.21  0
    // 0.21  0
    // 0.43 24
    // 0.49 24

    private const int N = 1000;

    [Benchmark(OperationsPerInvoke = N)]
    public void ClassDispose()
    {
        for (var i = 0; i < N; i++)
        {
            using var _ = new ClassDisposable();
        }
    }

    [Benchmark(OperationsPerInvoke = N)]
    public void StructDispose()
    {
        for (var i = 0; i < N; i++)
        {
            using var _ = new StructDisposable();
        }
    }


    [Benchmark(OperationsPerInvoke = N)]
    public void ClassArgument()
    {
        for (var i = 0; i < N; i++)
        {
            Call(new ClassDisposable());
        }
    }

    [Benchmark(OperationsPerInvoke = N)]
    public void StructArgument()
    {
        for (var i = 0; i < N; i++)
        {
            Call(new StructDisposable());
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Call(IDisposable disposable)
    {
        using var _ = disposable;
    }
}

public sealed class ClassDisposable : IDisposable
{
    public void Dispose()
    {
    }
}

public struct StructDisposable : IDisposable
{
    public void Dispose()
    {
    }
}
