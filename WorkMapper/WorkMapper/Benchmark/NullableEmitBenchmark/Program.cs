namespace NullableEmitBenchmark
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;

    using Smart.Reflection.Emit;

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

        private readonly Func<Data, bool> hasValue1 = Factory.CreateHasValue1<int>();
        private readonly Func<Data, bool> hasValue2 = Factory.CreateHasValue2<int>();
        private readonly Func<Data, bool> hasValue3;
        private readonly Func<Data, bool> hasValue4;
        private readonly Func<Data, int> getValue1 = Factory.CreateGetValue1<int>();
        private readonly Func<Data, int> getValue2 = Factory.CreateGetValue2<int>();
        private readonly Func<Data, int> getValue3;
        private readonly Func<Data, int> getValue4;

        public Benchmark()
        {
            hasValue3 = HasValue3Func;
            hasValue4 = HasValue4Func;
            getValue3 = GetValue3Func;
            getValue4 = GetValue3Func;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public bool HasValue1()
        {
            var f = hasValue1;
            var ret = default(bool);
            var data = new Data { Value = 0 };
            for (var i = 0; i < N; i++)
            {
                ret = f(data);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public bool HasValue2()
        {
            var f = hasValue2;
            var ret = default(bool);
            var data = new Data { Value = 0 };
            for (var i = 0; i < N; i++)
            {
                ret = f(data);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public bool HasValue3()
        {
            var f = hasValue3;
            var ret = default(bool);
            var data = new Data { Value = 0 };
            for (var i = 0; i < N; i++)
            {
                ret = f(data);
            }

            return ret;
        }

        public bool HasValue3Func(Data data) => data.Value.HasValue;

        [Benchmark(OperationsPerInvoke = N)]
        public bool HasValue4()
        {
            var f = hasValue4;
            var ret = default(bool);
            var data = new Data { Value = 0 };
            for (var i = 0; i < N; i++)
            {
                ret = f(data);
            }

            return ret;
        }

        public bool HasValue4Func(Data data) => HasValue(data.Value);

        [Benchmark(OperationsPerInvoke = N)]
        public int GetValue1()
        {
            var f = getValue1;
            var ret = default(int);
            var data = new Data { Value = 0 };
            for (var i = 0; i < N; i++)
            {
                ret = f(data);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int GetValue2()
        {
            var f = getValue2;
            var ret = default(int);
            var data = new Data { Value = 0 };
            for (var i = 0; i < N; i++)
            {
                ret = f(data);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int GetValue3()
        {
            var f = getValue3;
            var ret = default(int);
            var data = new Data { Value = 0 };
            for (var i = 0; i < N; i++)
            {
                ret = f(data);
            }

            return ret;
        }

        public int GetValue3Func(Data data) => data.Value!.Value;

        [Benchmark(OperationsPerInvoke = N)]
        public int GetValue4()
        {
            var f = getValue4;
            var ret = default(int);
            var data = new Data { Value = 0 };
            for (var i = 0; i < N; i++)
            {
                ret = f(data);
            }

            return ret;
        }

        public int GetValue4Func(Data data) => GetValue(data.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasValue<T>(T? value) where T : struct => value.HasValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T GetValue<T>(T? value) where T : struct => value!.Value;
    }

    public static class ILGeneratorExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasValue<T>(T? value) where T : struct => value.HasValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T GetValue<T>(T? value) where T : struct => value!.Value;

        public static void EmitNullableHasValue(this ILGenerator il, Type underlyingType)
        {
            var method = typeof(ILGeneratorExtensions).GetMethod(nameof(HasValue), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(underlyingType);
            il.Emit(OpCodes.Call, method);
        }

        public static void EmitNullableGetValue(this ILGenerator il, Type underlyingType)
        {
            var method = typeof(ILGeneratorExtensions).GetMethod(nameof(GetValue), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(underlyingType);
            il.Emit(OpCodes.Call, method);
        }
    }

    public static class Factory
    {
        public static Func<Data, bool> CreateHasValue1<T>()
            where T : struct
        {
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(bool), new[] { typeof(object), typeof(Data) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.EmitCallMethod(typeof(Data).GetProperty("Value")!.GetMethod!);

            var local = ilGenerator.DeclareLocal(typeof(T?));
            ilGenerator.EmitStloc(local);
            ilGenerator.EmitLdloca(local);
            ilGenerator.Emit(OpCodes.Call, typeof(T?).GetProperty("HasValue")!.GetMethod!);

            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Func<Data, bool>>(new object());
        }

        public static Func<Data, T> CreateGetValue1<T>()
            where T : struct
        {
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(int), new[] { typeof(object), typeof(Data) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.EmitCallMethod(typeof(Data).GetProperty("Value")!.GetMethod!);

            var local = ilGenerator.DeclareLocal(typeof(T?));
            ilGenerator.EmitStloc(local);
            ilGenerator.EmitLdloca(local);
            ilGenerator.Emit(OpCodes.Call, typeof(T?).GetProperty("Value")!.GetMethod!);

            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Func<Data, T>>(new object());
        }

        public static Func<Data, bool> CreateHasValue2<T>()
            where T : struct
        {
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(bool), new[] { typeof(object), typeof(Data) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.EmitCallMethod(typeof(Data).GetProperty("Value")!.GetMethod!);

            ilGenerator.EmitNullableHasValue(typeof(T));

            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Func<Data, bool>>(new object());
        }

        public static Func<Data, T> CreateGetValue2<T>()
            where T : struct
        {
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(int), new[] { typeof(object), typeof(Data) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.EmitCallMethod(typeof(Data).GetProperty("Value")!.GetMethod!);

            ilGenerator.EmitNullableGetValue(typeof(T));

            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Func<Data, T>>(new object());
        }
    }

    public class Data
    {
        public int? Value { get; set; }
    }

}
