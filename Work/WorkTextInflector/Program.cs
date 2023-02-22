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

    //[Benchmark]
    //public void Pascalize()
    //{
    //    for (var i = 0; i < N; i++)
    //    {
    //        Inflector.Pascalize("abc_xyz");
    //    }
    //}

    //[Benchmark]
    //public void Camelize()
    //{
    //    for (var i = 0; i < N; i++)
    //    {
    //        Inflector.Camelize("abc_xyz");
    //    }
    //}

    [Benchmark]
    public void Underscore()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector.Underscore("AbcXyz");
        }
    }

    [Benchmark]
    public void UnderscoreUpper()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector.Underscore("AbcXyz", true);
        }
    }

    // 2

    // TODO

    [Benchmark]
    public void Underscore2()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector2.Underscore("AbcXyz");
        }
    }

    [Benchmark]
    public void UnderscoreUpper2()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector2.Underscore("AbcXyz", true);
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
        var length = 0;

        // TODO
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

    // TODO 最初のcを特殊化することでUnsafe.IsAddressGreaterThanを減らせるか? v3
    // TODO word, cもUnsafeベース?、EndとUnsafe.IsAddressLessThan?
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
