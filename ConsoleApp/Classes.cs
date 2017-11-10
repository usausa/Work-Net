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

    public class DataClass
    {
        public int IntValue { get; set; }

        public string StringValue { get; set; }

        public IValueHolder<int> IntNotificationValue { get; } = new NotificationValue<int>();

        public IValueHolder<string> StringNotificationValue { get; } = new NotificationValue<string>();
    }
}
