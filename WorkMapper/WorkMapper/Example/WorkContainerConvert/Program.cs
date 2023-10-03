using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace WorkContainerConvert
{
    public static class Program
    {
        public static void Main()
        {
            //BenchmarkRunner.Run<Benchmark2>();
        }
    }

    public static class Converter
    {
        public static int Int64ToInt32(long value) => (int)value;
    }

    public class ResolutionContext
    {
    }

    public enum SourceType
    {
        None,
        Array,
        List,
        IList,
        IEnumerable
    }

    public enum DestinationType
    {
        None,
        Array,
        ListAssignable
    }

    public static class TypeResolver
    {
        public static (SourceType, Type?) ResolveSourceType(Type type)
        {
            if (type.IsArray)
            {
                return (SourceType.Array, type.GetElementType());
            }

            Type? t = type;
            do
            {
                if (t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    return (SourceType.List, t.GetGenericArguments()[0]);
                }

                t = t.BaseType;
            } while (t is not null);

            var listType = type.GetInterfaces().Prepend(type).FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>));
            if (listType is not null)
            {
                return (SourceType.IList, listType.GetGenericArguments()[0]);
            }

            var enumerableType = type.GetInterfaces().Prepend(type).FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (enumerableType is not null)
            {
                return (SourceType.IEnumerable, enumerableType.GetGenericArguments()[0]);
            }

            return (SourceType.None, null);
        }

        public static (DestinationType, Type?) ResolveDestinationType(Type type)
        {
            if (type.IsArray)
            {
                return (DestinationType.Array, type.GetElementType());
            }

            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    return (DestinationType.ListAssignable, type.GetGenericArguments()[0]);
                }

                if (type.IsAssignableFrom(typeof(List<>).MakeGenericType(type.GetGenericArguments()[0])))
                {
                    return (DestinationType.ListAssignable, type.GetGenericArguments()[0]);
                }
            }

            return (DestinationType.None, null);
        }
    }

    // [MEMO] インラインにできないとさして変わらない？、変換処理が非インライン+Virtualによって繰り返す部分が遅いので
    public static class ContainerConverter
    {
        //--------------------------------------------------------------------------------
        // Same
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] SameArrayToArray<T>(T[] source)
        {
            var array = new T[source.Length];
            source.AsSpan().CopyTo(array.AsSpan());
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> SameArrayToList<T>(T[] source)
        {
            return new List<T>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] SameListToArray<T>(List<T> source)
        {
            return source.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> SameListToList<T>(List<T> source)
        {
            return new List<T>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] SameIListToArray<T>(IList<T> source)
        {
            var array = new T[source.Count];
            source.CopyTo(array, 0);
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> SameIListToList<T>(IList<T> source)
        {
            return new List<T>(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] SameIEnumerableToArray<T>(IEnumerable<T> source)
        {
            var list = new List<T>(source);
            return list.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> SameIEnumerableToList<T>(IEnumerable<T> source)
        {
            return new List<T>(source);
        }

        //--------------------------------------------------------------------------------
        // Different
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD[] DifferentArrayToArray<TS, TD>(TS[] source, Func<TS, TD> converter)
        {
            var array = new TD[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                array[i] = value is not null ? converter(value) : default!;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD> DifferentArrayToList<TS, TD>(TS[] source, Func<TS, TD> converter)
        {
            var list = new List<TD>(source.Length);
            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                list.Add(value is not null ? converter(value) : default!);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD[] DifferentListToArray<TS, TD>(List<TS> source, Func<TS, TD> converter)
        {
            var array = new TD[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                var value = source[i];
                array[i] = value is not null ? converter(value) : default!;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD> DifferentListToList<TS, TD>(List<TS> source, Func<TS, TD> converter)
        {
            var list = new List<TD>(source.Count);
            for (var i = 0; i < source.Count; i++)
            {
                var value = source[i];
                list.Add(value is not null ? converter(value) : default!);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD[] DifferentIListToArray<TS, TD>(IList<TS> source, Func<TS, TD> converter)
        {
            var array = new TD[source.Count];
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                var value = source[i];
                array[i] = value is not null ? converter(value) : default!;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD> DifferentIListToList<TS, TD>(IList<TS> source, Func<TS, TD> converter)
        {
            var list = new List<TD>(source.Count);
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                var value = source[i];
                list.Add(value is not null ? converter(value) : default!);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD[] DifferentIEnumerableToArray<TS, TD>(IEnumerable<TS> source, Func<TS, TD> converter)
        {
            var list = new List<TD>();
            foreach (var value in source)
            {
                list.Add(value is not null ? converter(value) : default!);
            }
            return list.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD> DifferentIEnumerableToList<TS, TD>(IEnumerable<TS> source, Func<TS, TD> converter)
        {
            var list = new List<TD>();
            foreach (var value in source)
            {
                list.Add(value is not null ? converter(value) : default!);
            }
            return list;
        }

        //--------------------------------------------------------------------------------
        // Source Nullable
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD[] NullableArrayToArray<TS, TD>(TS?[] source, Func<TS, TD> converter)
            where TS : struct
        {
            var array = new TD[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                array[i] = value.HasValue ? converter(value.Value) : default!;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD> NullableArrayToList<TS, TD>(TS?[] source, Func<TS, TD> converter)
            where TS : struct
        {
            var list = new List<TD>(source.Length);
            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                list.Add(value.HasValue ? converter(value.Value) : default!);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD[] NullableListToArray<TS, TD>(List<TS?> source, Func<TS, TD> converter)
            where TS : struct
        {
            var array = new TD[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                var value = source[i];
                array[i] = value.HasValue ? converter(value.Value) : default!;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD> NullableListToList<TS, TD>(List<TS?> source, Func<TS, TD> converter)
            where TS : struct
        {
            var list = new List<TD>(source.Count);
            for (var i = 0; i < source.Count; i++)
            {
                var value = source[i];
                list.Add(value.HasValue ? converter(value.Value) : default!);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD[] NullableIListToArray<TS, TD>(IList<TS?> source, Func<TS, TD> converter)
            where TS : struct
        {
            var array = new TD[source.Count];
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                var value = source[i];
                array[i] = value.HasValue ? converter(value.Value) : default!;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD> NullableIListToList<TS, TD>(IList<TS?> source, Func<TS, TD> converter)
            where TS : struct
        {
            var list = new List<TD>(source.Count);
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                var value = source[i];
                list.Add(value.HasValue ? converter(value.Value) : default!);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD[] NullableIEnumerableToArray<TS, TD>(IEnumerable<TS?> source, Func<TS, TD> converter)
            where TS : struct
        {
            var list = new List<TD>();
            foreach (var value in source)
            {
                list.Add(value.HasValue ? converter(value.Value) : default!);
            }
            return list.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD> NullableIEnumerableToList<TS, TD>(IEnumerable<TS?> source, Func<TS, TD> converter)
            where TS : struct
        {
            var list = new List<TD>();
            foreach (var value in source)
            {
                list.Add(value.HasValue ? converter(value.Value) : default!);
            }
            return list;
        }

        //--------------------------------------------------------------------------------
        // Destination Nullable
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD?[] DifferentArrayToNullableArray<TS, TD>(TS[] source, Func<TS, TD> converter)
            where TD : struct
        {
            var array = new TD?[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                array[i] = value is not null ? converter(value) : default;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD?> DifferentArrayToNullableList<TS, TD>(TS[] source, Func<TS, TD> converter)
            where TD : struct
        {
            var list = new List<TD?>(source.Length);
            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                list.Add(value is not null ? converter(value) : default);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD?[] DifferentListToNullableArray<TS, TD>(List<TS> source, Func<TS, TD> converter)
            where TD : struct
        {
            var array = new TD?[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                var value = source[i];
                array[i] = value is not null ? converter(value) : default;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD?> DifferentListToNullableList<TS, TD>(List<TS> source, Func<TS, TD> converter)
            where TD : struct
        {
            var list = new List<TD?>(source.Count);
            for (var i = 0; i < source.Count; i++)
            {
                var value = source[i];
                list.Add(value is not null ? converter(value) : default);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD?[] DifferentIListToNullableArray<TS, TD>(IList<TS> source, Func<TS, TD> converter)
            where TD : struct
        {
            var array = new TD?[source.Count];
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                var value = source[i];
                array[i] = value is not null ? converter(value) : default;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD?> DifferentIListToNullableList<TS, TD>(IList<TS> source, Func<TS, TD> converter)
            where TD : struct
        {
            var list = new List<TD?>(source.Count);
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                var value = source[i];
                list.Add(value is not null ? converter(value) : default);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD?[] DifferentIEnumerableToNullableArray<TS, TD>(IEnumerable<TS> source, Func<TS, TD> converter)
            where TD : struct
        {
            var list = new List<TD?>();
            foreach (var value in source)
            {
                list.Add(value is not null ? converter(value) : default);
            }
            return list.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD?> DifferentIEnumerableToNullableList<TS, TD>(IEnumerable<TS> source, Func<TS, TD> converter)
            where TD : struct
        {
            var list = new List<TD?>();
            foreach (var value in source)
            {
                list.Add(value is not null ? converter(value) : default);
            }
            return list;
        }

        //--------------------------------------------------------------------------------
        // Nullable to Nullable
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD?[] NullableArrayToNullableArray<TS, TD>(TS?[] source, Func<TS, TD> converter)
            where TS : struct
            where TD : struct
        {
            var array = new TD?[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                array[i] = value.HasValue ? converter(value.Value) : default;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD?> NullableArrayToNullableList<TS, TD>(TS?[] source, Func<TS, TD> converter)
            where TS : struct
            where TD : struct
        {
            var list = new List<TD?>(source.Length);
            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                list.Add(value.HasValue ? converter(value.Value) : default);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD?[] NullableListToNullableArray<TS, TD>(List<TS?> source, Func<TS, TD> converter)
            where TS : struct
            where TD : struct
        {
            var array = new TD?[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                var value = source[i];
                array[i] = value.HasValue ? converter(value.Value) : default;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD?> NullableListToNullableList<TS, TD>(List<TS?> source, Func<TS, TD> converter)
            where TS : struct
            where TD : struct
        {
            var list = new List<TD?>(source.Count);
            for (var i = 0; i < source.Count; i++)
            {
                var value = source[i];
                list.Add(value.HasValue ? converter(value.Value) : default);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD?[] NullableIListToNullableArray<TS, TD>(IList<TS?> source, Func<TS, TD> converter)
            where TS : struct
            where TD : struct
        {
            var array = new TD?[source.Count];
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                var value = source[i];
                array[i] = value.HasValue ? converter(value.Value) : default;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD?> NullableIListToNullableList<TS, TD>(IList<TS?> source, Func<TS, TD> converter)
            where TS : struct
            where TD : struct
        {
            var list = new List<TD?>(source.Count);
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                var value = source[i];
                list.Add(value.HasValue ? converter(value.Value) : default);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD?[] NullableIEnumerableToNullableArray<TS, TD>(IEnumerable<TS?> source, Func<TS, TD> converter)
            where TS : struct
            where TD : struct
        {
            var list = new List<TD?>();
            foreach (var value in source)
            {
                list.Add(value.HasValue ? converter(value.Value) : default);
            }
            return list.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD?> NullableIEnumerableToNullableList<TS, TD>(IEnumerable<TS?> source, Func<TS, TD> converter)
            where TS : struct
            where TD : struct
        {
            var list = new List<TD?>();
            foreach (var value in source)
            {
                list.Add(value.HasValue ? converter(value.Value) : default);
            }
            return list;
        }

        //--------------------------------------------------------------------------------
        // Different
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD[] DifferentArrayToArray<TS, TD>(TS[] source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
        {
            var array = new TD[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                array[i] = value is not null ? converter(value, context) : default!;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD> DifferentArrayToList<TS, TD>(TS[] source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
        {
            var list = new List<TD>(source.Length);
            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                list.Add(value is not null ? converter(value, context) : default!);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD[] DifferentListToArray<TS, TD>(List<TS> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
        {
            var array = new TD[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                var value = source[i];
                array[i] = value is not null ? converter(value, context) : default!;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD> DifferentListToList<TS, TD>(List<TS> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
        {
            var list = new List<TD>(source.Count);
            for (var i = 0; i < source.Count; i++)
            {
                var value = source[i];
                list.Add(value is not null ? converter(value, context) : default!);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD[] DifferentIListToArray<TS, TD>(IList<TS> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
        {
            var array = new TD[source.Count];
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                var value = source[i];
                array[i] = value is not null ? converter(value, context) : default!;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD> DifferentIListToList<TS, TD>(IList<TS> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
        {
            var list = new List<TD>(source.Count);
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                var value = source[i];
                list.Add(value is not null ? converter(value, context) : default!);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD[] DifferentIEnumerableToArray<TS, TD>(IEnumerable<TS> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
        {
            var list = new List<TD>();
            foreach (var value in source)
            {
                list.Add(value is not null ? converter(value, context) : default!);
            }
            return list.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD> DifferentIEnumerableToList<TS, TD>(IEnumerable<TS> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
        {
            var list = new List<TD>();
            foreach (var value in source)
            {
                list.Add(value is not null ? converter(value, context) : default!);
            }
            return list;
        }

        //--------------------------------------------------------------------------------
        // Source Nullable
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD[] NullableArrayToArray<TS, TD>(TS?[] source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TS : struct
        {
            var array = new TD[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                array[i] = value.HasValue ? converter(value.Value, context) : default!;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD> NullableArrayToList<TS, TD>(TS?[] source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TS : struct
        {
            var list = new List<TD>(source.Length);
            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                list.Add(value.HasValue ? converter(value.Value, context) : default!);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD[] NullableListToArray<TS, TD>(List<TS?> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TS : struct
        {
            var array = new TD[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                var value = source[i];
                array[i] = value.HasValue ? converter(value.Value, context) : default!;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD> NullableListToList<TS, TD>(List<TS?> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TS : struct
        {
            var list = new List<TD>(source.Count);
            for (var i = 0; i < source.Count; i++)
            {
                var value = source[i];
                list.Add(value.HasValue ? converter(value.Value, context) : default!);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD[] NullableIListToArray<TS, TD>(IList<TS?> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TS : struct
        {
            var array = new TD[source.Count];
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                var value = source[i];
                array[i] = value.HasValue ? converter(value.Value, context) : default!;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD> NullableIListToList<TS, TD>(IList<TS?> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TS : struct
        {
            var list = new List<TD>(source.Count);
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                var value = source[i];
                list.Add(value.HasValue ? converter(value.Value, context) : default!);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD[] NullableIEnumerableToArray<TS, TD>(IEnumerable<TS?> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TS : struct
        {
            var list = new List<TD>();
            foreach (var value in source)
            {
                list.Add(value.HasValue ? converter(value.Value, context) : default!);
            }
            return list.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD> NullableIEnumerableToList<TS, TD>(IEnumerable<TS?> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TS : struct
        {
            var list = new List<TD>();
            foreach (var value in source)
            {
                list.Add(value.HasValue ? converter(value.Value, context) : default!);
            }
            return list;
        }

        //--------------------------------------------------------------------------------
        // Destination Nullable
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD?[] DifferentArrayToNullableArray<TS, TD>(TS[] source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TD : struct
        {
            var array = new TD?[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                array[i] = value is not null ? converter(value, context) : default;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD?> DifferentArrayToNullableList<TS, TD>(TS[] source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TD : struct
        {
            var list = new List<TD?>(source.Length);
            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                list.Add(value is not null ? converter(value, context) : default);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD?[] DifferentListToNullableArray<TS, TD>(List<TS> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TD : struct
        {
            var array = new TD?[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                var value = source[i];
                array[i] = value is not null ? converter(value, context) : default;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD?> DifferentListToNullableList<TS, TD>(List<TS> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TD : struct
        {
            var list = new List<TD?>(source.Count);
            for (var i = 0; i < source.Count; i++)
            {
                var value = source[i];
                list.Add(value is not null ? converter(value, context) : default);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD?[] DifferentIListToNullableArray<TS, TD>(IList<TS> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TD : struct
        {
            var array = new TD?[source.Count];
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                var value = source[i];
                array[i] = value is not null ? converter(value, context) : default;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD?> DifferentIListToNullableList<TS, TD>(IList<TS> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TD : struct
        {
            var list = new List<TD?>(source.Count);
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                var value = source[i];
                list.Add(value is not null ? converter(value, context) : default);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD?[] DifferentIEnumerableToNullableArray<TS, TD>(IEnumerable<TS> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TD : struct
        {
            var list = new List<TD?>();
            foreach (var value in source)
            {
                list.Add(value is not null ? converter(value, context) : default);
            }
            return list.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD?> DifferentIEnumerableToNullableList<TS, TD>(IEnumerable<TS> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TD : struct
        {
            var list = new List<TD?>();
            foreach (var value in source)
            {
                list.Add(value is not null ? converter(value, context) : default);
            }
            return list;
        }

        //--------------------------------------------------------------------------------
        // Nullable to Nullable
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD?[] NullableArrayToNullableArray<TS, TD>(TS?[] source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TS : struct
            where TD : struct
        {
            var array = new TD?[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                array[i] = value.HasValue ? converter(value.Value, context) : default;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD?> NullableArrayToNullableList<TS, TD>(TS?[] source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TS : struct
            where TD : struct
        {
            var list = new List<TD?>(source.Length);
            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                list.Add(value.HasValue ? converter(value.Value, context) : default);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD?[] NullableListToNullableArray<TS, TD>(List<TS?> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TS : struct
            where TD : struct
        {
            var array = new TD?[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                var value = source[i];
                array[i] = value.HasValue ? converter(value.Value, context) : default;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD?> NullableListToNullableList<TS, TD>(List<TS?> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TS : struct
            where TD : struct
        {
            var list = new List<TD?>(source.Count);
            for (var i = 0; i < source.Count; i++)
            {
                var value = source[i];
                list.Add(value.HasValue ? converter(value.Value, context) : default);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD?[] NullableIListToNullableArray<TS, TD>(IList<TS?> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TS : struct
            where TD : struct
        {
            var array = new TD?[source.Count];
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                var value = source[i];
                array[i] = value.HasValue ? converter(value.Value, context) : default;
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD?> NullableIListToNullableList<TS, TD>(IList<TS?> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TS : struct
            where TD : struct
        {
            var list = new List<TD?>(source.Count);
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                var value = source[i];
                list.Add(value.HasValue ? converter(value.Value, context) : default);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TD?[] NullableIEnumerableToNullableArray<TS, TD>(IEnumerable<TS?> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TS : struct
            where TD : struct
        {
            var list = new List<TD?>();
            foreach (var value in source)
            {
                list.Add(value.HasValue ? converter(value.Value, context) : default);
            }
            return list.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TD?> NullableIEnumerableToNullableList<TS, TD>(IEnumerable<TS?> source, Func<TS, ResolutionContext, TD> converter, ResolutionContext context)
            where TS : struct
            where TD : struct
        {
            var list = new List<TD?>();
            foreach (var value in source)
            {
                list.Add(value.HasValue ? converter(value.Value, context) : default);
            }
            return list;
        }

        //--------------------------------------------------------------------------------
        // ConvertStatic(最適な生成)
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] StaticArrayToArray(long[] source)
        {
            var array = new int[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                array[i] = Converter.Int64ToInt32(source[i]);
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<int> StaticArrayToList(long[] source)
        {
            var list = new List<int>(source.Length);
            for (var i = 0; i < source.Length; i++)
            {
                list.Add(Converter.Int64ToInt32(source[i]));
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AddSupport<int> StaticArrayToAdd(long[] source)
        {
            var destination = new AddSupport<int>();
            for (var i = 0; i < source.Length; i++)
            {
                destination.Add(Converter.Int64ToInt32(source[i]));
            }
            return destination;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] StaticListToArray(List<long> source)
        {
            var array = new int[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                array[i] = Converter.Int64ToInt32(source[i]);
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<int> StaticListToList(List<long> source)
        {
            var list = new List<int>(source.Count);
            for (var i = 0; i < source.Count; i++)
            {
                list.Add(Converter.Int64ToInt32(source[i]));
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AddSupport<int> StaticListToAdd(List<long> source)
        {
            var destination = new AddSupport<int>();
            for (var i = 0; i < source.Count; i++)
            {
                destination.Add(Converter.Int64ToInt32(source[i]));
            }
            return destination;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] StaticIListToArray(IList<long> source)
        {
            var array = new int[source.Count];
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                array[i] = Converter.Int64ToInt32(source[i]);
            }
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<int> StaticIListToList(IList<long> source)
        {
            var list = new List<int>(source.Count);
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                list.Add(Converter.Int64ToInt32(source[i]));
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AddSupport<int> StaticIListToAdd(IList<long> source)
        {
            var destination = new AddSupport<int>();
            var count = source.Count;
            for (var i = 0; i < count; i++)
            {
                destination.Add(Converter.Int64ToInt32(source[i]));
            }
            return destination;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] StaticIEnumerableToArray(IEnumerable<long> source)
        {
            var list = new List<int>();
            foreach (var value in source)
            {
                list.Add(Converter.Int64ToInt32(value));
            }
            return list.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<int> StaticIEnumerableToList(IEnumerable<long> source)
        {
            var list = new List<int>();
            foreach (var value in source)
            {
                list.Add(Converter.Int64ToInt32(value));
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AddSupport<int> StaticIEnumerableToAdd(IEnumerable<long> source)
        {
            var destination = new AddSupport<int>();
            foreach (var value in source)
            {
                destination.Add(Converter.Int64ToInt32(value));
            }
            return destination;
        }
    }

    //public sealed class ContainerConverter<TS, TD>
    //{
    //    public Func<TS, TD> converter;

    //    //--------------------------------------------------------------------------------
    //    // Member(変換にIF,Funcメンバを使うケース、FuncならCallにできるので改善？)
    //    //--------------------------------------------------------------------------------

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public TD[] DifferentArrayToArray(TS[] source)
    //    {
    //        var array = new TD[source.Length];
    //        for (var i = 0; i < source.Length; i++)
    //        {
    //            var value = source[i];
    //            array[i] = value is not null ? converter(value) : default;
    //        }
    //        return array;
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public List<TD> DifferentArrayToList(TS[] source)
    //    {
    //        var list = new List<TD>(source.Length);
    //        for (var i = 0; i < source.Length; i++)
    //        {
    //            var value = source[i];
    //            list.Add(value is not null ? converter(value) : default);
    //        }
    //        return list;
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public AddSupport<TD> DifferentArrayToAdd(TS[] source)
    //    {
    //        var destination = new AddSupport<TD>();
    //        for (var i = 0; i < source.Length; i++)
    //        {
    //            var value = source[i];
    //            destination.Add(value is not null ? converter(value) : default);
    //        }
    //        return destination;
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public TD[] DifferentListToArray(List<TS> source)
    //    {
    //        var array = new TD[source.Count];
    //        for (var i = 0; i < source.Count; i++)
    //        {
    //            var value = source[i];
    //            array[i] = value is not null ? converter(value) : default;
    //        }
    //        return array;
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public List<TD> DifferentListToList(List<TS> source)
    //    {
    //        var list = new List<TD>(source.Count);
    //        for (var i = 0; i < source.Count; i++)
    //        {
    //            var value = source[i];
    //            list.Add(value is not null ? converter(value) : default);
    //        }
    //        return list;
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public AddSupport<TD> DifferentListToAdd(List<TS> source)
    //    {
    //        var destination = new AddSupport<TD>();
    //        for (var i = 0; i < source.Count; i++)
    //        {
    //            var value = source[i];
    //            destination.Add(value is not null ? converter(value) : default);
    //        }
    //        return destination;
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public TD[] DifferentIListToArray(IList<TS> source)
    //    {
    //        var array = new TD[source.Count];
    //        var count = source.Count;
    //        for (var i = 0; i < count; i++)
    //        {
    //            var value = source[i];
    //            array[i] = value is not null ? converter(value) : default;
    //        }
    //        return array;
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public List<TD> DifferentIListToList(IList<TS> source)
    //    {
    //        var list = new List<TD>(source.Count);
    //        var count = source.Count;
    //        for (var i = 0; i < count; i++)
    //        {
    //            var value = source[i];
    //            list.Add(value is not null ? converter(value) : default);
    //        }
    //        return list;
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public AddSupport<TD> DifferentIListToAdd(IList<TS> source)
    //    {
    //        var destination = new AddSupport<TD>();
    //        var count = source.Count;
    //        for (var i = 0; i < count; i++)
    //        {
    //            var value = source[i];
    //            destination.Add(value is not null ? converter(value) : default);
    //        }
    //        return destination;
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public TD[] DifferentIEnumerableToArray(IEnumerable<TS> source)
    //    {
    //        var list = new List<TD>();
    //        foreach (var value in source)
    //        {
    //            list.Add(value is not null ? converter(value) : default);
    //        }
    //        return list.ToArray();
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public List<TD> DifferentIEnumerableToList(IEnumerable<TS> source)
    //    {
    //        var list = new List<TD>();
    //        foreach (var value in source)
    //        {
    //            list.Add(value is not null ? converter(value) : default);
    //        }
    //        return list;
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public AddSupport<TD> DifferentIEnumerableToAdd(IEnumerable<TS> source)
    //    {
    //        var destination = new AddSupport<TD>();
    //        foreach (var value in source)
    //        {
    //            destination.Add(value is not null ? converter(value) : default);
    //        }
    //        return destination;
    //    }
    //}

    public sealed class AddSupport<T>
    {
        public void Add(T value)
        {
        }
    }

    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddExporter(MarkdownExporter.Default, MarkdownExporter.GitHub);
            AddColumn(
                StatisticColumn.Mean,
                StatisticColumn.Min,
                StatisticColumn.Max,
                StatisticColumn.P90,
                StatisticColumn.Error,
                StatisticColumn.StdDev);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.MediumRun);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class Benchmark
    {
        private const int N = 1000;

        [AllowNull]
        private long[] array;

        [AllowNull]
        private List<long> list;

        [Params(0, 4, 16, 32, 64)]
        public int Size { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            array = new long[Size];
            list = new List<long>(array);
        }

        // Array

        // *
        [Benchmark(OperationsPerInvoke = N)]
        public long ArrayFor()
        {
            var ret = 0L;
            var source = array;
            for (var i = 0; i < N; i++)
            {
                ret = Counter.ArrayFor(source);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public long ArrayForEach()
        {
            var ret = 0L;
            var source = array;
            for (var i = 0; i < N; i++)
            {
                ret = Counter.ArrayForEach(source);
            }

            return ret;
        }

        // List

        // *?
        [Benchmark(OperationsPerInvoke = N)]
        public long ListFor()
        {
            var ret = 0L;
            var source = list;
            for (var i = 0; i < N; i++)
            {
                ret = Counter.ListFor(source);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public long ListFor2()
        {
            var ret = 0L;
            var source = list;
            for (var i = 0; i < N; i++)
            {
                ret = Counter.ListFor2(source);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public long ListForEach()
        {
            var ret = 0L;
            var source = list;
            for (var i = 0; i < N; i++)
            {
                ret = Counter.ListForEach(source);
            }

            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public long IListFor()
        {
            var ret = 0L;
            var source = list;
            for (var i = 0; i < N; i++)
            {
                ret = Counter.IListFor(source);
            }

            return ret;
        }

        // *
        [Benchmark(OperationsPerInvoke = N)]
        public long IListFor2()
        {
            var ret = 0L;
            var source = list;
            for (var i = 0; i < N; i++)
            {
                ret = Counter.IListFor2(source);
            }

            return ret;
        }

        // *
        [Benchmark(OperationsPerInvoke = N)]
        public long IListForEach()
        {
            var ret = 0L;
            var source = list;
            for (var i = 0; i < N; i++)
            {
                ret = Counter.IListForEach(source);
            }

            return ret;
        }
    }

    public static class Counter
    {
        // *
        public static long ArrayFor(long[] array)
        {
            var count = 0L;
            for (var i = 0; i < array.Length; i++)
            {
                count += array[i];
            }
            return count;
        }

        public static long ArrayForEach(long[] array)
        {
            var count = 0L;
            foreach (var value in array)
            {
                count += value;
            }
            return count;
        }

        public static long ListFor(List<long> list)
        {
            var count = 0L;
            for (var i = 0; i < list.Count; i++)
            {
                count += list[i];
            }
            return count;
        }

        public static long ListFor2(List<long> list)
        {
            var count = 0L;
            var length = list.Count;
            for (var i = 0; i < length; i++)
            {
                count += list[i];
            }
            return count;
        }

        public static long ListForEach(List<long> list)
        {
            var count = 0L;
            foreach (var value in list)
            {
                count += value;
            }
            return count;
        }

        public static long IListFor(IList<long> list)
        {
            var count = 0L;
            for (var i = 0; i < list.Count; i++)
            {
                count += list[i];
            }
            return count;
        }

        public static long IListFor2(IList<long> list)
        {
            var count = 0L;
            var length = list.Count;
            for (var i = 0; i < length; i++)
            {
                count += list[i];
            }
            return count;
        }

        public static long IListForEach(IList<long> list)
        {
            var count = 0L;
            foreach (var value in list)
            {
                count += value;
            }
            return count;
        }

        public static long IEnumerableForEach(IEnumerable<long> list)
        {
            var count = 0L;
            foreach (var value in list)
            {
                count += value;
            }
            return count;
        }
    }

    //[Config(typeof(BenchmarkConfig))]
    //public class Benchmark2
    //{
    //    private const int N = 1000;

    //    private int[] array;

    //    [Params(0, 4, 16, 32, 64, 256)]
    //    public int Size { get; set; }

    //    [GlobalSetup]
    //    public void Setup()
    //    {
    //        array = new int[Size];
    //    }

    //    [Benchmark(OperationsPerInvoke = N)]
    //    public void ToArray1()
    //    {
    //        var source = array;
    //        for (var i = 0; i < N; i++)
    //        {
    //            ContainerConverter.SameIEnumerableToArray(new MyEnumerable<int>(source));
    //        }
    //    }


    //    [Benchmark(OperationsPerInvoke = N)]
    //    public void ToArray2()
    //    {
    //        var source = array;
    //        for (var i = 0; i < N; i++)
    //        {
    //            ContainerConverter.SameIEnumerableToArray2(new MyEnumerable<int>(source));
    //        }
    //    }
    //}

    public struct MyEnumerator<T> : IEnumerator<T>
    {
        private readonly T[] array;

        private int index;

        public MyEnumerator(T[] array)
        {
            this.array = array;
            index = -1;
        }

        public bool MoveNext() => (uint)++index < array.Length;

        public void Reset() => throw new NotSupportedException();

        public T Current => array[index];

        object IEnumerator.Current => Current!;

        public void Dispose()
        {
        }
    }

    public readonly struct MyEnumerable<T> : IEnumerable<T>
    {
        private readonly T[] array;

        public MyEnumerable(T[] array)
        {
            this.array = array;
        }

        public IEnumerator<T> GetEnumerator() => new MyEnumerator<T>(array);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
