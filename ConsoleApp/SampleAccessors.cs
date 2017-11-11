namespace ConsoleApp
{
    using System;
    using System.Reflection;

    using Smart.Reflection;

    public sealed class IntValueAccessor : IAccessor
    {
        private readonly PropertyInfo source;

        public PropertyInfo Source
        {
            get { return source; }
        }

        public string Name
        {
            get { return source.Name; }
        }

        public Type Type
        {
            get { return source.PropertyType; }
        }

        public bool CanRead
        {
            get { return true; }
        }

        public bool CanWrite
        {
            get { return true; }
        }

        public IntValueAccessor(PropertyInfo pi)
        {
            source = pi;
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
        private readonly PropertyInfo source;

        public PropertyInfo Source
        {
            get { return source; }
        }

        public string Name
        {
            get { return source.Name; }
        }

        public Type Type
        {
            get { return source.PropertyType; }
        }

        public bool CanRead
        {
            get { return true; }
        }

        public bool CanWrite
        {
            get { return true; }
        }

        public StringValueAccessor(PropertyInfo pi)
        {
            source = pi;
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
        private readonly PropertyInfo source;

        public PropertyInfo Source
        {
            get { return source; }
        }

        public string Name
        {
            get { return source.Name; }
        }

        public Type Type
        {
            get { return source.PropertyType; }
        }

        public bool CanRead
        {
            get { return true; }
        }

        public bool CanWrite
        {
            get { return true; }
        }

        public IntNotificationValueAccessor(PropertyInfo pi)
        {
            source = pi;
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
        private readonly PropertyInfo source;

        public PropertyInfo Source
        {
            get { return source; }
        }

        public string Name
        {
            get { return source.Name; }
        }

        public Type Type
        {
            get { return source.PropertyType; }
        }

        public bool CanRead
        {
            get { return true; }
        }

        public bool CanWrite
        {
            get { return true; }
        }

        public StringNotificationValueAccessor(PropertyInfo pi)
        {
            source = pi;
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
        private readonly PropertyInfo source;

        public PropertyInfo Source
        {
            get { return source; }
        }

        public string Name
        {
            get { return source.Name; }
        }

        public Type Type
        {
            get { return source.PropertyType; }
        }

        public bool CanRead
        {
            get { return false; }
        }

        public bool CanWrite
        {
            get { return false; }
        }

        public UnsuportedAccessor(PropertyInfo pi)
        {
            source = pi;
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
