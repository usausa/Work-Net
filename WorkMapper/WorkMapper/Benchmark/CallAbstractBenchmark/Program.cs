namespace CallAbstractBenchmark
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

        private IAction action1;
        private IActionWithContext action2;
        private IAction action3;
        private IActionWithContext action4;

        private IAction[] simpleActions;

        private ActionEntry[] entries;

        private IActionWithContext[] actionWithContexts;

        [GlobalSetup]
        public void Setup()
        {
            action1 = new NopAction();
            action2 = new NopActionWithContext();
            action3 = new NopAction();
            action4 = new NopActionWithContext();

            simpleActions = new[] { action1, action3, action1, action3 };

            entries = new[]
            {
                new ActionEntry { Action = action1 },
                new ActionEntry { ActionWithContext = action2 },
                new ActionEntry { Action = action3 },
                new ActionEntry { ActionWithContext = action4 },
            };

            actionWithContexts = new[]
            {
                new ActionWrapper(action1),
                action2,
                new ActionWrapper(action3),
                action4
            };
        }

        // 8.437
        [Benchmark(OperationsPerInvoke = N)]
        public void Raw()
        {
            for (var i = 0; i < N; i++)
            {
                action1.Process(null);
                action3.Process(null);
                action1.Process(null);
                action3.Process(null);
            }
        }

        // 10.690
        [Benchmark(OperationsPerInvoke = N)]
        public void Simple()
        {
            for (var i = 0; i < N; i++)
            {
                for (var j = 0; j < entries.Length; j++)
                {
                    simpleActions[j].Process(null);
                }
            }
        }

        // 8.271
        [Benchmark(OperationsPerInvoke = N)]
        public void MixRaw()
        {
            for (var i = 0; i < N; i++)
            {
                action1.Process(null);
                action2.Process(null, null);
                action3.Process(null);
                action4.Process(null, null);
            }
        }

        // 13.404 Better than Wrap
        [Benchmark(OperationsPerInvoke = N)]
        public void MixEntry()
        {
            for (var i = 0; i < N; i++)
            {
                for (var j = 0; j < entries.Length; j++)
                {
                    var entry = entries[j];
                    if (entry.ActionWithContext is null)
                    {
                        entry.Action.Process(null);
                    }
                    else
                    {
                        entry.ActionWithContext.Process(null, null);
                    }
                }
            }
        }

        // 17.047
        [Benchmark(OperationsPerInvoke = N)]
        public void MixWrap()
        {
            for (var i = 0; i < N; i++)
            {
                for (var j = 0; j < actionWithContexts.Length; j++)
                {
                    actionWithContexts[j].Process(null, null);
                }
            }
        }
    }

    // Entry

    public sealed class ActionEntry
    {
        public IAction Action;

        public IActionWithContext ActionWithContext;
    }

    // Wrap

    public sealed class ActionWrapper : IActionWithContext
    {
        private readonly IAction action;

        public ActionWrapper(IAction action)
        {
            this.action = action;
        }

        public void Process(object value, object context) => action.Process(value);
    }

    // Raw

    public interface IAction
    {
        void Process(object value);
    }

    public interface IActionWithContext
    {
        void Process(object value, object context);
    }

    public sealed class NopAction : IAction
    {
        public void Process(object value)
        {
        }
    }

    public sealed class NopActionWithContext : IActionWithContext
    {
        public void Process(object value, object context)
        {
        }
    }
}
