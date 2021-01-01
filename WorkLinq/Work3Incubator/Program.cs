using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Work3Incubator
{
    public static class Program
    {
        public static void Main(string[] args)
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

    public class Benchmark
    {
        private readonly int[] intArray = Enumerable.Range(0, 64).ToArray();
        private readonly string[] stringArray = Enumerable.Range(0, 64).Select(x => x.ToString()).ToArray();

        private readonly List<int> intList = Enumerable.Range(0, 64).ToList();
        private readonly IList<int> intInterfaceList = Enumerable.Range(0, 64).ToList();
        private readonly IReadOnlyList<int> intReadOnlyInterfaceList = Enumerable.Range(0, 64).ToList();

        [Benchmark]
        public int CountIntArray() => intArray.Count(x => x % 2 == 0);
        [Benchmark]
        public int CountIntArray2() => intArray.CountOptimized(x => x % 2 == 0);

        [Benchmark]
        public int CountIntSpan2() => intArray.AsSpan().CountOptimized(x => x % 2 == 0);

        [Benchmark]
        public int CountIntList() => intList.Count(x => x % 2 == 0);
        [Benchmark]
        public int CountIntList2() => intList.CountOptimized(x => x % 2 == 0);

        [Benchmark]
        public int CountIntInterfaceList() => intInterfaceList.Count(x => x % 2 == 0);
        [Benchmark]
        public int CountIntInterfaceList2() => intInterfaceList.CountOptimized(x => x % 2 == 0);
        [Benchmark]
        public int CountIntInterfaceList3() => intReadOnlyInterfaceList.CountOptimized(x => x % 2 == 0);
    }

    public static class Extensions
    {
        //--------------------------------------------------------------------------------
        // Count
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountOptimized<T>(this Span<T?> source, Func<T?, bool> predicate)
        {
            var count = 0;
            for (var i = 0; i < source.Length; i++)
            {
                if (predicate(source[i]))
                {
                    count++;
                }
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountOptimized<T>(this T?[] source, Func<T?, bool> predicate)
        {
            var count = 0;
            for (var i = 0; i < source.Length; i++)
            {
                if (predicate(source[i]))
                {
                    count++;
                }
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountOptimized<T>(this T?[] source, int start, int length, Func<T?, bool> predicate)
        {
            var count = 0;
            var last = start + length;
            var max = last > source.Length ? source.Length : last;
            for (var i = start; i < max; i++)
            {
                if (predicate(source[i]))
                {
                    count++;
                }
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountOptimized<T>(this List<T?> source, Func<T?, bool> predicate)
        {
            var count = 0;
            for (var i = 0; i < source.Count; i++)
            {
                if (predicate(source[i]))
                {
                    count++;
                }
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountOptimized<T>(this List<T?> source, int start, int length, Func<T?, bool> predicate)
        {
            var count = 0;
            var last = start + length;
            var max = last > source.Count ? source.Count : last;
            for (var i = start; i < max; i++)
            {
                if (predicate(source[i]))
                {
                    count++;
                }
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountOptimized<T>(this IList<T?> source, Func<T?, bool> predicate)
        {
            var count = 0;
            for (var i = 0; i < source.Count; i++)
            {
                if (predicate(source[i]))
                {
                    count++;
                }
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountOptimized<T>(this IList<T?> source, int start, int length, Func<T?, bool> predicate)
        {
            var count = 0;
            var last = start + length;
            var max = last > source.Count ? source.Count : last;
            for (var i = start; i < max; i++)
            {
                if (predicate(source[i]))
                {
                    count++;
                }
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountOptimized<T>(this IReadOnlyList<T?> source, Func<T?, bool> predicate)
        {
            var count = 0;
            for (var i = 0; i < source.Count; i++)
            {
                if (predicate(source[i]))
                {
                    count++;
                }
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountOptimized<T>(this IReadOnlyList<T?> source, int start, int length, Func<T?, bool> predicate)
        {
            var count = 0;
            var last = start + length;
            var max = last > source.Count ? source.Count : last;
            for (var i = start; i < max; i++)
            {
                if (predicate(source[i]))
                {
                    count++;
                }
            }

            return count;
        }

        //--------------------------------------------------------------------------------
        // Any
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyOptimized<T>(this Span<T?> source, Func<T?, bool> predicate)
        {
            for (var i = 0; i < source.Length; i++)
            {
                if (predicate(source[i]))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyOptimized<T>(this T?[] source, Func<T?, bool> predicate)
        {
            for (var i = 0; i < source.Length; i++)
            {
                if (predicate(source[i]))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyOptimized<T>(this T?[] source, int start, int length, Func<T?, bool> predicate)
        {
            var last = start + length;
            var max = last > source.Length ? source.Length : last;
            for (var i = start; i < max; i++)
            {
                if (predicate(source[i]))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyOptimized<T>(this List<T?> source, Func<T?, bool> predicate)
        {
            for (var i = 0; i < source.Count; i++)
            {
                if (predicate(source[i]))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyOptimized<T>(this List<T?> source, int start, int length, Func<T?, bool> predicate)
        {
            var last = start + length;
            var max = last > source.Count ? source.Count : last;
            for (var i = start; i < max; i++)
            {
                if (predicate(source[i]))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyOptimized<T>(this IList<T?> source, Func<T?, bool> predicate)
        {
            for (var i = 0; i < source.Count; i++)
            {
                if (predicate(source[i]))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyOptimized<T>(this IList<T?> source, int start, int length, Func<T?, bool> predicate)
        {
            var last = start + length;
            var max = last > source.Count ? source.Count : last;
            for (var i = start; i < max; i++)
            {
                if (predicate(source[i]))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyOptimized<T>(this IReadOnlyList<T?> source, Func<T?, bool> predicate)
        {
            for (var i = 0; i < source.Count; i++)
            {
                if (predicate(source[i]))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyOptimized<T>(this IReadOnlyList<T?> source, int start, int length, Func<T?, bool> predicate)
        {
            var last = start + length;
            var max = last > source.Count ? source.Count : last;
            for (var i = start; i < max; i++)
            {
                if (predicate(source[i]))
                {
                    return true;
                }
            }

            return false;
        }

        //--------------------------------------------------------------------------------
        // All
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllOptimized<T>(this Span<T?> source, Func<T?, bool> predicate)
        {
            for (var i = 0; i < source.Length; i++)
            {
                if (!predicate(source[i]))
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllOptimized<T>(this T?[] source, Func<T?, bool> predicate)
        {
            for (var i = 0; i < source.Length; i++)
            {
                if (!predicate(source[i]))
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllOptimized<T>(this T?[] source, int start, int length, Func<T?, bool> predicate)
        {
            var last = start + length;
            var max = last > source.Length ? source.Length : last;
            for (var i = start; i < max; i++)
            {
                if (!predicate(source[i]))
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllOptimized<T>(this List<T?> source, Func<T?, bool> predicate)
        {
            for (var i = 0; i < source.Count; i++)
            {
                if (!predicate(source[i]))
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllOptimized<T>(this List<T?> source, int start, int length, Func<T?, bool> predicate)
        {
            var last = start + length;
            var max = last > source.Count ? source.Count : last;
            for (var i = start; i < max; i++)
            {
                if (!predicate(source[i]))
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllOptimized<T>(this IList<T?> source, Func<T?, bool> predicate)
        {
            for (var i = 0; i < source.Count; i++)
            {
                if (!predicate(source[i]))
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllOptimized<T>(this IList<T?> source, int start, int length, Func<T?, bool> predicate)
        {
            var last = start + length;
            var max = last > source.Count ? source.Count : last;
            for (var i = start; i < max; i++)
            {
                if (!predicate(source[i]))
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllOptimized<T>(this IReadOnlyList<T?> source, Func<T?, bool> predicate)
        {
            for (var i = 0; i < source.Count; i++)
            {
                if (!predicate(source[i]))
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllOptimized<T>(this IReadOnlyList<T?> source, int start, int length, Func<T?, bool> predicate)
        {
            var last = start + length;
            var max = last > source.Count ? source.Count : last;
            for (var i = start; i < max; i++)
            {
                if (!predicate(source[i]))
                {
                    return false;
                }
            }

            return true;
        }

        //--------------------------------------------------------------------------------
        // IndexOf
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this Span<T?> source, Func<T?, bool> predicate)
        {
            for (var i = 0; i < source.Length; i++)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this T?[] source, Func<T?, bool> predicate)
        {
            for (var i = 0; i < source.Length; i++)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this T?[] source, int start, int length, Func<T?, bool> predicate)
        {
            var last = start + length;
            var max = last > source.Length ? source.Length : last;
            for (var i = start; i < max; i++)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this List<T?> source, Func<T?, bool> predicate)
        {
            for (var i = 0; i < source.Count; i++)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this List<T?> source, int start, int length, Func<T?, bool> predicate)
        {
            var last = start + length;
            var max = last > source.Count ? source.Count : last;
            for (var i = start; i < max; i++)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this IList<T?> source, Func<T?, bool> predicate)
        {
            for (var i = 0; i < source.Count; i++)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this IList<T?> source, int start, int length, Func<T?, bool> predicate)
        {
            var last = start + length;
            var max = last > source.Count ? source.Count : last;
            for (var i = start; i < max; i++)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this IReadOnlyList<T?> source, Func<T?, bool> predicate)
        {
            for (var i = 0; i < source.Count; i++)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this IReadOnlyList<T?> source, int start, int length, Func<T?, bool> predicate)
        {
            var last = start + length;
            var max = last > source.Count ? source.Count : last;
            for (var i = start; i < max; i++)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        //--------------------------------------------------------------------------------
        // LastIndexOf
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(this Span<T?> source, Func<T?, bool> predicate)
        {
            for (var i = source.Length - 1; i >= 0; i--)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(this T?[] source, Func<T?, bool> predicate)
        {
            for (var i = source.Length - 1; i >= 0; i--)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(this T?[] source, int start, int length, Func<T?, bool> predicate)
        {
            var last = start + length;
            var max = last > source.Length ? source.Length : last;
            if (start < 0)
            {
                start = 0;
            }

            for (var i = max - 1; i >= start; i--)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(this List<T?> source, Func<T?, bool> predicate)
        {
            for (var i = source.Count - 1; i >= 0; i--)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(this List<T?> source, int start, int length, Func<T?, bool> predicate)
        {
            var last = start + length;
            var max = last > source.Count ? source.Count : last;
            if (start < 0)
            {
                start = 0;
            }

            for (var i = max - 1; i >= start; i--)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(this IList<T?> source, Func<T?, bool> predicate)
        {
            for (var i = source.Count - 1; i >= 0; i--)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(this IList<T?> source, int start, int length, Func<T?, bool> predicate)
        {
            var last = start + length;
            var max = last > source.Count ? source.Count : last;
            if (start < 0)
            {
                start = 0;
            }

            for (var i = max - 1; i >= start; i--)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(this IReadOnlyList<T?> source, Func<T?, bool> predicate)
        {
            for (var i = source.Count - 1; i >= 0; i--)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(this IReadOnlyList<T?> source, int start, int length, Func<T?, bool> predicate)
        {
            var last = start + length;
            var max = last > source.Count ? source.Count : last;
            if (start < 0)
            {
                start = 0;
            }

            for (var i = max - 1; i >= start; i--)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
