namespace Works
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    using Works.Helpers;

    public static class Program
    {
        public static void Main(string[] args)
        {
        }
    }

    //--------------------------------------------------------------------------------
    // Converter
    //--------------------------------------------------------------------------------

    public class WorkConverter
    {
        // TODO 2つ？
        //private readonly TypePairHashArray converterCache = new TypePairHashArray();

        private readonly IConverterFactory[] factories;

        public WorkConverter()
        {
            factories = new IConverterFactory[]
            {
                new NumericCastConverterFactory(),
                new ToStringConverterFactory(),
            };
        }

        //--------------------------------------------------------------------------------
        // Helper
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Delegate FindConverter(Type sourceType, Type targetType)
        {
            for (var i = 0; i < factories.Length; i++)
            {
                var converter = factories[i].GetConverter(sourceType, targetType);
                if (converter != null)
                {
                    return converter;
                }
            }

            return null;
        }

        // TODO 2つ？
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private Delegate GetConverter(Type sourceType, Type targetType)
        //{
        //    if (!converterCache.TryGetValue(sourceType, targetType, out var converter))
        //    {
        //        converter = converterCache.AddIfNotExist(sourceType, targetType, FindConverter);
        //    }

        //    return converter;
        //}

        //--------------------------------------------------------------------------------
        // CanConvert
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanConvert<TTarget>(object value) => CanConvert(value, typeof(TTarget));

        public bool CanConvert(object value, Type targetType)
        {
            // nullはターゲットのデフォルトに必ず変換できる
            if (value is null)
            {
                return true;
            }

            // 同じ型は必ず変換できる
            var sourceType = value.GetType();
            if (sourceType == (targetType.IsNullableType() ? Nullable.GetUnderlyingType(targetType) : targetType))
            {
                return true;
            }

            //return GetConverter(sourceType, targetType) != null;
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanConvert<TTarget>(Type sourceType) => CanConvert(sourceType, typeof(TTarget));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanConvert<TSource, TTarget>() => CanConvert(typeof(TSource), typeof(TTarget));

        public bool CanConvert(Type sourceType, Type targetType)
        {
            // TODO 影響による検証をした後で
            //return GetConverter(sourceType.IsNullableType() ? Nullable.GetUnderlyingType(sourceType) : sourceType, targetType) != null;
            throw new NotImplementedException();
        }

        //--------------------------------------------------------------------------------
        // Convert
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TTarget Convert<TTarget>(object value)
        {
            // TODO 最適化できる？
            //return (TTarget)Convert(value, typeof(TTarget));
            throw new NotImplementedException();
        }

        public object Convert(object value, Type targetType)
        {
            // Specialized null
            if (value is null)
            {
                return targetType.GetDefaultValue();
            }

            // Specialized same type for performance (Nullable is excluded because operation is slow)
            var sourceType = value.GetType();
            if (sourceType == (targetType.IsNullableType() ? Nullable.GetUnderlyingType(targetType) : targetType))
            {
                return value;
            }

            // TODO 要検証
            //var converter = GetConverter(sourceType, targetType);
            //if (converter is null)
            //{
            //    throw new ObjectConverterException($"Type {sourceType} can't convert to {targetType}");
            //}

            //return converter(value);
            throw new NotImplementedException();
        }

        public TTarget Convert<TSource, TTarget>(TSource value)
        {
            throw new NotImplementedException();
        }

        //--------------------------------------------------------------------------------
        // CreateConverter
        //--------------------------------------------------------------------------------

        public Func<object, object> CreateConverter(Type sourceType, Type targetType)
        {
            //var converter = GetConverter(sourceType.IsNullableType() ? Nullable.GetUnderlyingType(sourceType) : sourceType, targetType);
            //if (converter is null)
            //{
            //    return null;
            //}

            //return CreateConverter(
            //    targetType.GetDefaultValue(),
            //    targetType.IsNullableType() ? Nullable.GetUnderlyingType(targetType) : targetType,
            //    converter);
            throw new NotImplementedException();
        }

        //private static Func<object, object> CreateConverter(object defaultValue, Type targetType, Func<object, object> converter)
        //{
        //    return value => value is null
        //        ? defaultValue
        //        : value.GetType() == targetType
        //            ? value
        //            : converter(value);
        //}

        // TODO struct version split ?
        public Func<TSource, TTarget> CreateConverter<TSource, TTarget>()
        {
            throw new NotImplementedException();
        }
    }

    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    public interface IConverterFactory
    {
        Delegate GetConverter(Type sourceType, Type targetType);
    }

    public sealed class ToStringConverterFactory : IConverterFactory
    {
        private static readonly Func<object, object> Converter = source => source.ToString();

        public Delegate GetConverter(Type sourceType, Type targetType)
        {
            return targetType == typeof(string) ? Converter : null;
        }
    }

    public sealed class NumericCastConverterFactory : IConverterFactory
    {
        private static readonly Dictionary<Tuple<Type, Type>, Delegate> Converters = new Dictionary<Tuple<Type, Type>, Delegate>
        {
            // int
            { Tuple.Create(typeof(int), typeof(long)), (Func<int, long>)(x => (long)x) },
            // long
            { Tuple.Create(typeof(long), typeof(int)), (Func<long, int>)(x => (int)x) },
        };

        public Delegate GetConverter(Type sourceType, Type targetType)
        {
            if (sourceType.IsValueType && targetType.IsValueType)
            {
                var key = Tuple.Create(sourceType, targetType.IsNullableType() ? Nullable.GetUnderlyingType(targetType) : targetType);
                if (Converters.TryGetValue(key, out var converter))
                {
                    return converter;
                }
            }

            return null;
        }
    }
}
