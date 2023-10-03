namespace CastCostBenchmark
{
    using System;
    using System.Runtime.CompilerServices;

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

        private readonly Func<string, string> tc2c = Converter.ClassToClass;
        private readonly Func<string, int> tc2s = Converter.ClassToStruct;
        private readonly Func<int, int> ts2s = Converter.StructToStruct;
        private readonly Func<int, string> ts2c = Converter.StructToClass;

        private readonly Func<object, object> oc2c = x => Converter.ClassToClass((string)x);
        private readonly Func<object, object> oc2s = x => Converter.ClassToStruct((string)x);
        private readonly Func<object, object> os2s = x => Converter.StructToStruct((int)x);
        private readonly Func<object, object> os2c = x => Converter.StructToClass((int)x);

        [Benchmark(OperationsPerInvoke = N)]
        public string TypedClassToClass()
        {
            var ret = default(string);
            for (var i = 0; i < N; i++)
            {
                ret = Converter.TypedConvert(tc2c, string.Empty);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int TypedClassToStruct()
        {
            var ret = default(int);
            for (var i = 0; i < N; i++)
            {
                ret = Converter.TypedConvert(tc2s, string.Empty);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int TypedStructToStruct()
        {
            var ret = default(int);
            for (var i = 0; i < N; i++)
            {
                ret = Converter.TypedConvert(ts2s, 0);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public string TypedStructToClass()
        {
            var ret = default(string);
            for (var i = 0; i < N; i++)
            {
                ret = Converter.TypedConvert(ts2c, 0);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public object ObjectClassToClass()
        {
            var ret = default(object);
            for (var i = 0; i < N; i++)
            {
                ret = Converter.Convert(oc2c, string.Empty);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public object ObjectClassToStruct()
        {
            var ret = default(object);
            for (var i = 0; i < N; i++)
            {
                ret = Converter.Convert(oc2s, string.Empty);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public object ObjectStructToStruct()
        {
            var ret = default(object);
            for (var i = 0; i < N; i++)
            {
                ret = Converter.Convert(os2s, 0);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public object ObjectStructToClass()
        {
            var ret = default(object);
            for (var i = 0; i < N; i++)
            {
                ret = Converter.Convert(os2c, 0);
            }
            return ret;
        }
    }

    public static class Converter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD TypedConvert<TS, TD>(Func<TS, TD> converter, TS source) => converter(source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Convert(Func<object, object> converter, object source) => converter(source);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string ClassToClass(string s) => string.Empty;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int ClassToStruct(string s) => 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int StructToStruct(int i) => 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string StructToClass(int i) => string.Empty;
    }
}
