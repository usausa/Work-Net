namespace WorkBenchmarkStringCompare;

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
        AddExporter(MarkdownExporter.GitHub);
        AddColumn(
            StatisticColumn.Mean,
            StatisticColumn.Min,
            StatisticColumn.Max,
            StatisticColumn.P90,
            StatisticColumn.Error,
            StatisticColumn.StdDev);
        AddDiagnoser(MemoryDiagnoser.Default, new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(maxDepth: 3, printSource: true, printInstructionAddresses: true, exportDiff: true)));
        AddJob(Job.MediumRun);
    }
}

[Config(typeof(BenchmarkConfig))]
public class Benchmark
{
    [Params(2, 4, 8, 16)]
    public int Size { get; set; }

    private string x = default!;

    private string y = default!;

    [GlobalSetup]
    public void Setup()
    {
        x = new string('0', Size - 1) + "X";
        y = new string('0', Size - 1) + "Y";
    }

    [Benchmark]
    public bool OperatorEqual() => x == y;

    [Benchmark]
    public bool EqualsDefault() => String.Equals(x, y);

    [Benchmark]
    public bool EqualsOrdinal() => String.Equals(x, y, StringComparison.Ordinal);

    [Benchmark]

    public bool SpanSequence() => x.AsSpan().SequenceEqual(y);

    [Benchmark]

    public bool Custom() => Methods.IsNameEquals(x, y);
}

public sealed class Methods
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool IsNameEquals(string name1, string name2)
    {
        var length = name1.Length;
        if (length != name2.Length)
        {
            return false;
        }

        fixed (char* pName1 = name1)
        fixed (char* pName2 = name2)
        {
            var p1 = pName1;
            var p2 = pName2;
            var i = 0;
            for (; i <= length - 4; i += 4)
            {
                if (*(long*)(p1 + i) != *(long*)(p2 + i))
                {
                    return false;
                }
            }

            for (; i < length; i++)
            {
                if (p1[i] != p2[i])
                {
                    return false;
                }
            }
        }

        return true;
    }
}