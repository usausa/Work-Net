namespace ConsoleApp
{
    using Smart.ComponentModel;

    public class Class0
    {
    }

    public class Class1
    {
        public int Value { get; }

        public Class1(int value)
        {
            Value = value;
        }
    }

    public class Class2
    {
        public int Value1 { get; }

        public string Value2 { get; }

        public Class2(int value1, string value2)
        {
            Value1 = value1;
            Value2 = value2;
        }
    }

    public struct Size
    {
        public int X;

        public int Y;
    }

    public class DataClass
    {
        public int IntValue { get; set; }

        public byte ByteValue { get; set; }

        public char CharValue { get; set; }

        public short ShortValue { get; set; }

        public long LongValue { get; set; }

        public float FloatValue { get; set; }

        public double DoubleValue { get; set; }

        public Size StructValue { get; set; }

        public string StringValue { get; set; }

        public IValueHolder<int> IntNotificationValue { get; } = new NotificationValue<int>();

        public IValueHolder<string> StringNotificationValue { get; } = new NotificationValue<string>();
    }

    public enum MyEnum
    {
        Zero, One, Two
    }

    public class EnumPropertyData
    {
        public MyEnum EnumValue { get; set; }

        public IValueHolder<MyEnum> EnumNotificationValue { get; } = new NotificationValue<MyEnum>();
    }

    public struct MyStruct
    {
        public int X { get; set; }

        public int Y { get; set; }
    }

    public class StructPropertyData
    {
        public MyStruct StructValue { get; set; }

        public IValueHolder<MyStruct> StructNotificationValue { get; } = new NotificationValue<MyStruct>();
    }
}
