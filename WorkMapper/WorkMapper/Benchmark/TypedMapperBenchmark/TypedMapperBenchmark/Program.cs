using System;

namespace TypedMapperBenchmark
{
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
        private const int N = 1000;

        public readonly DelegateMapper<int, int> StructToStructMapper = new(_ => 0);
        public readonly DelegateMapper<int, string> StructToClassMapper = new(_ => null);
        public readonly DelegateMapper<string, int> ClassToStructMapper = new(_ => 0);
        public readonly DelegateMapper<string, string> ClassToClassMapper = new(_ => null);

        [Benchmark(OperationsPerInvoke = N)]
        public int StructToStruct()
        {
            var ret = 0;
            for (var i = 0; i < N; i++)
            {
                ret = StructToStructMapper.Map(0);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public string StructToClass()
        {
            var ret = default(string);
            for (var i = 0; i < N; i++)
            {
                ret = StructToClassMapper.Map(0);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int ClassToStruct()
        {
            var ret = 0;
            for (var i = 0; i < N; i++)
            {
                ret = ClassToStructMapper.Map(null);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public string ClassToClass()
        {
            var ret = default(string);
            for (var i = 0; i < N; i++)
            {
                ret = ClassToClassMapper.Map(null);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public object StructToStruct2()
        {
            var ret = default(object);
            for (var i = 0; i < N; i++)
            {
                ret = StructToStructMapper.Map2(0);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public object StructToClass2()
        {
            var ret = default(object);
            for (var i = 0; i < N; i++)
            {
                ret = StructToClassMapper.Map2(0);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public object ClassToStruct2()
        {
            var ret = default(object);
            for (var i = 0; i < N; i++)
            {
                ret = ClassToStructMapper.Map2(null);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public object ClassToClass2()
        {
            var ret = default(object);
            for (var i = 0; i < N; i++)
            {
                ret = ClassToClassMapper.Map2(null);
            }
            return ret;
        }
    }

    public interface IObjectMapper
    {
        object Map2(object source);
    }

    public interface ITypedMapper<in TSource, out TDestination>
    {
        TDestination Map(TSource source);
    }

    public sealed class DelegateMapper<TSource, TDestination> : ITypedMapper<TSource, TDestination>, IObjectMapper
    {
        private readonly Func<TSource, TDestination> func;

        public DelegateMapper(Func<TSource, TDestination> func)
        {
            this.func = func;
        }

        public TDestination Map(TSource source) => func(source);

        public object Map2(object source) => func((TSource)source);
    }
}
