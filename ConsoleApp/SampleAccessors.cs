namespace ConsoleApp
{
    using System;
    using System.Reflection;

    using Smart.Reflection;

    public sealed class IntValueAccessor : IAccessor
    {
        public PropertyInfo Source { get; }

        public string Name => Source.Name;

        public Type Type => Source.PropertyType;

        public bool CanRead { get; } = false;

        public bool CanWrite { get; } = false;

        public IntValueAccessor(PropertyInfo pi)
        {
            Source = pi;
        }

        public object GetValue(object target)
        {
            return ((DataClass)target).IntValue;
        }

        public void SetValue(object target, object value)
        {
            ((DataClass)target).IntValue = value == null ? default : (int)value;
        }
    }
}
