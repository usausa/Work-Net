namespace EmitDefaultBenchmark
{
    using System;
    using System.Reflection.Emit;

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

        private readonly Func<int> customIntFunc = Factory.CreateIntFunc();
        private readonly Func<int?> customNullableFunc = Factory.CreateNullableFunc();
        private readonly Func<string> customClassFunc = Factory.CreateClassFunc();
        private readonly Func<Data> customStructFunc = Factory.CreateStructFunc();
        private readonly Func<int> genericIntFunc = Factory.CreateFunc<int>();
        private readonly Func<int?> genericNullableFunc = Factory.CreateFunc<int?>();
        private readonly Func<string> genericClassFunc = Factory.CreateFunc<string>();
        private readonly Func<Data> genericStructFunc = Factory.CreateFunc<Data>();

        [Benchmark(OperationsPerInvoke = N)]
        public int CustomInt()
        {
            var f = customIntFunc;
            var ret = default(int);
            for (var i = 0; i < N; i++)
            {
                ret = f();
            }

            return ret;
        }


        [Benchmark(OperationsPerInvoke = N)]
        public int GenericInt()
        {
            var f = genericIntFunc;
            var ret = default(int);
            for (var i = 0; i < N; i++)
            {
                ret = f();
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int? CustomNullable()
        {
            var f = customNullableFunc;
            var ret = default(int?);
            for (var i = 0; i < N; i++)
            {
                ret = f();
            }

            return ret;
        }


        [Benchmark(OperationsPerInvoke = N)]
        public int? GenericNullable()
        {
            var f = genericNullableFunc;
            var ret = default(int?);
            for (var i = 0; i < N; i++)
            {
                ret = f();
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public string CustomClass()
        {
            var f = customClassFunc;
            var ret = default(string);
            for (var i = 0; i < N; i++)
            {
                ret = f();
            }

            return ret;
        }


        [Benchmark(OperationsPerInvoke = N)]
        public string GenericClass()
        {
            var f = genericClassFunc;
            var ret = default(string);
            for (var i = 0; i < N; i++)
            {
                ret = f();
            }

            return ret;
        }


        [Benchmark(OperationsPerInvoke = N)]
        public Data CustomStruct()
        {
            var f = customStructFunc;
            var ret = default(Data);
            for (var i = 0; i < N; i++)
            {
                ret = f();
            }

            return ret;
        }


        [Benchmark(OperationsPerInvoke = N)]
        public Data GenericStruct()
        {
            var f = genericStructFunc;
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
        public static Func<int> CreateIntFunc()
        {
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(int), new[] { typeof(object) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldc_I4_0);

            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Func<int>>(null);
        }

        public static Func<int?> CreateNullableFunc()
        {
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(int?), new[] { typeof(object) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            var local = ilGenerator.DeclareLocal(typeof(int?));
            ilGenerator.EmitLdloca(local);
            ilGenerator.Emit(OpCodes.Initobj, typeof(int?));
            ilGenerator.EmitLdloc(local);

            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Func<int?>>(null);
        }

        public static Func<string> CreateClassFunc()
        {
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(string), new[] { typeof(object) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldnull);

            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Func<string>>(null);
        }

        public static Func<Data> CreateStructFunc()
        {
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(Data), new[] { typeof(object) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            var local = ilGenerator.DeclareLocal(typeof(Data));
            ilGenerator.EmitLdloca(local);
            ilGenerator.Emit(OpCodes.Initobj, typeof(Data));
            ilGenerator.EmitLdloc(local);

            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Func<Data>>(null);
        }

        public static Func<T> CreateFunc<T>()
        {
            var type = typeof(T);
            var dynamicMethod = new DynamicMethod(string.Empty, type, new[] { typeof(object) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            //var method = typeof(Factory).GetMethod("GetDefaultValue")!.MakeGenericMethod(type);
            //ilGenerator.Emit(OpCodes.Call, method);
            ilGenerator.EmitStackDefaultValue(type);

            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Func<T>>(null);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static T GetDefaultValue<T>() => default;
    }

    public struct Data
    {
        public int X;
        public int Y;
    }
}
