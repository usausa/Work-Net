using WorkParser.SqlAst;

namespace WorkParser.SqlParsing;

public readonly record struct SqlToken(SqlTokenKind Kind, SqlTextSpan Span, string Text)
{
    public override string ToString() => $"{Kind} {Span} '{Text}'";

    public ReadOnlySpan<char> TextSpan => Text.AsSpan();
}
