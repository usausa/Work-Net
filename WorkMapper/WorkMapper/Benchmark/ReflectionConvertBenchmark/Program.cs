namespace ReflectionConvertBenchmark
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

        private readonly Source source = new(1);
        private readonly Destination destination = new();

        [Benchmark(OperationsPerInvoke = N)]
        public void TypedConvert()
        {
            var s = source;
            var d = destination;
            for (var i = 0; i < N; i++)
            {
                d.Setter = Converter.TypedConvert((long)s.Getter);
            }
        }


        [Benchmark(OperationsPerInvoke = N)]
        public void ObjectConvert()
        {
            var s = source;
            var d = destination;
            for (var i = 0; i < N; i++)
            {
                d.Setter = Converter.ObjectConvert(s.Getter);
            }
        }
    }

    public static class Converter
    {
        public static int TypedConvert(long value) => (int)value;

        public static object ObjectConvert(object value) => (int)(long)value;
    }

    public class Source
    {
        private readonly long value;

        public object Getter => value;

        public Source(long value)
        {
            this.value = value;
        }
    }

    public class Destination
    {
        public int Value { get; set; }

        public object Setter
        {
            set => Value = (int)value;
        }
    }
}
