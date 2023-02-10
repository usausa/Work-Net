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
    public void Pascalize()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector.Underscore("abc_xyz");
        }
    }

    [Benchmark]
    public void Camelize()
    {
        for (var i = 0; i < N; i++)
        {
            Inflector.Underscore("abc_xyz");
        }
    }

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
