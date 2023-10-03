using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkContainerConvert
{
    public class StructData
    {
        public EnumerableStruct Enumerable { get; set; }
        public EnumerableList List { get; set; }
    }

    public static class StructConvert
    {
        public static long[] Call1(StructData data)
        {
            return ConvertEnumerable(data.Enumerable);
        }

        public static long[] Call2(StructData data)
        {
            return ConvertList(data.List);
        }

        public static long[] BoxText1(StructData data)
        {
            var result = new List<long>();
            for (var i = 0; i < data.List.Count; i++)
            {
                result.Add(data.List[i]);
            }
            return result.ToArray();
        }

        public static long[] BoxText1b(StructData data)
        {
            var list = data.List;
            var count = list.Count;
            var result = new List<long>(count);
            for (var i = 0; i < count; i++)
            {
                result.Add(list[i]);
            }
            return result.ToArray();
        }

        public static long[] BoxText2(StructData data)
        {
            var result = new List<long>();
            foreach (var v in data.Enumerable)
            {
                result.Add(v);
            }
            return result.ToArray();
        }

        public static long[] BoxText2b(StructData data)
        {
            var result = new List<long>();
            var list = data.Enumerable;
            foreach (var v in list)
            {
                result.Add(v);
            }
            return result.ToArray();
        }

        public static long[] ConvertEnumerable(IEnumerable<int> source) => Array.Empty<long>();

        public static long[] ConvertList(IEnumerable<int> source) => Array.Empty<long>();
    }

    public struct EnumerableStruct : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public readonly struct EnumerableList : IList<int>
    {
        public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(int item) => throw new NotImplementedException();
        public void Clear() => throw new NotImplementedException();
        public bool Contains(int item) => throw new NotImplementedException();
        public void CopyTo(int[] array, int arrayIndex) => throw new NotImplementedException();
        public bool Remove(int item) => throw new NotImplementedException();
        public int Count  => throw new NotImplementedException();
        public bool IsReadOnly  => throw new NotImplementedException();
        public int IndexOf(int item) => throw new NotImplementedException();
        public void Insert(int index, int item) => throw new NotImplementedException();
        public void RemoveAt(int index) => throw new NotImplementedException();
        public int this[int index]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}
