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
}
