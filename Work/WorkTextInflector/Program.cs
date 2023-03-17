#pragma warning disable IDE0046
namespace WorkTextInflector;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class Program
{
    public static void Main()
    {
        _ = BenchmarkRunner.Run<Benchmark>();
    }
}

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        _ = AddExporter(MarkdownExporter.GitHub);
        _ = AddColumn(
            StatisticColumn.Mean,
            StatisticColumn.Min,
            StatisticColumn.Max,
            StatisticColumn.P90,
            StatisticColumn.Error,
            StatisticColumn.StdDev);
        _ = AddDiagnoser(MemoryDiagnoser.Default);
        _ = AddJob(Job.MediumRun);
    }
}

[Config(typeof(BenchmarkConfig))]
public class Benchmark
{
    private const int N = 1000;

    [Benchmark]
    [BenchmarkCategory("Pascalize")]
    public void Pascalize()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector.Pascalize("abc_xyz_abc_xyz");
        }
    }

    [Benchmark]
    [BenchmarkCategory("Camelize")]
    public void Camelize()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector.Camelize("abc_xyz_abc_xyz");
        }
    }

    [Benchmark]
    [BenchmarkCategory("Underscore")]
    public void Underscore()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector.Underscore("AbcXyzAbcXyz");
        }
    }

    [Benchmark]
    [BenchmarkCategory("UnderscoreUpper")]
    public void UnderscoreUpper()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector.Underscore("AbcXyzAbcXyz", true);
        }
    }

    // 2

    [Benchmark]
    [BenchmarkCategory("Pascalize")]
    public void Pascalize2()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector2.Pascalize("abc_xyz_abc_xyz");
        }
    }

    [Benchmark]
    [BenchmarkCategory("Camelize")]
    public void Camelize2()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector2.Camelize("abc_xyz_abc_xyz");
        }
    }

    [Benchmark]
    [BenchmarkCategory("Underscore")]
    public void Underscore2()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector2.Underscore("AbcXyzAbcXyz");
        }
    }

    [Benchmark]
    [BenchmarkCategory("UnderscoreUpper")]
    public void UnderscoreUpper2()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector2.Underscore("AbcXyzAbcXyz", true);
        }
    }

    // 3

    [Benchmark]
    [BenchmarkCategory("Pascalize")]
    public void Pascalize3()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector3.Pascalize("abc_xyz_abc_xyz");
        }
    }

    [Benchmark]
    [BenchmarkCategory("Camelize")]
    public void Camelize3()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector3.Camelize("abc_xyz_abc_xyz");
        }
    }

    [Benchmark]
    [BenchmarkCategory("Underscore")]
    public void Underscore3()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector3.Underscore("AbcXyzAbcXyz");
        }
    }

    [Benchmark]
    [BenchmarkCategory("UnderscoreUpper")]
    public void UnderscoreUpper3()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector3.Underscore("AbcXyzAbcXyz", true);
        }
    }
}

