namespace Work1Predicate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
            //AddJob(Job.LongRun);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class Benchmark
    {
        private readonly string[] values = Enumerable.Range(0, 100).Select(x => x.ToString()).ToArray();

        //[Benchmark]
        //public int FindByFunc() => Finder.ByFunc(values, x => x == string.Empty);

        //[Benchmark]
        //public int FindByDelegate() => Finder.ByDelegate(values, x => x == string.Empty);

        //[Benchmark]
        //public int FindByFunc() => Finder.ByFunc(values, -1, x => x!.Length);

        //[Benchmark]
        //public int FindByDelegate() => Finder.ByDelegate(values,  -1, x => x!.Length);

        [Benchmark]
        public int FindByFunc() => Finder.ByFunc(values, string.Empty, (x, y) => x!.Length - y!.Length);

        [Benchmark]
        public int FindByDelegate() => Finder.ByDelegate(values, string.Empty, (x, y) => x!.Length - y!.Length);

        //[Benchmark]
        //public List<int> SelectByFunc() => Selector.ByFunc(values, x => x?.Length ?? 0).ToList();

        //[Benchmark]
        //public List<int> SelectByDelegate() => Selector.ByDelegate(values, x => x?.Length ?? 0).ToList();
    }

    public delegate TResult? Selector<in T, out TResult>(T? obj);

    public delegate int Comparer<in T>(T? obj1, T? obj2);

    public static class Finder
    {
        public static int ByFunc<T>(T?[] array, Func<T?, bool> predicate)
        {
            for (var i = 0; i < array.Length; i++)
            {
                if (predicate(array[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public static int ByDelegate<T>(T?[] array, Predicate<T?> predicate)
        {
            for (var i = 0; i < array.Length; i++)
            {
                if (predicate(array[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public static int ByDelegate<T, TValue>(T?[] array, TValue? value, Selector<T?, TValue?> selector)
            where TValue : IEquatable<TValue>
        {
            for (var i = 0; i < array.Length; i++)
            {
                if (selector(array[i])!.Equals(value))
                {
                    return i;
                }
            }
            return -1;
        }

        public static int ByFunc<T, TValue>(T?[] array, TValue? value, Func<T?, TValue?> selector)
            where TValue : IEquatable<TValue>
        {
            for (var i = 0; i < array.Length; i++)
            {
                if (selector(array[i])!.Equals(value))
                {
                    return i;
                }
            }
            return -1;
        }

        public static int ByDelegate<T>(T?[] array, T? value, Comparer<T?> comparer)
        {
            for (var i = 0; i < array.Length; i++)
            {
                if (comparer(array[i], value) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public static int ByFunc<T>(T?[] array, T? value, Func<T?, T?, int> comparer)
        {
            for (var i = 0; i < array.Length; i++)
            {
                if (comparer(array[i], value) == 0)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    public static class Selector
    {
        public static IEnumerable<TResult?> ByFunc<T, TResult>(T?[] array, Func<T?, TResult?> selector)
        {
            for (var i = 0; i < array.Length; i++)
            {
                yield return selector(array[i]);
            }
        }

        public static IEnumerable<TResult?> ByDelegate<T, TResult>(T?[] array, Selector<T?, TResult?> selector)
        {
            for (var i = 0; i < array.Length; i++)
            {
                yield return selector(array[i]);
            }
        }
    }
}
