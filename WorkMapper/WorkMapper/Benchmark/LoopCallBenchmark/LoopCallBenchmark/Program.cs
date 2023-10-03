namespace LoopCallBenchmark
{
    using System;
    using System.Linq;

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

        [Params(2, 4, 8, 12, 16, 20, 24, 32)]
        public int Size { get; set; }

        private IHandler[] handlers;
        private Action<object, object>[] actions;

        [GlobalSetup]
        public void Setup()
        {
            handlers = Enumerable.Range(1, Size).Select(_ => (IHandler)new Handler()).ToArray();
            actions = Enumerable.Range(1, Size).Select(_ => (Action<object, object>)((_, _) => { })).ToArray();
        }

        [Benchmark(OperationsPerInvoke = N, Baseline = true)]
        public void HandlerFor()
        {
            for (var n = 0; n < N; n++)
            {
                var array = handlers;
                for (var i = 0; i < array.Length; i++)
                {
                    array[i].Process(null, null);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void ActionFor()
        {
            for (var n = 0; n < N; n++)
            {
                var array = actions;
                for (var i = 0; i < array.Length; i++)
                {
                    array[i](null, null);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void HandlerEach()
        {
            for (var n = 0; n < N; n++)
            {
                foreach (var handler in handlers)
                {
                    handler.Process(null, null);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void ActionEach()
        {
            for (var n = 0; n < N; n++)
            {
                foreach (var action in actions)
                {
                    action(null, null);
                }
            }
        }
    }

    public interface IHandler
    {
        public void Process(object arg1, object arg2);
    }

    public sealed class Handler : IHandler
    {
        public void Process(object arg1, object arg2)
        {
        }
    }
}
