using BenchmarkDotNet.Columns;

namespace FuncTypeBenchmark
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;

    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Running;

    using Smart.Reflection.Emit;

    public static class Program
    {
        public static void Main()
        {
            //ExpressionTest.Test<Data, int>(x => x.Value);
            //ExpressionTest.Test<Data, int>(x => 1);
            //ExpressionTest.Test<Data, int>(x => Method(x));
            BenchmarkRunner.Run<ExpressionBenchmark>();
        }

        private static int Method(Data data) => data.Value;
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
    public class ExpressionBenchmark
    {
        private const int N = 1_000_000;

        private readonly Data data = new Data { Value = 1 };

        private Func<Data, int> byFunction;
        private  Func<Data, int> byExpression; // TODO Faster than func
        private Func<Data, int> byEmit;

        [GlobalSetup]
        public void Setup()
        {
            byFunction = CodeFactory.CreateByFunc<Data, int>(x => x.Value);
            byExpression = CodeFactory.CreateByExpression<Data, int>(x => x.Value);
            byEmit = CodeFactory.CreateByEmit<Data, int>(x => x.Value);
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int ByFunction()
        {
            var ret = 0;
            for (var i = 0; i < N; i++)
            {
                ret = byFunction(data);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int ByExpression()
        {
            var ret = 0;
            for (var i = 0; i < N; i++)
            {
                ret = byExpression(data);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int ByEmit()
        {
            var ret = 0;
            for (var i = 0; i < N; i++)
            {
                ret = byEmit(data);
            }

            return ret;
        }
    }

    public class Data
    {
        public int Value { get; set; }
    }

    public static class CodeFactory
    {
        public static Func<TS, TM> CreateByFunc<TS, TM>(Func<TS, TM> function)
        {
            return function;
        }

        public static Func<TS, TM> CreateByExpression<TS, TM>(Expression<Func<TS, TM>> expression)
        {
            return expression.Compile();
        }

        public static Func<TS, TM> CreateByEmit<TS, TM>(Expression<Func<TS, TM>> expression)
        {
            var memberExpression = (MemberExpression)expression.Body;
            var pi = (PropertyInfo)memberExpression.Member;

            var dynamicMethod = new DynamicMethod(string.Empty, typeof(TM), new[] { typeof(object), typeof(TS) }, true);
            var il = dynamicMethod.GetILGenerator();

            if (!pi.GetGetMethod().IsStatic)
            {
                il.Emit(OpCodes.Ldarg_1);
            }

            il.EmitCallMethod(pi.GetGetMethod());

            il.Emit(OpCodes.Ret);

            return (Func<TS, TM>)dynamicMethod.CreateDelegate(typeof(Func<TS, TM>), new object());
        }
    }
}
