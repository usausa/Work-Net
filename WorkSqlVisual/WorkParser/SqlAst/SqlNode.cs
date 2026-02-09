using System.Collections;

namespace WorkParser.SqlAst;

public sealed class SqlNode : IEnumerable<SqlNode>
{
    private readonly List<SqlNode> _children = new();

    public SqlNodeKind Kind { get; }
    public SqlTextSpan Span { get; }
    public string? Text { get; }

    public IReadOnlyList<SqlNode> Children => _children;

    public SqlNode(SqlNodeKind kind, SqlTextSpan span, string? text = null)
    {
        Kind = kind;
        Span = span;
        Text = text;
    }

    public SqlNode Add(SqlNode child)
    {
        _children.Add(child);
        return this;
    }

    public IEnumerator<SqlNode> GetEnumerator() => _children.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerable<SqlNode> DescendantsAndSelf()
    {
        yield return this;
        foreach (var c in _children)
        {
            foreach (var d in c.DescendantsAndSelf())
                yield return d;
        }
    }
}
