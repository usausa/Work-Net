namespace BranchBenchmark2
{
    using System;
    using System.Linq;
    using System.Reflection.Emit;

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
            AddJob(Job.ShortRun);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class Benchmark
    {
        private const int N = 1000;

        private Func<int[], int[]> converter1;

        private Func<int[], int[]> converter2;

        private int[] source;

        [GlobalSetup]
        public void Setup()
        {
            var factory = new Factory();
            converter1 = factory.Create(true);
            converter2 = factory.Create(false);
            source = Enumerable.Range(1, 10).ToArray();
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void Short()
        {
            var s = source;
            var converter = converter1;
            for (var i = 0; i < N; i++)
            {
                converter(s);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void Default()
        {
            var s = source;
            var converter = converter2;
            for (var i = 0; i < N; i++)
            {
                converter(s);
            }
        }
    }

    public class Factory
    {
        public Func<int[], int[]> Create(bool isShort)
        {
            // Method
            var dynamicMethod = new DynamicMethod(
                "Converter",
                typeof(int[]),
                new[] { typeof(object), typeof(int[]) },
                true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            var sourceLocal = ilGenerator.DeclareLocal(typeof(int[]));
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stloc, sourceLocal);

            var destinationLocal = ilGenerator.DeclareLocal(typeof(int[]));
            var indexLocal = ilGenerator.DeclareLocal(typeof(int));

            var conditionLabel = ilGenerator.DefineLabel();
            var loopLabel = ilGenerator.DefineLabel();

            // Result
            ilGenerator.Emit(OpCodes.Ldloc, sourceLocal);
            ilGenerator.Emit(OpCodes.Ldlen);
            ilGenerator.Emit(OpCodes.Conv_I4);
            ilGenerator.Emit(OpCodes.Newarr, typeof(int));
            ilGenerator.Emit(OpCodes.Stloc, destinationLocal);

            // Loop
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Stloc, indexLocal);
            ilGenerator.Emit(isShort ? OpCodes.Br_S : OpCodes.Br, conditionLabel);

            // Loop start
            ilGenerator.MarkLabel(loopLabel);

            // Prepare result
            ilGenerator.Emit(OpCodes.Ldloc, destinationLocal);
            ilGenerator.Emit(OpCodes.Ldloc, indexLocal);

            // Get element
            ilGenerator.Emit(OpCodes.Ldloc, sourceLocal);
            ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
            ilGenerator.Emit(OpCodes.Ldelem, typeof(int));

            // Set element
            ilGenerator.Emit(OpCodes.Stelem, typeof(int));

            // Increment
            ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Add);
            ilGenerator.Emit(OpCodes.Stloc, indexLocal);

            // Condition
            ilGenerator.MarkLabel(conditionLabel);

            ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
            ilGenerator.Emit(OpCodes.Ldloc, sourceLocal);
            ilGenerator.Emit(OpCodes.Ldlen);
            ilGenerator.Emit(OpCodes.Conv_I4);
            ilGenerator.Emit(isShort ? OpCodes.Blt_S : OpCodes.Blt, loopLabel);

            // Return
            ilGenerator.Emit(OpCodes.Ldloc, destinationLocal);
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Func<int[], int[]>>();
        }
    }
}
