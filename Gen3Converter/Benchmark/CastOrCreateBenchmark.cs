namespace Benchmark
{
    using System;

    using BenchmarkDotNet.Attributes;

    [Config(typeof(BenchmarkConfig))]
    public class CastOrCreateBenchmark
    {

        private Func<int, string> valueToObject1;
        private Func<object, object> valueToObject2;

        private Func<string, int> objectToValue1;
        private Func<object, object> objectToValue2;

        private Func<int, int> valueToValue1;
        private Func<object, object> valueToValue2;

        private Func<string, string> objectToObject1;
        private Func<object, object> objectToObject2;

        [GlobalSetup]
        public void Setup()
        {
            valueToObject1 = IntToString;
            valueToObject2 = s => IntToString((int)s);

            objectToValue1 = StringToInt;
            objectToValue2 = s => StringToInt((string)s);

            valueToValue1 = IntToInt;
            valueToValue2 = s => IntToInt((int)s);

            objectToObject1 = StringToString;
            objectToObject2 = s => StringToString((string)s);
        }

        private static string IntToString(int x) => string.Empty;

        private static int StringToInt(string x) => 0;

        private static int IntToInt(int x) => x;

        private static string StringToString(string x) => x;

        [Benchmark]
        public string ValueToObject1() => valueToObject1(1);

        [Benchmark]
        public object ValueToObject2() => valueToObject2(1);

        [Benchmark]
        public int ObjectToValue1() => objectToValue1(string.Empty);

        [Benchmark]
        public object ObjectToValue2() => objectToValue2(string.Empty);

        [Benchmark]
        public int ValueToValue1() => valueToValue1(1);

        [Benchmark]
        public object ValueToValue2() => valueToValue2(1);

        [Benchmark]
        public string ObjectToObject1() => objectToObject1(string.Empty);

        [Benchmark]
        public object ObjectToObject2() => objectToObject2(string.Empty);
    }
}
