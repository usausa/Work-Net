namespace NullableStructBenchmark
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

        private readonly MapperA mapperA = new();
        private readonly MapperB mapperB = new();

        [Benchmark(OperationsPerInvoke = N)]
        public StructDestinationInner1? Map1A()
        {
            var m = mapperA;
            var s = new StructSourceInner1();
            var ret = default(StructDestinationInner1?);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map1(s);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public StructDestinationInner1? Map1B()
        {
            var m = mapperB;
            var s = new StructSourceInner1();
            var ret = default(StructDestinationInner1?);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map1(s);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public StructDestinationInner2? Map2A()
        {
            var m = mapperA;
            var s = new StructSourceInner2();
            var ret = default(StructDestinationInner2?);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map2(s);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public StructDestinationInner2? Map2B()
        {
            var m = mapperB;
            var s = new StructSourceInner2();
            var ret = default(StructDestinationInner2?);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map2(s);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public StructDestinationInner4? Map4A()
        {
            var m = mapperA;
            var s = new StructSourceInner4();
            var ret = default(StructDestinationInner4?);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map4(s);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public StructDestinationInner4? Map4B()
        {
            var m = mapperB;
            var s = new StructSourceInner4();
            var ret = default(StructDestinationInner4?);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map4(s);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public StructDestinationInner6? Map6A()
        {
            var m = mapperA;
            var s = new StructSourceInner6();
            var ret = default(StructDestinationInner6?);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map6(s);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public StructDestinationInner6? Map6B()
        {
            var m = mapperB;
            var s = new StructSourceInner6();
            var ret = default(StructDestinationInner6?);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map6(s);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public StructDestinationInner8? Map8A()
        {
            var m = mapperA;
            var s = new StructSourceInner8();
            var ret = default(StructDestinationInner8?);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map8(s);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public StructDestinationInner8? Map8B()
        {
            var m = mapperB;
            var s = new StructSourceInner8();
            var ret = default(StructDestinationInner8?);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map8(s);
            }
            return ret;
        }
    }

    public sealed class MapperA
    {
        public StructDestinationInner1? Map1(StructSourceInner1? source)
        {
            if (source is null)
            {
                return null;
            }

            var d = new StructDestinationInner1();
            d.Value1 = source.Value.Value1;
            return d;
        }

        public StructDestinationInner2? Map2(StructSourceInner2? source)
        {
            if (source is null)
            {
                return null;
            }

            var d = new StructDestinationInner2();
            d.Value1 = source.Value.Value1;
            d.Value2 = source.Value.Value2;
            return d;
        }

        public StructDestinationInner4? Map4(StructSourceInner4? source)
        {
            if (source is null)
            {
                return null;
            }

            var d = new StructDestinationInner4();
            d.Value1 = source.Value.Value1;
            d.Value2 = source.Value.Value2;
            d.Value3 = source.Value.Value3;
            d.Value4 = source.Value.Value4;
            return d;
        }

        public StructDestinationInner6? Map6(StructSourceInner6? source)
        {
            if (source is null)
            {
                return null;
            }

            var d = new StructDestinationInner6();
            d.Value1 = source.Value.Value1;
            d.Value2 = source.Value.Value2;
            d.Value3 = source.Value.Value3;
            d.Value4 = source.Value.Value4;
            d.Value5 = source.Value.Value5;
            d.Value6 = source.Value.Value6;
            return d;
        }

        public StructDestinationInner8? Map8(StructSourceInner8? source)
        {
            if (source is null)
            {
                return null;
            }

            var d = new StructDestinationInner8();
            d.Value1 = source.Value.Value1;
            d.Value2 = source.Value.Value2;
            d.Value3 = source.Value.Value3;
            d.Value4 = source.Value.Value4;
            d.Value5 = source.Value.Value5;
            d.Value8 = source.Value.Value8;
            return d;
        }
    }

    public sealed class MapperB
    {
        public StructDestinationInner1? Map1(StructSourceInner1? source)
        {
            if (source is null)
            {
                return null;
            }

            var s = source.Value;

            var d = new StructDestinationInner1();
            d.Value1 = s.Value1;
            return d;
        }

        public StructDestinationInner2? Map2(StructSourceInner2? source)
        {
            if (source is null)
            {
                return null;
            }

            var s = source.Value;

            var d = new StructDestinationInner2();
            d.Value1 = s.Value1;
            d.Value2 = s.Value2;
            return d;
        }

        public StructDestinationInner4? Map4(StructSourceInner4? source)
        {
            if (source is null)
            {
                return null;
            }

            var s = source.Value;

            var d = new StructDestinationInner4();
            d.Value1 = s.Value1;
            d.Value2 = s.Value2;
            d.Value3 = s.Value3;
            d.Value4 = s.Value4;
            return d;
        }

        public StructDestinationInner6? Map6(StructSourceInner6? source)
        {
            if (source is null)
            {
                return null;
            }

            var s = source.Value;

            var d = new StructDestinationInner6();
            d.Value1 = s.Value1;
            d.Value2 = s.Value2;
            d.Value3 = s.Value3;
            d.Value4 = s.Value4;
            d.Value5 = s.Value5;
            d.Value6 = s.Value6;
            return d;
        }

        public StructDestinationInner8? Map8(StructSourceInner8? source)
        {
            if (source is null)
            {
                return null;
            }

            var s = source.Value;

            var d = new StructDestinationInner8();
            d.Value1 = s.Value1;
            d.Value2 = s.Value2;
            d.Value3 = s.Value3;
            d.Value4 = s.Value4;
            d.Value5 = s.Value5;
            d.Value6 = s.Value6;
            d.Value7 = s.Value7;
            d.Value8 = s.Value8;
            return d;
        }
    }

    public struct StructSourceInner1
    {
        public long Value1 { get; set; }
    }

    public struct StructDestinationInner1
    {
        public long Value1 { get; set; }
    }

    public struct StructSourceInner2
    {
        public long Value1 { get; set; }
        public long Value2 { get; set; }
    }

    public struct StructDestinationInner2
    {
        public long Value1 { get; set; }
        public long Value2 { get; set; }
    }

    public struct StructSourceInner4
    {
        public long Value1 { get; set; }
        public long Value2 { get; set; }
        public long Value3 { get; set; }
        public long Value4 { get; set; }
    }

    public struct StructDestinationInner4
    {
        public long Value1 { get; set; }
        public long Value2 { get; set; }
        public long Value3 { get; set; }
        public long Value4 { get; set; }
    }

    public struct StructSourceInner6
    {
        public long Value1 { get; set; }
        public long Value2 { get; set; }
        public long Value3 { get; set; }
        public long Value4 { get; set; }
        public long Value5 { get; set; }
        public long Value6 { get; set; }
    }

    public struct StructDestinationInner6
    {
        public long Value1 { get; set; }
        public long Value2 { get; set; }
        public long Value3 { get; set; }
        public long Value4 { get; set; }
        public long Value5 { get; set; }
        public long Value6 { get; set; }
    }

    public struct StructSourceInner8
    {
        public long Value1 { get; set; }
        public long Value2 { get; set; }
        public long Value3 { get; set; }
        public long Value4 { get; set; }
        public long Value5 { get; set; }
        public long Value6 { get; set; }
        public long Value7 { get; set; }
        public long Value8 { get; set; }
    }

    public struct StructDestinationInner8
    {
        public long Value1 { get; set; }
        public long Value2 { get; set; }
        public long Value3 { get; set; }
        public long Value4 { get; set; }
        public long Value5 { get; set; }
        public long Value6 { get; set; }
        public long Value7 { get; set; }
        public long Value8 { get; set; }
    }
}
