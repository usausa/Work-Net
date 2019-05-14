namespace Benchmark
{
    using System;

    using BenchmarkDotNet.Attributes;

    [Config(typeof(BenchmarkConfig))]
    public class BaseBenchmark
    {
        private Func<long, int> rawToRaw;
        private Func<long, object> rawToObject;
        private Func<object, int> objectToRaw;
        private Func<object, object> objectToObject;

        private Func<object, object> rawToRawWithCast;
        private Func<long, int> rawToObjectWithCast;
        private Func<object, int> objectToObjectWithCast;

        [GlobalSetup]
        public void Setup()
        {
            rawToRaw = s => (int)s;
            rawToObject = s => (int)s;
            objectToRaw = s => (int)(long)s;
            objectToObject = s => (int)(long)s;

            // id source is primitive ?, make cast code ?
            rawToRawWithCast = s => rawToRaw((long)s);
            rawToObjectWithCast = s => (int)rawToObject(s); // better than objectToObject
            objectToObjectWithCast = s => (int)objectToObject(s);
        }

        [Benchmark]
        public int RawToRaw() => rawToRaw(1L);

        [Benchmark]
        public int RawToObject() => (int)rawToObject(1L);

        [Benchmark]
        public object RawToObjectReturnObject() => rawToObject(1L);

        [Benchmark]
        public int ObjectToRaw() => objectToRaw(1L);

        [Benchmark]
        public int ObjectToObject() => (int)objectToObject(1L);

        [Benchmark]
        public object ObjectToObjectReturnObject() => objectToObject(1L);

        [Benchmark]
        public int RawToRawWithCast() => (int)rawToRawWithCast(1L);

        [Benchmark]
        public object RawToRawWithCastReturnObject() => rawToRawWithCast(1L);

        [Benchmark]
        public int RawToObjectWithCast() => rawToObjectWithCast(1L);

        [Benchmark]
        public int ObjectToObjectWithCast() => objectToObjectWithCast(1L);
    }
}
