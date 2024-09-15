namespace Benchmark;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;

[Config(typeof(BenchmarkConfig))]
public class CastVsAsBenchmark
{
    private readonly object o = new Factory();

    [Benchmark]
    public object ByCast() => ((IFactory)o).Create();

    [Benchmark]
    public void ByAs() => Unsafe.As<IFactory>(o).Create();

    public interface IFactory
    {
        object Create();
    }

    public class Factory : IFactory
    {
        public object Create() => null;
    }
}
