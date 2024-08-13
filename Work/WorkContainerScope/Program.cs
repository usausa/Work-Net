namespace WorkContainerScope;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

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
        AddExporter(MarkdownExporter.GitHub);
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
    private const int N = 1000;

    private Func<IResolver, object> singleton1 = default!;
    private Func<IResolver, object> singleton2 = default!;

    [GlobalSetup]
    public void Setup()
    {
        singleton1 = new SingletonScope().Create(() => new object());
        singleton2 = new SingletonScope2().Create(() => new object());
    }

    [Benchmark]
    public void Scope1()
    {
        for (var i = 0; i < N; i++)
        {
            singleton1(null!);
        }
    }

    [Benchmark]
    public void Scope2()
    {
        for (var i = 0; i < N; i++)
        {
            singleton2(null!);
        }
    }
}

public sealed class SingletonScope2 : IScope
{
    private Func<IResolver, object>? objectFactory;

    public Func<IResolver, object> Create(Func<object> factory)
    {
        if (objectFactory is null)
        {
            var holder = new SingletonHolder(factory);
            objectFactory = holder.Resolve;
        }

        return objectFactory;
    }
}

public sealed class SingletonHolder
{
    private readonly object value;

    public SingletonHolder(object value)
    {
        this.value = value;
    }

    public object Resolve(IResolver resolver) => value;
}


public sealed class SingletonScope : IScope
{
    private object? value;

    private Func<IResolver, object>? objectFactory;

    public Func<IResolver, object> Create(Func<object> factory)
    {
        if (objectFactory is null)
        {
            value = factory();
            objectFactory = _ => value;
        }

        return objectFactory;
    }
}

public interface IScope
{
    Func<IResolver, object> Create(Func<object> factory);
}

public interface IResolver
{
    object Resolve(Type type);
}
