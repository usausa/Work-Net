using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Work2Optimized
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
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

    // int, string

    [Config(typeof(BenchmarkConfig))]
    public class IntBenchmark
    {
        public IEnumerable<object[]> ArrayData()
        {
            //yield return new object[] { new int[1], 0 };
            yield return new object[] { new int[4], -1 };
            yield return new object[] { new int[16], -1 };
            yield return new object[] { new int[64], -1 };
            yield return new object[] { new int[256], -1 };
            //yield return new object[] { new int[1024], -1 };
            yield return new object[] { new int[4096], -1 };
        }

        public IEnumerable<object[]> ListData()
        {
            yield return new object[] { new List<int>(new int[1]), 0 };
            yield return new object[] { new List<int>(new int[4]), -1 };
            yield return new object[] { new List<int>(new int[16]), -1 };
            yield return new object[] { new List<int>(new int[64]), -1 };
            //yield return new object[] { new List<int>(new int[256]), -1 };
            yield return new object[] { new List<int>(new int[1024]), -1 };
        }

        [ArgumentsSource(nameof(ArrayData))]
        [Benchmark] public int Span(int[] values, int find) => Finder.Span<int>(values, x => x == find);

        [ArgumentsSource(nameof(ArrayData))]
        [Benchmark] public int SpanPointer(int[] values, int find) => Finder.SpanPointer<int>(values, x => x == find);

        [ArgumentsSource(nameof(ArrayData))]
        [Benchmark] public int SpanPointer2(int[] values, int find) => Finder.SpanPointer2<int>(values, x => x == find);

        [ArgumentsSource(nameof(ArrayData))]
        [Benchmark] public int SpanUnsafeWhile(int[] values, int find) => Finder.SpanUnsafeWhile<int>(values, x => x == find);

        [ArgumentsSource(nameof(ArrayData))]
        [Benchmark] public int SpanUnsafeFor(int[] values, int find) => Finder.SpanUnsafeFor<int>(values, x => x == find);

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int Array(int[] values, int find) => Finder.Array(values, x => x == find);

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int ArrayPointer(int[] values, int find) => Finder.ArrayPointer(values, x => x == find);

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int ArrayPointer2(int[] values, int find) => Finder.ArrayPointer2(values, x => x == find);

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int ArrayRangeMax(int[] values, int find) => Finder.ArrayRangeMax(values, 0, values.Length, x => x == find);

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int ArrayRangeLengthDec(int[] values, int find) => Finder.ArrayRangeLengthDec(values, 0, values.Length, x => x == find);

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int ArrayRangeWhile(int[] values, int find) => Finder.ArrayRangeWhile(values, 0, values.Length, x => x == find);

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int ArrayRangePointer(int[] values, int find) => Finder.ArrayRangePointer(values, 0, values.Length, x => x == find);

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int ArrayRangePointer2(int[] values, int find) => Finder.ArrayRangePointer2(values, 0, values.Length, x => x == find);

        // List

        //[ArgumentsSource(nameof(ListData))]
        //[Benchmark] public int List(List<int> values, int find) => Finder.List(values, x => x == find);

        //[ArgumentsSource(nameof(ListData))]
        //[Benchmark] public int ListRange(List<int> values, int find) => Finder.ListRangeMax(values, 0, values.Count, x => x == find);

        // Interface

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int InterfaceList(int[] values, int find) => Finder.InterfaceList(values, x => x == find);

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int InterfaceListRangeMax(int[] values, int find) => Finder.InterfaceListRangeMax(values, 0, values.Length, x => x == find);

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int Enumerable(int[] values, int find) => Finder.Enumerable(values, x => x == find);
    }

        [Config(typeof(BenchmarkConfig))]
    public class StringBenchmark
    {
        public IEnumerable<object?[]> ArrayData()
        {
            //yield return new object?[] { new string?[1], null };
            yield return new object?[] { new string?[4], string.Empty };
            yield return new object?[] { new string?[16], string.Empty };
            yield return new object?[] { new string?[64], string.Empty };
            //yield return new object?[] { new string?[256], string.Empty };
            yield return new object?[] { new string?[1024], string.Empty };
            yield return new object?[] { new string?[4096], string.Empty };
        }

        public IEnumerable<object?[]> ListData()
        {
            //yield return new object?[] { new List<string?>(new string?[1]), null };
            yield return new object?[] { new List<string?>(new string?[4]), string.Empty };
            yield return new object?[] { new List<string?>(new string?[16]), string.Empty };
            yield return new object?[] { new List<string?>(new string?[64]), string.Empty };
            //yield return new object?[] { new List<string?>(new string?[256]), string.Empty };
            yield return new object?[] { new List<string?>(new string?[1024]), string.Empty };
            yield return new object?[] { new List<string?>(new string?[4096]), string.Empty };
        }

        [ArgumentsSource(nameof(ArrayData))]
        [Benchmark] public int Span(string?[] values, string? find) => Finder.Span<string?>(values, x => x == find);

        [ArgumentsSource(nameof(ArrayData))]
        [Benchmark] public int SpanUnsafeWhile(string?[] values, string? find) => Finder.SpanUnsafeWhile<string?>(values, x => x == find);

        [ArgumentsSource(nameof(ArrayData))]
        [Benchmark] public int SpanUnsafeFor(string?[] values, string? find) => Finder.SpanUnsafeFor<string?>(values, x => x == find);

        // Array

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int Array(string?[] values, string? find) => Finder.Array(values, x => x == find);

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int ArrayRangeMax(string?[] values, string? find) => Finder.ArrayRangeMax(values, 0, values.Length, x => x == find);

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int ArrayRangeLengthDec(string?[] values, string? find) => Finder.ArrayRangeLengthDec(values, 0, values.Length, x => x == find);

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int ArrayRangeWhile(string?[] values, string? find) => Finder.ArrayRangeWhile(values, 0, values.Length, x => x == find);

        // List

        //[ArgumentsSource(nameof(ListData))]
        //[Benchmark] public int List(List<string?> values, string? find) => Finder.List(values, x => x == find);

        //[ArgumentsSource(nameof(ListData))]
        //[Benchmark] public int List2(List<string?> values, string? find) => Finder.List2(values, x => x == find);

        //[ArgumentsSource(nameof(ListData))]
        //[Benchmark] public int ListRange(List<string?> values, string? find) => Finder.ListRangeMax(values, 0, values.Count, x => x == find);

        //[ArgumentsSource(nameof(ListData))]
        //[Benchmark] public int ListRange2(List<string?> values, string? find) => Finder.ListRangeMax2(values, 0, values.Count, x => x == find);

        // Interface

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int InterfaceList(string?[] values, string? find) => Finder.InterfaceList(values, x => x == find);

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int InterfaceListRangeMax(string?[] values, string? find) => Finder.InterfaceListRangeMax(values, 0, values.Length, x => x == find);

        //[ArgumentsSource(nameof(ArrayData))]
        //[Benchmark] public int Enumerable(string?[] values, string? find) => Finder.Enumerable(values, x => x == find);
    }

    public static class Finder
    {
        public static int Span<T>(ReadOnlySpan<T?> source, Predicate<T?> predicate)
        {
            for (var index = 0; index < source.Length; index++)
            {
                if (predicate(source[index]))
                {
                    return index;
                }
            }

            return -1;
        }

        public static unsafe int SpanPointer<T>(ReadOnlySpan<T> source, Predicate<T> predicate)
            where T : unmanaged
        {
            fixed (T* ptr = source)
            {
                for (var index = 0; index < source.Length; index++)
                {
                    if (predicate(*(ptr + index)))
                    {
                        return index;
                    }
                }
            }

            return -1;
        }

        public static unsafe int SpanPointer2<T>(ReadOnlySpan<T> source, Predicate<T> predicate)
            where T : unmanaged
        {
            fixed (T* ptr = source)
            {
                var p = ptr;
                for (var index = 0; index < source.Length; index++)
                {
                    if (predicate(*p))
                    {
                        return index;
                    }

                    p++;
                }
            }

            return -1;
        }

        public static int SpanUnsafeWhile<T>(ReadOnlySpan<T?> source, Predicate<T?> predicate)
        {
            ref var start = ref MemoryMarshal.GetReference(source);
            ref var end = ref Unsafe.Add(ref start, source.Length);

            var index = 0;
            while (Unsafe.IsAddressLessThan(ref start, ref end))
            {
                if (predicate(start))
                {
                    return index;
                }

                start = ref Unsafe.Add(ref start, 1);
                index++;
            }

            return -1;
        }

        public static int SpanUnsafeFor<T>(ReadOnlySpan<T?> source, Predicate<T?> predicate)
        {
            ref var reference = ref MemoryMarshal.GetReference(source);

            for (var index = 0; index < source.Length; index++)
            {
                if (predicate(Unsafe.Add(ref reference, index)))
                {
                    return index;
                }
            }

            return -1;
        }

        public static int Array<T>(T?[] source, Predicate<T?> predicate)
        {
            for (var index = 0; index < source.Length; index++)
            {
                if (predicate(source[index]))
                {
                    return index;
                }
            }

            return -1;
        }

        public static unsafe int ArrayPointer<T>(T[] source, Predicate<T> predicate)
            where T : unmanaged
        {
            fixed (T* ptr = &source[0])
            {
                for (var index = 0; index < source.Length; index++)
                {
                    if (predicate(*(ptr + index)))
                    {
                        return index;
                    }
                }
            }

            return -1;
        }

        public static unsafe int ArrayPointer2<T>(T[] source, Predicate<T> predicate)
            where T : unmanaged
        {
            fixed (T* ptr = &source[0])
            {
                var p = ptr;
                for (var index = 0; index < source.Length; index++)
                {
                    if (predicate(*p))
                    {
                        return index;
                    }

                    p++;
                }
            }

            return -1;
        }

        public static int ArrayRangeMax<T>(T?[] source, int start, int length, Predicate<T?> predicate)
        {
            var max = Math.Min(source.Length - start, length);
            for (var index = 0; index < max; index++)
            {
                if (predicate(source[start]))
                {
                    return index;
                }

                start++;
            }

            return -1;
        }

        public static int ArrayRangeLengthDec<T>(T?[] source, int start, int length, Predicate<T?> predicate)
        {
            for (var index = start; index < source.Length && length > 0; index++)
            {
                if (predicate(source[index]))
                {
                    return index - start;
                }

                length--;
            }

            return -1;
        }

        public static int ArrayRangeWhile<T>(T?[] source, int start, int length, Predicate<T?> predicate)
        {
            var end = Math.Min(source.Length, start + length);
            var index = 0;
            while (start < end)
            {
                if (predicate(source[start]))
                {
                    return index;
                }

                start++;
                index++;
            }

            return -1;
        }

        public static unsafe int ArrayRangePointer<T>(T[] source, int start, int length, Predicate<T> predicate)
            where T : unmanaged
        {
            fixed (T* ptr = &source[start])
            {
                for (var index = 0; index < length; index++)
                {
                    if (predicate(*(ptr + index)))
                    {
                        return index;
                    }
                }
            }

            return -1;
        }

        public static unsafe int ArrayRangePointer2<T>(T[] source, int start, int length, Predicate<T> predicate)
            where T : unmanaged
        {
            fixed (T* ptr = &source[start])
            {
                var p = ptr;
                for (var index = 0; index < length; index++)
                {
                    if (predicate(*p))
                    {
                        return index;
                    }

                    p++;
                }
            }

            return -1;
        }

        public static int List<T>(List<T?> source, Predicate<T?> predicate)
        {
            for (var index = 0; index < source.Count; index++)
            {
                if (predicate(source[index]))
                {
                    return index;
                }
            }

            return -1;
        }

        public static int List2<T>(List<T?> source, Predicate<T?> predicate)
        {
            var max = source.Count;
            for (var index = 0; index < max; index++)
            {
                if (predicate(source[index]))
                {
                    return index;
                }
            }

            return -1;
        }

        public static int ListRangeMax<T>(List<T?> source, int start, int length, Predicate<T?> predicate)
        {
            var max = Math.Min(source.Count - start, length);
            for (var index = 0; index < max; index++)
            {
                if (predicate(source[start]))
                {
                    return index;
                }

                start++;
            }

            return -1;
        }

        public static int ListRangeMax2<T>(List<T?> source, int start, int length, Predicate<T?> predicate)
        {
            var max = source.Count > length ? length : source.Count;
            for (var index = start; index < max; index++)
            {
                if (predicate(source[index]))
                {
                    return index;
                }
            }

            return -1;
        }

        public static int InterfaceList<T>(IReadOnlyList<T?> source, Predicate<T?> predicate)
        {
            for (var index = 0; index < source.Count; index++)
            {
                if (predicate(source[index]))
                {
                    return index;
                }
            }

            return -1;
        }

        public static int InterfaceListRangeMax<T>(IReadOnlyList<T?> source, int start, int length, Predicate<T?> predicate)
        {
            var max = Math.Min(source.Count - start, length);
            for (var index = 0; index < max; index++)
            {
                if (predicate(source[start + index]))
                {
                    return index;
                }
            }

            return -1;
        }

        public static int Enumerable<T>(IEnumerable<T?> source, Predicate<T?> predicate)
        {
            var index = 0;
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }
    }
}
