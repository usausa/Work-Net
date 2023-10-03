using System;
using System.Reflection.Emit;

namespace DupBenchmark
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

        private Func<Data> dupFactory = Factory.CreateDupFunc();
        private Func<Data> localFactory = Factory.CreateDupFunc();

        [Benchmark(OperationsPerInvoke = N)]
        public Data DupFactory()
        {
            var f = dupFactory;
            var ret = default(Data);
            for (var i = 0; i < N; i++)
            {
                ret = f();
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public Data LocalFactory()
        {
            var f = localFactory;
            var ret = default(Data);
            for (var i = 0; i < N; i++)
            {
                ret = f();
            }
            return ret;
        }
    }

    public static class Factory
    {
        public static Func<Data> CreateDupFunc()
        {
            var type = typeof(Data);
            var ctor = type.GetConstructor(Type.EmptyTypes)!;
            var property = type.GetProperty("Value")!;

            // Func
            var dynamicMethod = new DynamicMethod(string.Empty, type, new[] { typeof(object) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            // Class new
            ilGenerator.Emit(OpCodes.Newobj, ctor);

            // Set 1
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Callvirt, property.SetMethod!);

            // Return
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Func<Data>>();
        }

        public static Func<Data> CreateLocalFunc()
        {
            var type = typeof(Data);
            var ctor = type.GetConstructor(Type.EmptyTypes)!;
            var property = type.GetProperty("Value")!;

            // Func
            var dynamicMethod = new DynamicMethod(string.Empty, type, new[] { typeof(object) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();
            var local = ilGenerator.DeclareLocal(type);

            // Class new
            ilGenerator.Emit(OpCodes.Newobj, ctor);

            ilGenerator.Emit(OpCodes.Stloc_0, local);

            // Set 1
            ilGenerator.Emit(OpCodes.Ldloc_0, local);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Callvirt, property.SetMethod!);

            ilGenerator.Emit(OpCodes.Ldloc_0, local);

            // Return
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Func<Data>>();
        }
    }

    public class Data
    {
        public int Value { get; set; }
    }
}
