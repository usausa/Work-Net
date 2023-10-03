namespace WorkEnumerableConverter
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class MyList<T> : IList<T>
    {
        private readonly List<T> list;

        public MyList(IEnumerable<T> source)
        {
            this.list = source.ToList();
        }

        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
        public void Add(T item) => list.Add(item);
        public void Clear() => list.Clear();
        public bool Contains(T item) => list.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);
        public bool Remove(T item) => list.Remove(item);
        public int Count => list.Count;
        public bool IsReadOnly => false;
        public int IndexOf(T item) => list.IndexOf(item);
        public void Insert(int index, T item) => list.Insert(index, item);
        public void RemoveAt(int index) => list.RemoveAt(index);
        public T this[int index]
        {
            get => list[index];
            set => list[index] = value;
        }
    }

    public readonly struct MyListStruct<T> : IList<T>
    {
        private readonly List<T> list;

        public MyListStruct(IEnumerable<T> source)
        {
            this.list = source.ToList();
        }

        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
        public void Add(T item) => list.Add(item);
        public void Clear() => list.Clear();
        public bool Contains(T item) => list.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);
        public bool Remove(T item) => list.Remove(item);
        public int Count => list.Count;
        public bool IsReadOnly => false;
        public int IndexOf(T item) => list.IndexOf(item);
        public void Insert(int index, T item) => list.Insert(index, item);
        public void RemoveAt(int index) => list.RemoveAt(index);
        public T this[int index]
        {
            get => list[index];
            set => list[index] = value;
        }
    }

    public class MyEnumerable<T> : IEnumerable<T>
    {
        private readonly List<T> list;

        public MyEnumerable(IEnumerable<T> source)
        {
            this.list = source.ToList();
        }

        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public readonly struct MyEnumerableStruct<T> : IEnumerable<T>
    {
        private readonly List<T> list;

        public MyEnumerableStruct(IEnumerable<T> source)
        {
            this.list = source.ToList();
        }

        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
