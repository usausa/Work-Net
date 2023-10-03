using System.Runtime.CompilerServices;

namespace DispatchBenchmark
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

        private object[] objects;

        private object[] objects2;

        private IAction[] actions;

        [GlobalSetup]
        public void Setup()
        {
            var action = new NopAction();
            Action<object, object> nop = (_, _) => { };
            Action<object, object, object> nopWithContext = (_, _, _) => { };

            objects = new object[]
            {
                actions,
                nop,
                nopWithContext
            };

            objects2 = new object[]
            {
                nop,
                nop,
                nop
            };

            actions = new IAction[]
            {
                action,
                new ActionDelegateHolder(nop),
                new ActionDelegateWithContextHolder(nopWithContext),
            };
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void TypeDispatch()
        {
            var array = objects;
            for (var i = 0; i < N; i++)
            {
                foreach (var obj in array)
                {
                    if (obj is Action<object, object> action)
                    {
                        action(null, null);
                    }
                    else if (obj is Action<object, object, object> actionWithContext)
                    {
                        actionWithContext(null, null, null);
                    }
                    else if (obj is IAction actionInterface)
                    {
                        actionInterface.Action(null, null, null);
                    }
                }
            }
        }

        // Faster if condition
        [Benchmark(OperationsPerInvoke = N)]
        public void TypeDispatch2()
        {
            var array = objects2;
            for (var i = 0; i < N; i++)
            {
                foreach (var obj in array)
                {
                    if (obj is Action<object, object> action)
                    {
                        action(null, null);
                    }
                    else if (obj is Action<object, object, object> actionWithContext)
                    {
                        actionWithContext(null, null, null);
                    }
                    else if (obj is IAction actionInterface)
                    {
                        actionInterface.Action(null, null, null);
                    }
                }
            }
        }

        // Faster
        [Benchmark(OperationsPerInvoke = N)]
        public void InterfaceDispatch()
        {
            var array = actions;
            for (var i = 0; i < N; i++)
            {
                foreach (var action in array)
                {
                    action.Action(null, null, null);
                }
            }
        }
    }

    public sealed class NopAction : IAction
    {
        public void Action(object s, object d, object c)
        {
        }
    }

    public interface IAction
    {
        void Action(object s, object d, object c);
    }

    public sealed class ActionDelegateHolder : IAction
    {
        private readonly Action<object, object> action;

        public ActionDelegateHolder(Action<object, object> action)
        {
            this.action = action;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Action(object s, object d, object c) => action(s, d);
    }

    public sealed class ActionDelegateWithContextHolder : IAction
    {
        private readonly Action<object, object, object> action;

        public ActionDelegateWithContextHolder(Action<object, object, object> action)
        {
            this.action = action;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Action(object s, object d, object c) => action(s, d, c);
    }
}
