namespace Benchmark
{
    using System;

    using BenchmarkDotNet.Attributes;

    [Config(typeof(BenchmarkConfig))]
    public class PrimitiveObjectBenchmark
    {
        private readonly object parameter = 1;

        private Func<int, int> primitiveConverter;
        private Func<object, object> primitiveCallObjectConverter;
        private Func<object, object> objectCastConverter;   // better than call, but...
        private Delegate primitiveConverterDelegate;    // Too slow

        [GlobalSetup]
        public void Setup()
        {
            primitiveConverter = x => x;
            primitiveCallObjectConverter = x => primitiveConverter((int)x);
            primitiveConverterDelegate = primitiveConverter;
            objectCastConverter = x => (int)x;
        }

        [Benchmark]
        public int PrimitiveConverter() => primitiveConverter((int)parameter);

        [Benchmark]
        public object PrimitiveCallObjectConverter() => primitiveCallObjectConverter(parameter);

        [Benchmark]
        public object ObjectCastConverter() => objectCastConverter(parameter);

        [Benchmark]
        public object PrimitiveConverterDelegate() => primitiveConverterDelegate.DynamicInvoke(parameter);

        [Benchmark]
        public int PrimitiveCallObjectConverter2() => (int)primitiveCallObjectConverter(parameter);

        [Benchmark]
        public int ObjectCastConverter2() => (int)objectCastConverter(parameter);

        [Benchmark]
        public int PrimitiveConverterDelegate2() => (int)primitiveConverterDelegate.DynamicInvoke(parameter);
    }
}
