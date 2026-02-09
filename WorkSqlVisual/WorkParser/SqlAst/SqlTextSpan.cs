namespace WorkParser.SqlAst;

public readonly record struct SqlTextSpan(int Start, int Length)
{
    public int End => Start + Length;

    public override string ToString() => $"[{Start}..{End})";
}
