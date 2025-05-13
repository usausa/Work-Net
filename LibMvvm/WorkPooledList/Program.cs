namespace WorkPooledList;

using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
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
        AddDiagnoser(MemoryDiagnoser.Default, new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(maxDepth: 3, printSource: true, printInstructionAddresses: true, exportDiff: true)));
        AddJob(Job.MediumRun);
        //AddJob(Job.ShortRun);
    }
}

[Config(typeof(BenchmarkConfig))]
public class Benchmark
{
    private const int N = 1000;

    private string[] values = default!;

    [Params(0, 1, 8, 16, 32)]
    public int Size { get; set; }

    [IterationSetup]
    public void Setup()
    {
        values = Enumerable.Range(0, Size).Select(x => x.ToString()).ToArray();
    }

    [Benchmark]
    public void AddClearList()
    {
        // ReSharper disable once CollectionNeverQueried.Local
        var list = new List<string>();
        for (var i = 0; i < N; i++)
        {
            foreach (var value in values)
            {
                list.Add(value);
            }
            list.Clear();
        }
    }

    [Benchmark]
    public void AddClearPooledList()
    {
        using var list = new PooledList<string>();
        for (var i = 0; i < N; i++)
        {
            foreach (var value in values)
            {
                list.Add(value);
            }
            list.Clear();
        }
    }

    [Benchmark]
    public void AddClearPooledList2()
    {
        using var list = new PooledList<string>(32);
        for (var i = 0; i < N; i++)
        {
            foreach (var value in values)
            {
                list.Add(value);
            }
            list.Clear();
        }
    }

    [Benchmark]
    public void AddClearPooledListSelfDispose()
    {
        var list = new PooledList<string>();
        for (var i = 0; i < N; i++)
        {
            foreach (var value in values)
            {
                list.Add(value);
            }
            list.Clear();
        }
        list.Dispose();
    }

    [Benchmark]
    public void AddClearPooledListSelfDispose2()
    {
        var list = new PooledList<string>(32);
        for (var i = 0; i < N; i++)
        {
            foreach (var value in values)
            {
                list.Add(value);
            }
            list.Clear();
        }
        list.Dispose();
    }

    [Benchmark]
    public void LifecycleList()
    {
        for (var i = 0; i < N; i++)
        {
            // ReSharper disable once CollectionNeverQueried.Local
            var list = new List<string>();
            foreach (var value in values)
            {
                list.Add(value);
            }
        }
    }

    [Benchmark]
    public void LifecyclePooled()
    {
        for (var i = 0; i < N; i++)
        {
            var list = new PooledList<string>();
            foreach (var value in values)
            {
                list.Add(value);
            }
            list.Dispose();
        }
    }

    [Benchmark]
    public void IterateList()
    {
        var list = new List<string>();
        foreach (var value in values)
        {
            list.Add(value);
        }

        for (var i = 0; i < N; i++)
        {
            foreach (var _ in list)
            {
            }
        }
    }

    [Benchmark]
    public void IteratePooled()
    {
        var list = new PooledList<string>();
        foreach (var value in values)
        {
            list.Add(value);
        }

        for (var i = 0; i < N; i++)
        {
            foreach (var _ in list)
            {
            }
        }
        list.Dispose();
    }

    [Benchmark]
    public void IteratePooledFor()
    {
        var list = new PooledList<string>();
        foreach (var value in values)
        {
            list.Add(value);
        }

        for (var i = 0; i < N; i++)
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var j = 0; j < list.Count; j++)
            {
                _ = list[j];
            }
        }
        list.Dispose();
    }
}

public sealed class PooledList<T> : IReadOnlyList<T>, IDisposable
{
    private const int DefaultCapacity = 16;

    private static readonly T[] EmptyArray = [];

    private T[] items;
    private int size;

    // Read-only property describing how many elements are in the List.
    public int Count => size;

    public T this[int index] => items[index];

    public PooledList()
    {
        items = EmptyArray;
    }

    public PooledList(int capacity)
    {
        items = ArrayPool<T>.Shared.Rent(capacity);
    }

    public void Dispose()
    {
        if (items.Length > 0)
        {
            ArrayPool<T>.Shared.Return(items, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            items = EmptyArray;
        }
        size = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        var array = items;
        var length = size;
        if ((uint)length < (uint)array.Length)
        {
            size = length + 1;
            array[length] = item;
        }
        else
        {
            Grow();
            size = length + 1;
            items[length] = item;
        }
    }

    private void Grow()
    {
        var length = items.Length == 0 ? DefaultCapacity : items.Length * 2;
        var newItems = ArrayPool<T>.Shared.Rent(length);
        if (size > 0)
        {
            Array.Copy(items, newItems, size);
        }
        if (items.Length > 0)
        {
            ArrayPool<T>.Shared.Return(items, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
        items = newItems;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            var length = size;
            size = 0;
            if (length > 0)
            {
                Array.Clear(items, 0, length);
            }
        }
        else
        {
            size = 0;
        }
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

    public struct Enumerator : IEnumerator<T>
    {
        private readonly PooledList<T> list;
        private int index;
        private T? current;

        internal Enumerator(PooledList<T> list)
        {
            this.list = list;
            current = default;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            var localList = list;
            if ((uint)index < (uint)localList.size)
            {
                current = localList.items[index];
                index++;
                return true;
            }

            current = default;
            return false;
        }

        void IEnumerator.Reset()
        {
            index = 0;
            current = default;
        }

        public T Current => current!;

        object? IEnumerator.Current => current;
    }
}
