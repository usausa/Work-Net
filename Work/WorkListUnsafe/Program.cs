namespace WorkListUnsafe;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static class Program
{
    public static void Main(string[] args)
    {
        var list = new List<string>
        {
            "111",
            "222",
            "333",
            "444"
        };

        list.InsertBlock(2, 3);

        Debug.WriteLine(list.Count);
        foreach (var value in list)
        {
            Debug.WriteLine(value);
        }
    }
}

public static class Helper
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_version")]
    private static extern ref int _version<T>(List<T> list);

    public static void InsertBlock<T>(this List<T> list, int index, int count)
    {
        CollectionsMarshal.SetCount(list, list.Count + count);
        var span = CollectionsMarshal.AsSpan(list);
        span[index..^count].CopyTo(span[(index + count)..]);
        span.Slice(index, count).Fill(default!);
        _version(list)++;
    }
}
