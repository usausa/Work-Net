using System;
using System.Collections.Generic;

namespace WorkGetConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            var converters = new ConverterDictionary();
            //converters.Set(typeof(long), typeof(int), 1);
            converters.Set(typeof(long?), typeof(int), 2);
            converters.Set(typeof(long?), typeof(int?), 3);
            //converters.Set(typeof(long), typeof(int?), 4);

            var ret = converters.TryGetConverter(typeof(long), typeof(int?), out var result);
        }
    }

    internal sealed class ConverterDictionary
    {
        private readonly Dictionary<Tuple<Type, Type>, object> converters = new();

        public void Set(Type sourceType, Type destinationType, object func)
        {
            converters[Tuple.Create(sourceType, destinationType)] = func;
        }

        public bool TryGetConverter(Type sourceType, Type destinationType, out object func)
        {
            if (converters.TryGetValue(Tuple.Create(sourceType, destinationType), out func))
            {
                return true;
            }

            var sourceUnderlyingType = Nullable.GetUnderlyingType(sourceType);
            if (sourceUnderlyingType is not null)
            {
                if (converters.TryGetValue(Tuple.Create(sourceUnderlyingType, destinationType), out func))
                {
                    return true;
                }

                sourceType = sourceUnderlyingType;
            }

            var destinationUnderlyingType = Nullable.GetUnderlyingType(destinationType);
            if (destinationUnderlyingType is not null)
            {
                if (converters.TryGetValue(Tuple.Create(sourceType, destinationUnderlyingType), out func))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
