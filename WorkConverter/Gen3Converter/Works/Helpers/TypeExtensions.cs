namespace Works.Helpers
{
    using System;
    using System.Runtime.CompilerServices;

    public static class TypeExtensions
    {
        private static readonly Type NullableType = typeof(Nullable<>);

        private static readonly ThreadsafeTypeHashArrayMap<object> DefaultValues = new ThreadsafeTypeHashArrayMap<object>();

        private static readonly Func<Type, object> NullFactory = t => null;

        private static readonly Func<Type, object> ValueFactory = Activator.CreateInstance;

        static TypeExtensions()
        {
            DefaultValues.AddIfNotExist(typeof(bool), default(bool));
            DefaultValues.AddIfNotExist(typeof(byte), default(byte));
            DefaultValues.AddIfNotExist(typeof(sbyte), default(sbyte));
            DefaultValues.AddIfNotExist(typeof(short), default(short));
            DefaultValues.AddIfNotExist(typeof(ushort), default(ushort));
            DefaultValues.AddIfNotExist(typeof(int), default(int));
            DefaultValues.AddIfNotExist(typeof(uint), default(uint));
            DefaultValues.AddIfNotExist(typeof(long), default(long));
            DefaultValues.AddIfNotExist(typeof(ulong), default(ulong));
            DefaultValues.AddIfNotExist(typeof(IntPtr), default(IntPtr));
            DefaultValues.AddIfNotExist(typeof(UIntPtr), default(UIntPtr));
            DefaultValues.AddIfNotExist(typeof(char), default(char));
            DefaultValues.AddIfNotExist(typeof(double), default(double));
            DefaultValues.AddIfNotExist(typeof(float), default(float));
            DefaultValues.AddIfNotExist(typeof(decimal), default(decimal));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object GetDefaultValue(this Type type)
        {
            if (type.IsValueType)
            {
                if (DefaultValues.TryGetValue(type, out object value))
                {
                    return value;
                }

                if (type.IsNullableType())
                {
                    return DefaultValues.AddIfNotExist(type, NullFactory);
                }

                return DefaultValues.AddIfNotExist(type, ValueFactory);
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullableType(this Type type)
        {
            return type.IsGenericType && (type.GetGenericTypeDefinition() == NullableType);
        }
    }
}
