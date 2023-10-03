using System;
using System.Reflection.Emit;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BranchBenchmark
{
    public static class Program
    {
        public static void Main()
        {
            BenchmarkRunner.Run<MapperBenchmark>();
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
    public class MapperBenchmark
    {
        private const int N = 1000;

        private readonly Func<object, int> branchShort = Factory.Create(false);
        private readonly Func<object, int> branch = Factory.Create(false);

        [Benchmark(OperationsPerInvoke = N)]
        public int Branch0()
        {
            var arg = (object)null;
            var f = branch;
            var ret = 0;
            for (var i = 0; i < N; i++)
            {
                ret = f(arg);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int BranchS0()
        {
            var arg = (object)null;
            var f = branchShort;
            var ret = 0;
            for (var i = 0; i < N; i++)
            {
                ret = f(arg);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int Branch1()
        {
            var arg = string.Empty;
            var f = branch;
            var ret = 0;
            for (var i = 0; i < N; i++)
            {
                ret = f(arg);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int BranchS1()
        {
            var arg = string.Empty;
            var f = branchShort;
            var ret = 0;
            for (var i = 0; i < N; i++)
            {
                ret = f(arg);
            }
            return ret;
        }
    }

    public static class Factory
    {
        public static Func<object, int> Create(bool isShort)
        {
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(int), new[] { typeof(object), typeof(object) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            var falseLabel = ilGenerator.DefineLabel();

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(isShort ? OpCodes.Brfalse_S : OpCodes.Brfalse, falseLabel);

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Pop);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Pop);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Pop);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Pop);

            ilGenerator.Emit(OpCodes.Ldc_I4, 1);
            ilGenerator.Emit(OpCodes.Ret);

            ilGenerator.MarkLabel(falseLabel);

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Pop);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Pop);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Pop);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Pop);

            ilGenerator.Emit(OpCodes.Ldc_I4, 0);
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Func<object, int>>(null);
        }
    }
}
