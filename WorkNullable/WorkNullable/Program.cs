using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace WorkNullable
{
    class Program
    {
        static void Main(string[] args)
        {
            //var d1 = new Data();
            //var d2 = new Data();
            //var a = Logic.Select(d1, d2, x => x.Value, Comparer<int>.Default);
            //var b = Logic.Select2(d1, d2, x => x.Value, Comparer<int>.Default);

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
            AddJob(Job.LongRun);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class Benchmark
    {
        // Pattern 1

        //private readonly Data data1 = new Data();
        //private readonly Data data2 = new Data();

        //[Benchmark]
        //public int Select()
        //{
        //    var ret = 0;
        //    for (var i = 0; i < 1000; i++)
        //    {
        //        ret = Logic.Select(data1, data2, x => x.Value, Comparer<int>.Default);
        //    }

        //    return ret;
        //}

        //[Benchmark]
        //public int Select2()
        //{
        //    var ret = 0;
        //    for (var i = 0; i < 1000; i++)
        //    {
        //        ret = Logic.Select2(data1, data2, x => x.Value, Comparer<int>.Default);
        //    }

        //    return ret;
        //}
    }

    // TODO 3 AtomicReference

    // TODO 4 Add?

    // TODO Nullable 1

    public class Data
    {
        public int Value { get; set; }
    }

    public static class Logic
    {
        // Pattern 1

//        public static int Select<T, TK>(T a, T b, Func<T, TK> selector, IComparer<TK> comparer)
//            => comparer.Compare(selector(a), selector(b));

//#nullable enable
//        public static int Select2<T, TK>(T? a, T? b, Func<T?, TK?> selector, IComparer<TK> comparer)
//            => comparer.Compare(selector(a), selector(b));
//#nullable disable
    }
}
