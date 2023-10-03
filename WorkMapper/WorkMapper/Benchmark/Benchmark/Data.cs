using System;
using System.Collections.Generic;
using System.Text;

namespace Benchmark
{
    // Simple
    public class SimpleSource
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public int Value3 { get; set; }
        public int Value4 { get; set; }
        public string Value5 { get; set; }
        public string Value6 { get; set; }
        public string Value7 { get; set; }
        public string Value8 { get; set; }
    }

    public class SimpleDestination
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public int Value3 { get; set; }
        public int Value4 { get; set; }
        public string Value5 { get; set; }
        public string Value6 { get; set; }
        public string Value7 { get; set; }
        public string Value8 { get; set; }
    }

    // Mixed
    public class MixedSource
    {
        public string StringValue { get; set; }
        public int IntValue { get; set; }
        public long LongValue { get; set; }
        public int? NullableIntValue { get; set; }
        public float FloatValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public bool BoolValue { get; set; }
        public MyEnum EnumValue { get; set; }
    }

    public class MixedDestination
    {
        public string StringValue { get; set; }
        public int IntValue { get; set; }
        public long LongValue { get; set; }
        public int? NullableIntValue { get; set; }
        public float FloatValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public bool BoolValue { get; set; }
        public MyEnum EnumValue { get; set; }
    }

    // Factory
    public static class RawMapperFactory
    {
        public static IActionMapper<SimpleSource, SimpleDestination> CreateSimpleMapper()
        {
            return new ActionMapper<SimpleSource, SimpleDestination>(
                () => new SimpleDestination(),
                new Action<SimpleSource, SimpleDestination>[]
                {
                    (s, d) => d.Value1 = s.Value1,
                    (s, d) => d.Value2 = s.Value2,
                    (s, d) => d.Value3 = s.Value3,
                    (s, d) => d.Value4 = s.Value4,
                    (s, d) => d.Value5 = s.Value5,
                    (s, d) => d.Value6 = s.Value6,
                    (s, d) => d.Value7 = s.Value7,
                    (s, d) => d.Value8 = s.Value8
                });
        }

        public static IActionMapper<MixedSource, MixedDestination> CreateMixedMapper()
        {
            return new ActionMapper<MixedSource, MixedDestination>(
                () => new MixedDestination(),
                new Action<MixedSource, MixedDestination>[]
                {
                    (s, d) => d.StringValue = s.StringValue,
                    (s, d) => d.IntValue = s.IntValue,
                    (s, d) => d.LongValue = s.LongValue,
                    (s, d) => d.NullableIntValue = s.NullableIntValue,
                    (s, d) => d.FloatValue = s.FloatValue,
                    (s, d) => d.DateTimeValue = s.DateTimeValue,
                    (s, d) => d.BoolValue = s.BoolValue,
                    (s, d) => d.EnumValue = s.EnumValue
                });
        }
    }

    // Other
    public enum MyEnum
    {
        Zero,
        One
    }
}
