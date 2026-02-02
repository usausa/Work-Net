#pragma warning disable SA1312
namespace ConvertBenchmark;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

public static class Program
{
    public static void Main()
    {
        BenchmarkRunner.Run<Benchmark>();
    }
}

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddExporter(MarkdownExporter.GitHub);
        AddColumn(
            StatisticColumn.Mean,
            StatisticColumn.Min,
            StatisticColumn.Max,
            StatisticColumn.P90,
            StatisticColumn.Error,
            StatisticColumn.StdDev);
    }
}

[Config(typeof(BenchmarkConfig))]
public class Benchmark
{
    [Benchmark]
    public int Direct()
    {
        return Int32.Parse("123");
    }

    [Benchmark]
    public int Generic()
    {
        return DefaultValueConverter.Convert<string, int>("123");
    }

    [Benchmark]
    public int Specialized()
    {
        return DefaultValueConverter.ConvertToInt32("123");
    }
}

public static class DefaultValueConverter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ConvertToInt32(string source) => Int32.Parse(source);

    /// <summary>
    /// Converts a value from source type to destination type.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <returns>The converted value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TDestination Convert<TSource, TDestination>(TSource source)
    {
        // Same type - no conversion needed
        if (typeof(TSource) == typeof(TDestination))
        {
            return (TDestination)(object)source!;
        }

        // Nullable<T> -> T (get value)
        var sourceUnderlyingType = Nullable.GetUnderlyingType(typeof(TSource));
        if (sourceUnderlyingType == typeof(TDestination))
        {
            return (TDestination)(object)source!;
        }

        // T -> Nullable<T>
        if (Nullable.GetUnderlyingType(typeof(TDestination)) == typeof(TSource))
        {
            return (TDestination)(object)source!;
        }

        // Nullable<T> -> string (special case: call ToString on underlying value)
        if (sourceUnderlyingType is not null && typeof(TDestination) == typeof(string))
        {
            if (source is null)
            {
                return default!;
            }
            return (TDestination)(object)source.ToString()!;
        }

        // Numeric conversions - JIT will optimize away unused branches
        return ConvertNumeric<TSource, TDestination>(source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TDestination ConvertNumeric<TSource, TDestination>(TSource source)
    {
        // int -> other numeric types
        if (typeof(TSource) == typeof(int))
        {
            var value = (int)(object)source!;
            if (typeof(TDestination) == typeof(long)) return (TDestination)(object)(long)value;
            if (typeof(TDestination) == typeof(short)) return (TDestination)(object)(short)value;
            if (typeof(TDestination) == typeof(byte)) return (TDestination)(object)(byte)value;
            if (typeof(TDestination) == typeof(sbyte)) return (TDestination)(object)(sbyte)value;
            if (typeof(TDestination) == typeof(uint)) return (TDestination)(object)(uint)value;
            if (typeof(TDestination) == typeof(ulong)) return (TDestination)(object)(ulong)value;
            if (typeof(TDestination) == typeof(ushort)) return (TDestination)(object)(ushort)value;
            if (typeof(TDestination) == typeof(float)) return (TDestination)(object)(float)value;
            if (typeof(TDestination) == typeof(double)) return (TDestination)(object)(double)value;
            if (typeof(TDestination) == typeof(decimal)) return (TDestination)(object)(decimal)value;
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString()!;
        }

        // long -> other numeric types
        if (typeof(TSource) == typeof(long))
        {
            var value = (long)(object)source!;
            if (typeof(TDestination) == typeof(int)) return (TDestination)(object)(int)value;
            if (typeof(TDestination) == typeof(short)) return (TDestination)(object)(short)value;
            if (typeof(TDestination) == typeof(byte)) return (TDestination)(object)(byte)value;
            if (typeof(TDestination) == typeof(sbyte)) return (TDestination)(object)(sbyte)value;
            if (typeof(TDestination) == typeof(uint)) return (TDestination)(object)(uint)value;
            if (typeof(TDestination) == typeof(ulong)) return (TDestination)(object)(ulong)value;
            if (typeof(TDestination) == typeof(ushort)) return (TDestination)(object)(ushort)value;
            if (typeof(TDestination) == typeof(float)) return (TDestination)(object)(float)value;
            if (typeof(TDestination) == typeof(double)) return (TDestination)(object)(double)value;
            if (typeof(TDestination) == typeof(decimal)) return (TDestination)(object)(decimal)value;
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString()!;
        }

        // short -> other numeric types
        if (typeof(TSource) == typeof(short))
        {
            var value = (short)(object)source!;
            if (typeof(TDestination) == typeof(int)) return (TDestination)(object)(int)value;
            if (typeof(TDestination) == typeof(long)) return (TDestination)(object)(long)value;
            if (typeof(TDestination) == typeof(byte)) return (TDestination)(object)(byte)value;
            if (typeof(TDestination) == typeof(sbyte)) return (TDestination)(object)(sbyte)value;
            if (typeof(TDestination) == typeof(uint)) return (TDestination)(object)(uint)value;
            if (typeof(TDestination) == typeof(ulong)) return (TDestination)(object)(ulong)value;
            if (typeof(TDestination) == typeof(ushort)) return (TDestination)(object)(ushort)value;
            if (typeof(TDestination) == typeof(float)) return (TDestination)(object)(float)value;
            if (typeof(TDestination) == typeof(double)) return (TDestination)(object)(double)value;
            if (typeof(TDestination) == typeof(decimal)) return (TDestination)(object)(decimal)value;
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString()!;
        }

        // byte -> other numeric types
        if (typeof(TSource) == typeof(byte))
        {
            var value = (byte)(object)source!;
            if (typeof(TDestination) == typeof(int)) return (TDestination)(object)(int)value;
            if (typeof(TDestination) == typeof(long)) return (TDestination)(object)(long)value;
            if (typeof(TDestination) == typeof(short)) return (TDestination)(object)(short)value;
            if (typeof(TDestination) == typeof(sbyte)) return (TDestination)(object)(sbyte)value;
            if (typeof(TDestination) == typeof(uint)) return (TDestination)(object)(uint)value;
            if (typeof(TDestination) == typeof(ulong)) return (TDestination)(object)(ulong)value;
            if (typeof(TDestination) == typeof(ushort)) return (TDestination)(object)(ushort)value;
            if (typeof(TDestination) == typeof(float)) return (TDestination)(object)(float)value;
            if (typeof(TDestination) == typeof(double)) return (TDestination)(object)(double)value;
            if (typeof(TDestination) == typeof(decimal)) return (TDestination)(object)(decimal)value;
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString()!;
        }

        // float -> other numeric types
        if (typeof(TSource) == typeof(float))
        {
            var value = (float)(object)source!;
            if (typeof(TDestination) == typeof(int)) return (TDestination)(object)(int)value;
            if (typeof(TDestination) == typeof(long)) return (TDestination)(object)(long)value;
            if (typeof(TDestination) == typeof(short)) return (TDestination)(object)(short)value;
            if (typeof(TDestination) == typeof(byte)) return (TDestination)(object)(byte)value;
            if (typeof(TDestination) == typeof(double)) return (TDestination)(object)(double)value;
            if (typeof(TDestination) == typeof(decimal)) return (TDestination)(object)(decimal)value;
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString()!;
        }

        // double -> other numeric types
        if (typeof(TSource) == typeof(double))
        {
            var value = (double)(object)source!;
            if (typeof(TDestination) == typeof(int)) return (TDestination)(object)(int)value;
            if (typeof(TDestination) == typeof(long)) return (TDestination)(object)(long)value;
            if (typeof(TDestination) == typeof(short)) return (TDestination)(object)(short)value;
            if (typeof(TDestination) == typeof(byte)) return (TDestination)(object)(byte)value;
            if (typeof(TDestination) == typeof(float)) return (TDestination)(object)(float)value;
            if (typeof(TDestination) == typeof(decimal)) return (TDestination)(object)(decimal)value;
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString()!;
        }

        // decimal -> other numeric types
        if (typeof(TSource) == typeof(decimal))
        {
            var value = (decimal)(object)source!;
            if (typeof(TDestination) == typeof(int)) return (TDestination)(object)(int)value;
            if (typeof(TDestination) == typeof(long)) return (TDestination)(object)(long)value;
            if (typeof(TDestination) == typeof(short)) return (TDestination)(object)(short)value;
            if (typeof(TDestination) == typeof(byte)) return (TDestination)(object)(byte)value;
            if (typeof(TDestination) == typeof(float)) return (TDestination)(object)(float)value;
            if (typeof(TDestination) == typeof(double)) return (TDestination)(object)(double)value;
            if (typeof(TDestination) == typeof(string)) return (TDestination)(object)value.ToString()!;
        }

        // string -> numeric types (parsing)
        if (typeof(TSource) == typeof(string))
        {
            var value = (string)(object)source!;
            if (typeof(TDestination) == typeof(int)) return (TDestination)(object)int.Parse(value);
            if (typeof(TDestination) == typeof(long)) return (TDestination)(object)long.Parse(value);
            if (typeof(TDestination) == typeof(short)) return (TDestination)(object)short.Parse(value);
            if (typeof(TDestination) == typeof(byte)) return (TDestination)(object)byte.Parse(value);
            if (typeof(TDestination) == typeof(sbyte)) return (TDestination)(object)sbyte.Parse(value);
            if (typeof(TDestination) == typeof(uint)) return (TDestination)(object)uint.Parse(value);
            if (typeof(TDestination) == typeof(ulong)) return (TDestination)(object)ulong.Parse(value);
            if (typeof(TDestination) == typeof(ushort)) return (TDestination)(object)ushort.Parse(value);
            if (typeof(TDestination) == typeof(float)) return (TDestination)(object)float.Parse(value);
            if (typeof(TDestination) == typeof(double)) return (TDestination)(object)double.Parse(value);
            if (typeof(TDestination) == typeof(decimal)) return (TDestination)(object)decimal.Parse(value);
            if (typeof(TDestination) == typeof(bool)) return (TDestination)(object)bool.Parse(value);
            if (typeof(TDestination) == typeof(DateTime)) return (TDestination)(object)DateTime.Parse(value);
            if (typeof(TDestination) == typeof(Guid)) return (TDestination)(object)Guid.Parse(value);
        }

        // bool -> string
        if (typeof(TSource) == typeof(bool) && typeof(TDestination) == typeof(string))
        {
            var value = (bool)(object)source!;
            return (TDestination)(object)value.ToString()!;
        }

        // DateTime -> string
        if (typeof(TSource) == typeof(DateTime) && typeof(TDestination) == typeof(string))
        {
            var value = (DateTime)(object)source!;
            return (TDestination)(object)value.ToString()!;
        }

        // Guid -> string
        if (typeof(TSource) == typeof(Guid) && typeof(TDestination) == typeof(string))
        {
            var value = (Guid)(object)source!;
            return (TDestination)(object)value.ToString()!;
        }

        // Fallback: try direct cast (for enums and compatible types)
        return (TDestination)(object)source!;
    }
}
