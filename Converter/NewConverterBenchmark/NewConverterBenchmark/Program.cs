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
        public int ValueToValueAsObject()
        {
            var ret = 0;
            for (var i = 0; i < 1000; i++)
            {
                ret = converter.ConvertValueToValueAsObject(0);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public string ValueToClassAsObject()
        {
            var ret = string.Empty;
            for (var i = 0; i < 1000; i++)
            {
                ret = converter.ConvertValueToClassAsObject(0);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int ClassToValueAsObject()
        {
            var ret = 0;
            for (var i = 0; i < 1000; i++)
            {
                ret = converter.ConvertClassToValueAsObject(string.Empty);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public string ClassToClassAsObject()
        {
            var ret = string.Empty;
            for (var i = 0; i < 1000; i++)
            {
                ret = converter.ConvertClassToClassAsObject(string.Empty);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int ValueToValueCast()
        {
            var ret = 0;
            for (var i = 0; i < 1000; i++)
            {
                ret = (int)converter.ConvertValueToValueCast(0);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public string ValueToClassCast()
        {
            var ret = string.Empty;
            for (var i = 0; i < 1000; i++)
            {
                ret = (string)converter.ConvertValueToClassCast(0);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int ClassToValueCast()
        {
            var ret = 0;
            for (var i = 0; i < 1000; i++)
            {
                ret = (int)converter.ConvertClassToValueCast(string.Empty);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public string ClassToClassCast()
        {
            var ret = string.Empty;
            for (var i = 0; i < 1000; i++)
            {
                ret = (string)converter.ConvertClassToClassCast(string.Empty);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int ValueToValueAsObjectCast()
        {
            var ret = 0;
            for (var i = 0; i < 1000; i++)
            {
                ret = (int)converter.ConvertValueToValueAsObjectCast(0);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public string ValueToClassAsObjectCast()
        {
            var ret = string.Empty;
            for (var i = 0; i < 1000; i++)
            {
                ret = (string)converter.ConvertValueToClassAsObjectCast(0);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public int ClassToValueAsObjectCast()
        {
            var ret = 0;
            for (var i = 0; i < 1000; i++)
            {
                ret = (int)converter.ConvertClassToValueAsObjectCast(string.Empty);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public string ClassToClassAsObjectCast()
        {
            var ret = string.Empty;
            for (var i = 0; i < 1000; i++)
            {
                ret = (string)converter.ConvertClassToClassAsObjectCast(string.Empty);
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

        private readonly Func<object, object> valueToValueCast;
        private readonly Func<object, object> valueToClassCast;
        private readonly Func<object, object> classToValueCast;
        private readonly Func<object, object> classToClassCast;

        private readonly object valueToValueRawAsObject;
        private readonly object valueToClassRawAsObject;
        private readonly object classToValueRawAsObject;
        private readonly object classToClassRawAsObject;

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
            valueToValueCast = x => ValueToValue((int)x);
            valueToClassCast = x => ValueToClass((int)x);
            classToValueCast = x => ClassToValue((string)x);
            classToClassCast = x => ClassToClass((string)x);
            valueToValueRawAsObject = valueToValueRaw;
            valueToClassRawAsObject = valueToClassRaw;
            classToValueRawAsObject = classToValueRaw;
            classToClassRawAsObject = classToClassRaw;
        }

        public int ConvertValueToValueRaw(int x) => valueToValueRaw(x);
        public string ConvertValueToClassRaw(int x) => valueToClassRaw(x);
        public int ConvertClassToValueRaw(string x) => classToValueRaw(x);
        public string ConvertClassToClassRaw(string x) => classToClassRaw(x);

        public int ConvertValueToValueAsObject(int x) => ((Func<int, int>)valueToValueRawAsObject)(x);
        public string ConvertValueToClassAsObject(int x) => ((Func<int, string>)valueToClassRawAsObject)(x);
        public int ConvertClassToValueAsObject(string x) => ((Func<string, int>)classToValueRawAsObject)(x);
        public string ConvertClassToClassAsObject(string x) => ((Func<string, string>)classToClassRawAsObject)(x);

        public object ConvertValueToValueCast(object x) => valueToValueCast(x);
        public object ConvertValueToClassCast(object x) => valueToClassCast(x);
        public object ConvertClassToValueCast(object x) => classToValueCast(x);
        public object ConvertClassToClassCast(object x) => classToClassCast(x);

        public object ConvertValueToValueAsObjectCast(object x) => ((Func<int, int>)valueToValueRawAsObject)((int)x);
        public object ConvertValueToClassAsObjectCast(object x) => ((Func<int, string>)valueToClassRawAsObject)((int)x);
        public object ConvertClassToValueAsObjectCast(object x) => ((Func<string, int>)classToValueRawAsObject)((string)x);
        public object ConvertClassToClassAsObjectCast(object x) => ((Func<string, string>)classToClassRawAsObject)((string)x);
    }
}
