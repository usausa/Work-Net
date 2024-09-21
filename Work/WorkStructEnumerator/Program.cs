namespace WorkStructEnumerator;

using System.Diagnostics;

internal class Program
{
    public static void Main()
    {
        var str = "123\r\n234\naaa";
        foreach (var entry in new LineSplitEnumerator(str))
        {
            Debug.WriteLine(entry.Line.ToString());
        }
    }
}

public ref struct LineSplitEnumerator
{
    private ReadOnlySpan<char> str;

    public LineSplitEnumerator(ReadOnlySpan<char> str)
    {
        this.str = str;
        Current = default;
    }

    public LineSplitEnumerator GetEnumerator() => this;

    public bool MoveNext()
    {
        if (str.Length == 0)
            return false;

        var span = str;
        var index = span.IndexOfAny('\r', '\n');
        if (index == -1)
        {
            str = ReadOnlySpan<char>.Empty;
            Current = new LineSplitEntry(span, ReadOnlySpan<char>.Empty);
            return true;
        }

        if (index < span.Length - 1 && span[index] == '\r')
        {
            var next = span[index + 1];
            if (next == '\n')
            {
                Current = new LineSplitEntry(span[..index], span.Slice(index, 2));
                str = span[(index + 2)..];
                return true;
            }
        }

        Current = new LineSplitEntry(span[..index], span.Slice(index, 1));
        str = span[(index + 1)..];
        return true;
    }

    public LineSplitEntry Current { get; private set; }
}

public readonly ref struct LineSplitEntry
{
    public ReadOnlySpan<char> Line { get; }

    public ReadOnlySpan<char> Separator { get; }

    public LineSplitEntry(ReadOnlySpan<char> line, ReadOnlySpan<char> separator)
    {
        Line = line;
        Separator = separator;
    }

    public void Deconstruct(out ReadOnlySpan<char> line, out ReadOnlySpan<char> separator)
    {
        line = Line;
        separator = Separator;
    }

    public static implicit operator ReadOnlySpan<char>(LineSplitEntry entry) => entry.Line;
}
