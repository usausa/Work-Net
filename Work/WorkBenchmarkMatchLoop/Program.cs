namespace WorkBenchmarkMatchLoop;

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
    //[Params(2, 4, 8, 16)]
    [Params(2, 16, 32)]
    public int Size { get; set; }

    private Matcher matcher = default!;

    private ClassEntry[] classEntries = default!;

    private StructEntry[] structEntries = default!;

    [GlobalSetup]
    public void Setup()
    {
        matcher = new Matcher(Size);
        classEntries = Enumerable.Range(0, Size).Select(x => new ClassEntry(typeof(object), $"Field{x}")).ToArray();
        structEntries = Enumerable.Range(0, Size).Select(x => new StructEntry(typeof(object), $"Field{x}")).ToArray();
    }

    [Benchmark]
    public bool FindClassByLoop() => matcher.FindClassByLoop(classEntries);

    [Benchmark]
    public bool FindStructByLoop() => matcher.FindStructByLoop(structEntries);

    [Benchmark]
    public bool FindStructByRefLoop() => matcher.FindStructByRefLoop(structEntries);

    [Benchmark]
    public bool FindStructBySpanRefLoop() => matcher.FindStructBySpanRefLoop(structEntries);

    [Benchmark]
    public bool FindStructBySpanRefAdd() => matcher.FindStructBySpanRefAdd(structEntries);

    [Benchmark]
    public bool FindStructBySpanRefAdd2() => matcher.FindStructBySpanRefAdd2(structEntries);

    [Benchmark]
    public bool FindStructBySpanRefWhile() => matcher.FindStructBySpanRefWhile(structEntries);

    [Benchmark]
    public bool FindStructBySpanRefWhile2() => matcher.FindStructBySpanRefWhile2(structEntries);
}

public sealed class Matcher
{
    private readonly ClassEntry[] classEntries;

    private readonly StructEntry[] structEntries;

    public Matcher(int size)
    {
        classEntries = Enumerable.Range(0, size).Select(x => new ClassEntry(typeof(object), $"Field{x}")).ToArray();
        structEntries = Enumerable.Range(0, size).Select(x => new StructEntry(typeof(object), $"Field{x}")).ToArray();
    }

    public bool FindClassByLoop(ClassEntry[] otherEntry)
    {
        var array = classEntries;

        if (array.Length != otherEntry.Length)
        {
            return false;
        }

        for (var i = 0; i < array.Length; i++)
        {
            var entry = array[i];
            var other = otherEntry[i];
            if ((entry.Type != other.Type) || (entry.Name != other.Name))
            {
                return false;
            }
        }

        return true;
    }

    public bool FindStructByLoop(StructEntry[] otherEntry)
    {
        var array = structEntries;

        if (array.Length != otherEntry.Length)
        {
            return false;
        }

        for (var i = 0; i < array.Length; i++)
        {
            var entry = array[i];
            var other = otherEntry[i];
            if ((entry.Type != other.Type) || (entry.Name != other.Name))
            {
                return false;
            }
        }

        return true;
    }

    public bool FindStructByRefLoop(StructEntry[] otherEntry)
    {
        var array = structEntries;

        if (array.Length != otherEntry.Length)
        {
            return false;
        }

        for (var i = 0; i < array.Length; i++)
        {
            ref var entry = ref array[i];
            ref var other = ref otherEntry[i];
            if ((entry.Type != other.Type) || (entry.Name != other.Name))
            {
                return false;
            }
        }

        return true;
    }

    public bool FindStructBySpanRefLoop(StructEntry[] otherEntry)
    {
        var span = structEntries.AsSpan();
        var otherSpan = otherEntry.AsSpan();

        if (span.Length != otherSpan.Length)
        {
            return false;
        }

        for (var i = 0; i < span.Length; i++)
        {
            ref var entry = ref span[i];
            ref var other = ref otherSpan[i];
            if ((entry.Type != other.Type) || (entry.Name != other.Name))
            {
                return false;
            }
        }

        return true;
    }

    public bool FindStructBySpanRefAdd(StructEntry[] otherEntry)
    {
        var span1 = structEntries.AsSpan();
        var span2 = otherEntry.AsSpan();

        var length = span1.Length;
        if (length != span2.Length)
        {
            return false;
        }

        ref var entry = ref MemoryMarshal.GetReference(span1);
        ref var other = ref MemoryMarshal.GetReference(span2);
        for (var i = 0; i < length; i++)
        {
            if ((entry.Type != other.Type) || (entry.Name != other.Name))
            {
                return false;
            }

            entry = ref Unsafe.Add(ref entry, 1);
            other = ref Unsafe.Add(ref other, 1);
        }

        return true;
    }

    public bool FindStructBySpanRefAdd2(StructEntry[] otherEntry)
    {
        var span1 = structEntries.AsSpan();
        var span2 = otherEntry.AsSpan();

        var length = span1.Length;
        if (length != span2.Length)
        {
            return false;
        }

        ref var entry = ref MemoryMarshal.GetReference(span1);
        ref var other = ref MemoryMarshal.GetReference(span2);
        do
        {
            if ((entry.Type != other.Type) || (entry.Name != other.Name))
            {
                return false;
            }

            entry = ref Unsafe.Add(ref entry, 1);
            other = ref Unsafe.Add(ref other, 1);

            length--;
        } while (length != 0);

        return true;
    }

    public bool FindStructBySpanRefWhile(StructEntry[] otherEntry)
    {
        var span1 = structEntries.AsSpan();
        var span2 = otherEntry.AsSpan();

        var length = span1.Length;
        if (length != span2.Length)
        {
            return false;
        }

        ref var entry = ref MemoryMarshal.GetReference(span1);
        ref var end = ref Unsafe.Add(ref entry, length);
        ref var other = ref MemoryMarshal.GetReference(span2);
        do
        {
            if ((entry.Type != other.Type) || (entry.Name != other.Name))
            {
                return false;
            }

            entry = ref Unsafe.Add(ref entry, 1);
            other = ref Unsafe.Add(ref other, 1);
        }
        while (Unsafe.IsAddressLessThan(ref entry, ref end));

        return true;
    }

    public bool FindStructBySpanRefWhile2(StructEntry[] otherEntry)
    {
        var span1 = structEntries.AsSpan();
        var span2 = otherEntry.AsSpan();

        var length = span1.Length;
        if (length != span2.Length)
        {
            return false;
        }

        ref var entry = ref MemoryMarshal.GetReference(span1);
        ref var end = ref Unsafe.Add(ref entry, length);
        ref var other = ref MemoryMarshal.GetReference(span2);

        Compare:
        if ((entry.Type != other.Type) || (entry.Name != other.Name))
        {
            return false;
        }

        entry = ref Unsafe.Add(ref entry, 1);
        if (Unsafe.IsAddressLessThan(ref entry, ref end))
        {
            other = ref Unsafe.Add(ref other, 1);
            goto Compare;
        }

        return true;
    }
}

public sealed class ClassEntry
{
    public readonly Type Type;

    public readonly string Name;

    public ClassEntry(Type type, string name)
    {
        Type = type;
        Name = name;
    }
}

public struct StructEntry
{
    public readonly Type Type;

    public readonly string Name;

    public StructEntry(Type type, string name)
    {
        Type = type;
        Name = name;
    }
}
