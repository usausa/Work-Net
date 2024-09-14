namespace WorkLinqPairwise;

using System.Diagnostics;

internal class Program
{
    public static void Main()
    {
        var array = new[] { 1, 2, 3, 4 };
        foreach (var value in array.Pairwise())
        {
            Debug.WriteLine($"({value.Item1}, {value.Item2})");
        }
    }
}

public static class Extensions
{
    public static IEnumerable<(T, T)> Pairwise<T>(this IEnumerable<T> source)
    {
        using var en = source.GetEnumerator();
        if (!en.MoveNext())
        {
            yield break;
        }

        var previous = en.Current;

        while (en.MoveNext())
        {
            yield return (previous, en.Current);
            previous = en.Current;
        }
    }
}
