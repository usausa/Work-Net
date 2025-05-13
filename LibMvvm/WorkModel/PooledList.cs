namespace Smart.Mvvm.Internal;

using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

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
