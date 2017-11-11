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

        public bool CanRead { get; } = true;

        public bool CanWrite { get; } = true;

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

    public sealed class StringValueAccessor : IAccessor
    {
        public PropertyInfo Source { get; }

        public string Name => Source.Name;

        public Type Type => Source.PropertyType;

        public bool CanRead { get; } = true;

        public bool CanWrite { get; } = true;

        public StringValueAccessor(PropertyInfo pi)
        {
            Source = pi;
        }

        public object GetValue(object target)
        {
            return ((DataClass)target).StringValue;
        }

        public void SetValue(object target, object value)
        {
            ((DataClass)target).StringValue = (string)value;
        }
    }

    public sealed class IntNotificationValueAccessor : IAccessor
    {
        public PropertyInfo Source { get; }

        public string Name => Source.Name;

        public Type Type => Source.PropertyType;

        public bool CanRead { get; } = true;

        public bool CanWrite { get; } = true;

        public IntNotificationValueAccessor(PropertyInfo pi)
        {
            Source = pi;
        }

        public object GetValue(object target)
        {
            return ((DataClass)target).IntNotificationValue.Value;
        }

        public void SetValue(object target, object value)
        {
            ((DataClass)target).IntNotificationValue.Value = value == null ? default : (int)value;
        }
    }

    public sealed class StringNotificationValueAccessor : IAccessor
    {
        public PropertyInfo Source { get; }

        public string Name => Source.Name;

        public Type Type => Source.PropertyType;

        public bool CanRead { get; } = true;

        public bool CanWrite { get; } = true;

        public StringNotificationValueAccessor(PropertyInfo pi)
        {
            Source = pi;
        }

        public object GetValue(object target)
        {
            return ((DataClass)target).StringNotificationValue.Value;
        }

        public void SetValue(object target, object value)
        {
            ((DataClass)target).StringNotificationValue.Value = (string)value;
        }
    }

    public sealed class UnsuportedAccessor : IAccessor
    {
        public PropertyInfo Source { get; }

        public string Name => Source.Name;

        public Type Type => Source.PropertyType;

        public bool CanRead { get; } = false;

        public bool CanWrite { get; } = false;

        public UnsuportedAccessor(PropertyInfo pi)
        {
            Source = pi;
        }

        public object GetValue(object target)
        {
            throw new NotSupportedException();
        }

        public void SetValue(object target, object value)
        {
            throw new NotSupportedException();
        }
    }
}
