using System;

namespace NewConverterBenchmark
{
    using BenchmarkDotNet.Attributes;
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
            AddDiagnoser(MemoryDiagnoser.Default);
            //AddJob(Job.LongRun);
            //AddJob(Job.MediumRun);
            AddJob(Job.ShortRun);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class Benchmark
    {
        private const int N = 1000;

        private readonly Converter converter = new Converter();

        [Benchmark(OperationsPerInvoke = N)]
        public int ValueToValueRaw()
        {
            var ret = 0;
            for (var i = 0; i < 1000; i++)
            {
                ret = converter.ConvertValueToValueRaw(0);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public string ValueToClassRaw()
        {
            var ret = string.Empty;
            for (var i = 0; i < 1000; i++)
            {
                ret = converter.ConvertValueToClassRaw(0);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int ClassToValueRaw()
        {
            var ret = 0;
            for (var i = 0; i < 1000; i++)
            {
                ret = converter.ConvertClassToValueRaw(string.Empty);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public string ClassToClassRaw()
        {
            var ret = string.Empty;
            for (var i = 0; i < 1000; i++)
            {
                ret = converter.ConvertClassToClassRaw(string.Empty);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int ValueToValue()
        {
            var ret = 0;
            for (var i = 0; i < 1000; i++)
            {
                ret = (int)converter.ConvertValueToValue(0);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public string ValueToClass()
        {
            var ret = string.Empty;
            for (var i = 0; i < 1000; i++)
            {
                ret = (string)converter.ConvertValueToClass(0);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int ClassToValue()
        {
            var ret = 0;
            for (var i = 0; i < 1000; i++)
            {
                ret = (int)converter.ConvertClassToValue(string.Empty);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public string ClassToClass()
        {
            var ret = string.Empty;
            for (var i = 0; i < 1000; i++)
            {
                ret = (string)converter.ConvertClassToClass(string.Empty);
            }

            return ret;
        }
    }

    public class Converter
    {
        private readonly Func<int, int> valueToValueRaw;
        private readonly Func<int, string> valueToClassRaw;
        private readonly Func<string, int> classToValueRaw;
        private readonly Func<string, string> classToClassRaw;

        private readonly Func<object, object> valueToValue;
        private readonly Func<object, object> valueToClass;
        private readonly Func<object, object> classToValue;
        private readonly Func<object, object> classToClass;

        private static int ValueToValue(int x) => 0;
        private static string ValueToClass(int x) => string.Empty;
        private static int ClassToValue(string x) => 0;
        private static string ClassToClass(string x) => string.Empty;

        public Converter()
        {
            // ReSharper disable ConvertClosureToMethodGroup
            valueToValueRaw = x => ValueToValue(x);
            valueToClassRaw = x => ValueToClass(x);
            classToValueRaw = x => ClassToValue(x);
            classToClassRaw = x => ClassToClass(x);
            // ReSharper restore ConvertClosureToMethodGroup
            valueToValue = x => ValueToValue((int)x);
            valueToClass = x => ValueToClass((int)x);
            classToValue = x => ClassToValue((string)x);
            classToClass = x => ClassToClass((string)x);
        }

        public int ConvertValueToValueRaw(int x) => valueToValueRaw(x);
        public string ConvertValueToClassRaw(int x) => valueToClassRaw(x);
        public int ConvertClassToValueRaw(string x) => classToValueRaw(x);
        public string ConvertClassToClassRaw(string x) => classToClassRaw(x);

        public object ConvertValueToValue(object x) => valueToValue(x);
        public object ConvertValueToClass(object x) => valueToClass(x);
        public object ConvertClassToValue(object x) => classToValue(x);
        public object ConvertClassToClass(object x) => classToClass(x);
    }
}
