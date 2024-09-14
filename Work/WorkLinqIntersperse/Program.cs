namespace WorkLinqIntersperse;

using System.Diagnostics;

internal class Program
{
    public static void Main()
    {
        var array = new[] { "aaa", "bbb", "ccc" };
        foreach (var value in array.Intersperse(","))
        {
            Debug.WriteLine(value);
        }
    }
}

public static class Extensions
{
    public static IEnumerable<T> Intersperse<T>(this IEnumerable<T> source, T delimiter)
    {
        using var en = source.GetEnumerator();
        if (!en.MoveNext())
        {
            yield break;
        }

        yield return en.Current;

        while (en.MoveNext())
        {
            yield return delimiter;
            yield return en.Current;
        }
    }
}