public static class Inflector
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Pascalize(ReadOnlySpan<char> word) => Camelize(word, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Camelize(ReadOnlySpan<char> word) => Camelize(word, false);

    public static unsafe string Camelize(ReadOnlySpan<char> word, bool toUpper)
    {
        if (word.IsEmpty)
        {
            return string.Empty;
        }

        var buffer = word.Length < 512 ? stackalloc char[word.Length] : new char[word.Length];
        var length = 0;

        fixed (char* pBuffer = buffer)
        {
            var isLowerPrevious = false;
            foreach (var c in word)
            {
                if (c == '_')
                {
                    toUpper = true;
                }
                else
                {
                    if (toUpper)
                    {
                        pBuffer[length++] = Char.ToUpperInvariant(c);
                        toUpper = false;
                    }
                    else if (isLowerPrevious)
                    {
                        pBuffer[length++] = c;
                    }
                    else
                    {
                        pBuffer[length++] = Char.ToLowerInvariant(c);
                    }

                    isLowerPrevious = Char.IsLower(c);
                }
            }

            return new string(pBuffer, 0, length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Underscore(ReadOnlySpan<char> word) => Underscore(word, false);

    public static unsafe string Underscore(ReadOnlySpan<char> word, bool toUpper)
    {
        if (word.IsEmpty)
        {
            return string.Empty;
        }

        var bufferSize = word.Length << 1;
        var buffer = bufferSize < 512 ? stackalloc char[bufferSize] : new char[bufferSize];
        var length = 0;

        fixed (char* pBuffer = buffer)
        {
            if (toUpper)
            {
                foreach (var c in word)
                {
                    if (Char.IsUpper(c))
                    {
                        if (length > 0)
                        {
                            pBuffer[length++] = '_';
                        }

                        pBuffer[length++] = c;
                    }
                    else
                    {
                        pBuffer[length++] = Char.ToUpperInvariant(c);
                    }
                }
            }
            else
            {
                foreach (var c in word)
                {
                    if (Char.IsUpper(c))
                    {
                        if (length > 0)
                        {
                            pBuffer[length++] = '_';
                        }

                        pBuffer[length++] = Char.ToLowerInvariant(c);
                    }
                    else
                    {
                        pBuffer[length++] = c;
                    }
                }
            }

            return new string(pBuffer, 0, length);
        }
    }
}

public static class Inflector2
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Pascalize(ReadOnlySpan<char> word) => Camelize(word, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Camelize(ReadOnlySpan<char> word) => Camelize(word, false);

    public static unsafe string Camelize(ReadOnlySpan<char> word, bool toUpper)
    {
        if (word.IsEmpty)
        {
            return string.Empty;
        }

        var buffer = word.Length < 512 ? stackalloc char[word.Length] : new char[word.Length];

        ref var start = ref MemoryMarshal.GetReference(buffer);
        ref var b = ref start;

        var isLowerPrevious = false;
        foreach (var c in word)
        {
            if (c == '_')
            {
                toUpper = true;
            }
            else
            {
                if (toUpper)
                {
                    b = Char.ToUpperInvariant(c);
                    b = ref Unsafe.Add(ref b, 1);
                    toUpper = false;
                }
                else if (isLowerPrevious)
                {
                    b = c;
                    b = ref Unsafe.Add(ref b, 1);
                }
                else
                {
                    b = Char.ToLowerInvariant(c);
                    b = ref Unsafe.Add(ref b, 1);
                }

                isLowerPrevious = Char.IsLower(c);
            }
        }

        return new string(buffer[..((int)Unsafe.ByteOffset(ref start, ref b) >> 1)]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Underscore(ReadOnlySpan<char> word) => Underscore(word, false);

    public static unsafe string Underscore(ReadOnlySpan<char> word, bool toUpper)
    {
        if (word.IsEmpty)
        {
            return string.Empty;
        }

        var bufferSize = word.Length << 1;
        var buffer = bufferSize < 512 ? stackalloc char[bufferSize] : new char[bufferSize];

        ref var start = ref MemoryMarshal.GetReference(buffer);
        ref var b = ref start;

        if (toUpper)
        {
            foreach (var c in word)
            {
                if (Char.IsUpper(c))
                {
                    if (Unsafe.IsAddressGreaterThan(ref b, ref start))
                    {
                        b = '_';
                        b = ref Unsafe.Add(ref b, 1);
                    }

                    b = c;
                    b = ref Unsafe.Add(ref b, 1);
                }
                else
                {
                    b = Char.ToUpperInvariant(c);
                    b = ref Unsafe.Add(ref b, 1);
                }
            }
        }
        else
        {
            foreach (var c in word)
            {
                if (Char.IsUpper(c))
                {
                    if (Unsafe.IsAddressGreaterThan(ref b, ref start))
                    {
                        b = '_';
                        b = ref Unsafe.Add(ref b, 1);
                    }

                    b = Char.ToLowerInvariant(c);
                    b = ref Unsafe.Add(ref b, 1);
                }
                else
                {
                    b = c;
                    b = ref Unsafe.Add(ref b, 1);
                }
            }
        }

        return new string(buffer[..((int)Unsafe.ByteOffset(ref start, ref b) >> 1)]);
    }
}

public static class Inflector3
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Pascalize(ReadOnlySpan<char> word) => Camelize(word, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Camelize(ReadOnlySpan<char> word) => Camelize(word, false);

    public static unsafe string Camelize(ReadOnlySpan<char> word, bool toUpper)
    {
        if (word.IsEmpty)
        {
            return string.Empty;
        }

        var length = word.Length;
        var buffer = length < 512 ? stackalloc char[length] : new char[length];

        ref var start = ref MemoryMarshal.GetReference(buffer);
        ref var b = ref start;

        ref var c = ref MemoryMarshal.GetReference(word);
        ref var end = ref Unsafe.Add(ref c, length);

        var isLowerPrevious = false;
        do
        {
            if (c == '_')
            {
                toUpper = true;
            }
            else
            {
                if (toUpper)
                {
                    b = Char.ToUpperInvariant(c);
                    b = ref Unsafe.Add(ref b, 1);
                    toUpper = false;
                }
                else if (isLowerPrevious)
                {
                    b = c;
                    b = ref Unsafe.Add(ref b, 1);
                }
                else
                {
                    b = Char.ToLowerInvariant(c);
                    b = ref Unsafe.Add(ref b, 1);
                }

                isLowerPrevious = Char.IsLower(c);
            }

            c = ref Unsafe.Add(ref c, 1);
        } while (Unsafe.IsAddressLessThan(ref c, ref end));

        return new string(buffer[..((int)Unsafe.ByteOffset(ref start, ref b) >> 1)]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Underscore(ReadOnlySpan<char> word) => Underscore(word, false);

    public static unsafe string Underscore(ReadOnlySpan<char> word, bool toUpper)
    {
        if (word.IsEmpty)
        {
            return string.Empty;
        }

        var length = word.Length;
        var bufferSize = length << 1;
        var buffer = bufferSize < 512 ? stackalloc char[bufferSize] : new char[bufferSize];

        ref var start = ref MemoryMarshal.GetReference(buffer);
        ref var b = ref start;

        ref var c = ref MemoryMarshal.GetReference(word);
        ref var end = ref Unsafe.Add(ref c, length);

        if (toUpper)
        {
            if (Char.IsUpper(c))
            {
                b = c;
                b = ref Unsafe.Add(ref b, 1);
            }
            else
            {
                b = Char.ToUpperInvariant(c);
                b = ref Unsafe.Add(ref b, 1);
            }

            c = ref Unsafe.Add(ref c, 1);

            while (Unsafe.IsAddressLessThan(ref c, ref end))
            {
                if (Char.IsUpper(c))
                {
                    b = '_';
                    b = ref Unsafe.Add(ref b, 1);
                    b = c;
                    b = ref Unsafe.Add(ref b, 1);
                }
                else
                {
                    b = Char.ToUpperInvariant(c);
                    b = ref Unsafe.Add(ref b, 1);
                }

                c = ref Unsafe.Add(ref c, 1);
            }
        }
        else
        {
            if (Char.IsUpper(c))
            {
                b = Char.ToLowerInvariant(c);
                b = ref Unsafe.Add(ref b, 1);
            }
            else
            {
                b = c;
                b = ref Unsafe.Add(ref b, 1);
            }

            c = ref Unsafe.Add(ref c, 1);

            while (Unsafe.IsAddressLessThan(ref c, ref end))
            {
                if (Char.IsUpper(c))
                {
                    b = '_';
                    b = ref Unsafe.Add(ref b, 1);
                    b = Char.ToLowerInvariant(c);
                    b = ref Unsafe.Add(ref b, 1);
                }
                else
                {
                    b = c;
                    b = ref Unsafe.Add(ref b, 1);
                }

                c = ref Unsafe.Add(ref c, 1);
            }
        }

        return new string(buffer[..((int)Unsafe.ByteOffset(ref start, ref b) >> 1)]);
    }
}
