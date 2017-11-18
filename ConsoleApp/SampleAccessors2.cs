namespace ConsoleApp
{
    using System;
    using System.Reflection;

    using Smart.Reflection;
    public sealed class IntNotificationValueAccessor : IAccessor
    {
        private readonly PropertyInfo source;   // IValueHolder<T>

        private readonly Type type; // T

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
            get { return type; }
        }

        public bool CanRead
        {
            get { return true; }
        }

        public bool CanWrite
        {
            get { return true; }
        }

        public IntNotificationValueAccessor(PropertyInfo pi, Type type)
        {
            source = pi;
            this.type = type;
        }

        public object GetValue(object target)
        {
            return ((DataClass)target).IntNotificationValue.Value;
        }

        public void SetValue(object target, object value)
        {
            if (value == null)
            {
                ((DataClass)target).IntNotificationValue.Value = default;
            }
            else
            {
                ((DataClass)target).IntNotificationValue.Value = (int)value;
            }
        }
    }

    public sealed class StringNotificationValueAccessor : IAccessor
    {
        private readonly PropertyInfo source;   // IValueHolder<T>

        private readonly Type type; // T

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
            get { return type; }
        }

        public bool CanRead
        {
            get { return true; }
        }

        public bool CanWrite
        {
            get { return true; }
        }

        public StringNotificationValueAccessor(PropertyInfo pi, Type type)
        {
            source = pi;
            this.type = type;
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
