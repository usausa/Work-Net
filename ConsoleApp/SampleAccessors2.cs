namespace ConsoleApp
{
    using System;
    using System.Reflection;

    using Smart.Reflection;

    public sealed class SampleByteValueAccessor : IAccessor
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

        public SampleByteValueAccessor(PropertyInfo pi)
        {
            source = pi;
        }

        public object GetValue(object target)
        {
            return ((DataClass)target).ByteValue;
        }

        public void SetValue(object target, object value)
        {
            if (value == null)
            {
                ((DataClass)target).ByteValue = default;
            }
            else
            {
                ((DataClass)target).ByteValue = (byte)value;
            }
        }
    }

    public sealed class SampleCharValueAccessor : IAccessor
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

        public SampleCharValueAccessor(PropertyInfo pi)
        {
            source = pi;
        }

        public object GetValue(object target)
        {
            return ((DataClass)target).CharValue;
        }

        public void SetValue(object target, object value)
        {
            if (value == null)
            {
                ((DataClass)target).CharValue = default;
            }
            else
            {
                ((DataClass)target).CharValue = (char)value;
            }
        }
    }

    public sealed class SampleShortValueAccessor : IAccessor
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

        public SampleShortValueAccessor(PropertyInfo pi)
        {
            source = pi;
        }

        public object GetValue(object target)
        {
            return ((DataClass)target).ShortValue;
        }

        public void SetValue(object target, object value)
        {
            if (value == null)
            {
                ((DataClass)target).ShortValue = default;
            }
            else
            {
                ((DataClass)target).ShortValue = (short)value;
            }
        }
    }

    public sealed class SampleLongValueAccessor : IAccessor
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

        public SampleLongValueAccessor(PropertyInfo pi)
        {
            source = pi;
        }

        public object GetValue(object target)
        {
            return ((DataClass)target).LongValue;
        }

        public void SetValue(object target, object value)
        {
            if (value == null)
            {
                ((DataClass)target).LongValue = default;
            }
            else
            {
                ((DataClass)target).LongValue = (short)value;
            }
        }
    }

    public sealed class SampleFloatValueAccessor : IAccessor
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

        public SampleFloatValueAccessor(PropertyInfo pi)
        {
            source = pi;
        }

        public object GetValue(object target)
        {
            return ((DataClass)target).FloatValue;
        }

        public void SetValue(object target, object value)
        {
            if (value == null)
            {
                ((DataClass)target).FloatValue = default;
            }
            else
            {
                ((DataClass)target).FloatValue = (short)value;
            }
        }
    }

    public sealed class SampleDoubleValueAccessor : IAccessor
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

        public SampleDoubleValueAccessor(PropertyInfo pi)
        {
            source = pi;
        }

        public object GetValue(object target)
        {
            return ((DataClass)target).DoubleValue;
        }

        public void SetValue(object target, object value)
        {
            if (value == null)
            {
                ((DataClass)target).DoubleValue = default;
            }
            else
            {
                ((DataClass)target).DoubleValue = (short)value;
            }
        }
    }

    public sealed class SampleStructValueAccessor : IAccessor
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

        public SampleStructValueAccessor(PropertyInfo pi)
        {
            source = pi;
        }

        public object GetValue(object target)
        {
            return ((DataClass)target).StructValue;
        }

        public void SetValue(object target, object value)
        {
            if (value == null)
            {
                ((DataClass)target).StructValue = default;
            }
            else
            {
                ((DataClass)target).StructValue = (Size)value;
            }
        }
    }
}
