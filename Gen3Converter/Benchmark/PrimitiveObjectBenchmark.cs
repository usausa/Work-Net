namespace Benchmark
{
    using System;

    using BenchmarkDotNet.Attributes;

    [Config(typeof(BenchmarkConfig))]
    public class PrimitiveObjectBenchmark
    {
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
        public int PrimitiveConverter() => primitiveConverter(1);

        [Benchmark]
        public object PrimitiveCallObjectConverter() => primitiveCallObjectConverter(1);

        [Benchmark]
        public object ObjectCastConverter() => objectCastConverter(1);

        [Benchmark]
        public object PrimitiveConverterDelegate() => primitiveConverterDelegate.DynamicInvoke(1);

        [Benchmark]
        public int PrimitiveCallObjectConverter2() => (int)primitiveCallObjectConverter(1);

        [Benchmark]
        public int ObjectCastConverter2() => (int)objectCastConverter(1);

        [Benchmark]
        public int PrimitiveConverterDelegate2() => (int)primitiveConverterDelegate.DynamicInvoke(1);
    }
}
