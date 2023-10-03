using System;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace ConvertArrayBenchmark
{
    public static class Program
    {
        public static void Main()
        {
            BenchmarkRunner.Run<Benchmark>();
        }
    }

    // IE fast LINQ
    // List/IList fast 2
    // IList / List 20%?
    // with source check equal ?
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
    public unsafe class Benchmark
    {
        private const int N = 1000;

        private long[] array;

        private readonly Func<long, int> convert;

        private readonly delegate*<long, int> functionPointer;

        private readonly InstanceConverter converter = new();

        public Benchmark()
        {
            convert = Converter.Convert;
            functionPointer = &Converter.Convert;
            converter.converter = convert;
        }


        [Params(0, 4, 16, 32, 64)]
        public int Size { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            array = new long[Size];
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void StaticDirect()
        {
            var source = array;
            for (var i = 0; i < N; i++)
            {
                StaticConverter.ConvertDirect(source);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void StaticDirectNoInline()
        {
            var source = array;
            for (var i = 0; i < N; i++)
            {
                StaticConverter.ConvertDirectNoInline(source);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void StaticWithFunctionPointer()
        {
            var source = array;
            for (var i = 0; i < N; i++)
            {
                StaticConverter.ConvertWithFunctionPointer(source, functionPointer);
            }
        }


        [Benchmark(OperationsPerInvoke = N)]
        public void StaticWithFunc()
        {
            var source = array;
            for (var i = 0; i < N; i++)
            {
                StaticConverter.ConvertWithFunc(source, convert);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void ConvertByFunc()
        {
            var source = array;
            for (var i = 0; i < N; i++)
            {
                converter.Convert(source);
            }
        }
    }

    public static class Converter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Convert(long value) => (int)value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int ConvertNoInline(long value) => (int)value;
    }

    public static class StaticConverter
    {
        public static int[] ConvertDirect(long[] source)
        {
            var array = new int[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                array[i] = Converter.Convert(source[i]);
            }
            return array;
        }

        public static int[] ConvertDirectNoInline(long[] source)
        {
            var array = new int[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                array[i] = Converter.ConvertNoInline(source[i]);
            }
            return array;
        }

        public static int[] ConvertWithFunc(long[] source, Func<long, int> converter)
        {
            var array = new int[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                array[i] = converter(source[i]);
            }
            return array;
        }

        public static unsafe int[] ConvertWithFunctionPointer(long[] source, delegate*<long, int> converter)
        {
            var array = new int[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                array[i] = converter(source[i]);
            }
            return array;
        }
    }

    public sealed class InstanceConverter
    {
        // ReSharper disable once InconsistentNaming
        public Func<long, int> converter;

        public int[] Convert(long[] source)
        {
            var c = converter;
            var array = new int[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                array[i] = c(source[i]);
            }
            return array;
        }
    }
}
