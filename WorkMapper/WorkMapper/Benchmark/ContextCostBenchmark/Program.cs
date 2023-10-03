namespace ContextCostBenchmark
{
    using System;

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

        private Action<object, object> map;

        private Action<object, object, object> mapWithContextNone;

        private Action<object, object, object> mapWithContextHalf;

        private Action<object, object, object> mapWithContextAll;

        [GlobalSetup]
        public void Setup()
        {
            var func = (Func<object, object>)(_ => null);
            var funcWithContext = (Func<object, object, object>)((_, _) => null);

            map = (_, _) =>
            {
                func(null);
                func(null);
                func(null);
                func(null);
                func(null);
                func(null);
                func(null);
                func(null);
            };
            mapWithContextNone = (_, _, _) =>
            {
                func(null);
                func(null);
                func(null);
                func(null);
                func(null);
                func(null);
                func(null);
                func(null);
            };
            mapWithContextHalf = (_, _, c) =>
            {
                func(null);
                funcWithContext(null, c);
                func(null);
                funcWithContext(null, c);
                func(null);
                funcWithContext(null, c);
                func(null);
                funcWithContext(null, c);
            };
            mapWithContextAll = (_, _, c) =>
            {
                funcWithContext(null, c);
                funcWithContext(null, c);
                funcWithContext(null, c);
                funcWithContext(null, c);
                funcWithContext(null, c);
                funcWithContext(null, c);
                funcWithContext(null, c);
                funcWithContext(null, c);
            };
        }

        [Benchmark(OperationsPerInvoke = N, Baseline = true)]
        public void Map()
        {
            var m = map;
            for (var i = 0; i < N; i++)
            {
                m(null, null);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void MapWithContextNone()
        {
            var m = mapWithContextNone;
            for (var i = 0; i < N; i++)
            {
                m(null, null, null);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void MapWithContextHalf()
        {
            var m = mapWithContextHalf;
            for (var i = 0; i < N; i++)
            {
                m(null, null, null);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void MapWithContextAll()
        {
            var m = mapWithContextAll;
            for (var i = 0; i < N; i++)
            {
                m(null, null, null);
            }
        }
    }
}
