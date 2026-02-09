using WorkParser.SqlAst;

namespace WorkParser.SqlParsing;

public sealed class SqlParser
{
    private readonly string _sql;
    private readonly List<SqlToken> _tokens;
    private int _i;

    public SqlParser(string sql)
    {
        _sql = sql ?? throw new ArgumentNullException(nameof(sql));
        _tokens = new SqlLexer(_sql).Lex(includeWhitespace: true).ToList();
        _i = 0;
    }

    public SqlNode Parse()
    {
        var root = new SqlNode(SqlNodeKind.Statement, new SqlTextSpan(0, _sql.Length));

        while (!IsEof())
        {
            var t = Peek();

            if (IsComment(t))
            {
                root.Add(new SqlNode(SqlNodeKind.Comment, t.Span, t.Text));
                Advance();
                continue;
            }

            if (t.Kind == SqlTokenKind.Whitespace)
            {
                Advance();
                continue;
            }

            if (IsKeyword(t, "WITH"))
            {
                root.Add(ParseWith());
                continue;
            }

            if (IsKeyword(t, "SELECT"))
            {
                root.Add(ParseSelect());
                continue;
            }

            // default: keep token node so we don't lose information
            root.Add(new SqlNode(SqlNodeKind.Token, t.Span, t.Text));
            Advance();
        }

        return root;
    }

    private SqlNode ParseWith()
    {
        var start = Peek().Span.Start;
        var withNode = new SqlNode(SqlNodeKind.With, new SqlTextSpan(start, 0));

        withNode.Add(TakeTokenOrCommentNode());
        SkipTrivia(withNode);

        // optionally: RECURSIVE (for dialects)
        if (IsKeyword(PeekNonTrivia(), "RECURSIVE"))
        {
            ConsumeNonTrivia(withNode);
            SkipTrivia(withNode);
        }

        while (!IsEof())
        {
            SkipTrivia(withNode);

            // CTE name
            if (!IsIdentifierLike(PeekNonTrivia()))
                break;

            var cteStart = PeekNonTrivia().Span.Start;
            var cte = new SqlNode(SqlNodeKind.Cte, new SqlTextSpan(cteStart, 0));

            ConsumeNonTrivia(cte); // name
            SkipTrivia(cte);

            // optional column list: (a,b)
            if (MatchNonTrivia("("))
            {
                ConsumeNonTrivia(cte);
                int depth = 1;
                while (!IsEof() && depth > 0)
                {
                    var tok = Peek();
                    if (IsComment(tok))
                    {
                        cte.Add(new SqlNode(SqlNodeKind.Comment, tok.Span, tok.Text));
                        Advance();
                        continue;
                    }
                    if (tok.Kind == SqlTokenKind.Symbol)
                    {
                        if (tok.Text == "(") depth++;
                        else if (tok.Text == ")") depth--;
                    }
                    cte.Add(new SqlNode(SqlNodeKind.Token, tok.Span, tok.Text));
                    Advance();
                }
                SkipTrivia(cte);
            }

            // AS
            if (IsKeyword(PeekNonTrivia(), "AS"))
            {
                ConsumeNonTrivia(cte);
                SkipTrivia(cte);
            }

            // ( <select/statement> )
            if (MatchNonTrivia("("))
            {
                ConsumeNonTrivia(cte);
                int depth = 1;
                while (!IsEof() && depth > 0)
                {
                    var tok = Peek();
                    if (IsComment(tok))
                    {
                        cte.Add(new SqlNode(SqlNodeKind.Comment, tok.Span, tok.Text));
                        Advance();
                        continue;
                    }
                    if (tok.Kind == SqlTokenKind.Symbol)
                    {
                        if (tok.Text == "(") depth++;
                        else if (tok.Text == ")") depth--;
                    }
                    cte.Add(new SqlNode(SqlNodeKind.Token, tok.Span, tok.Text));
                    Advance();
                }
            }

            cte = SetSpanToChildren(cte);
            withNode.Add(cte);

            SkipTrivia(withNode);
            if (MatchNonTrivia(","))
            {
                ConsumeNonTrivia(withNode);
                continue;
            }

            break;
        }

        // consume following SELECT if present as a child node, so WITH tree is connected
        SkipTrivia(withNode);
        if (IsKeyword(PeekNonTrivia(), "SELECT"))
        {
            withNode.Add(ParseSelect());
        }

        return SetSpanToChildren(withNode);
    }

    private SqlNode ParseSelect()
    {
        var start = PeekNonTrivia().Span.Start;
        var select = new SqlNode(SqlNodeKind.Select, new SqlTextSpan(start, 0));

        // consume until statement terminator ';' at depth 0
        int parenDepth = 0;
        while (!IsEof())
        {
            var tok = Peek();

            if (IsComment(tok))
            {
                select.Add(new SqlNode(SqlNodeKind.Comment, tok.Span, tok.Text));
                Advance();
                continue;
            }

            if (tok.Kind == SqlTokenKind.Symbol)
            {
                if (tok.Text == "(") parenDepth++;
                else if (tok.Text == ")" && parenDepth > 0) parenDepth--;
                else if (tok.Text == ";" && parenDepth == 0)
                {
                    select.Add(new SqlNode(SqlNodeKind.Token, tok.Span, tok.Text));
                    Advance();
                    break;
                }
            }

            if (tok.Kind != SqlTokenKind.Whitespace)
            {
                // Clause-level parsing: create a sub-node and collect tokens until next top-level clause
                if (IsKeyword(tok, "FROM"))
                {
                    select.Add(ParseClause(SqlNodeKind.From, kwStop: new[] { "WHERE", "GROUP", "HAVING", "ORDER", "WINDOW" }));
                    continue;
                }
                if (IsKeyword(tok, "WHERE"))
                {
                    select.Add(ParseClause(SqlNodeKind.Where, kwStop: new[] { "GROUP", "HAVING", "ORDER", "WINDOW" }));
                    continue;
                }
                if (IsKeyword(tok, "GROUP"))
                {
                    select.Add(ParseClause(SqlNodeKind.GroupBy, kwStop: new[] { "HAVING", "ORDER", "WINDOW" }));
                    continue;
                }
                if (IsKeyword(tok, "HAVING"))
                {
                    select.Add(ParseClause(SqlNodeKind.Having, kwStop: new[] { "ORDER", "WINDOW" }));
                    continue;
                }
                if (IsKeyword(tok, "ORDER"))
                {
                    select.Add(ParseClause(SqlNodeKind.OrderBy, kwStop: new[] { "WINDOW" }));
                    continue;
                }
                if (IsKeyword(tok, "WINDOW"))
                {
                    select.Add(ParseWindowClause());
                    continue;
                }

                if (IsKeyword(tok, "OVER"))
                {
                    select.Add(ParseOver());
                    continue;
                }
            }

            select.Add(new SqlNode(SqlNodeKind.Token, tok.Span, tok.Text));
            Advance();
        }

        return SetSpanToChildren(select);
    }

    private SqlNode ParseClause(SqlNodeKind kind, string[] kwStop)
    {
        var start = Peek().Span.Start;
        var node = new SqlNode(kind, new SqlTextSpan(start, 0));

        int parenDepth = 0;
        while (!IsEof())
        {
            var tok = Peek();

            if (IsComment(tok))
            {
                node.Add(new SqlNode(SqlNodeKind.Comment, tok.Span, tok.Text));
                Advance();
                continue;
            }

            if (tok.Kind == SqlTokenKind.Symbol)
            {
                if (tok.Text == "(") parenDepth++;
                else if (tok.Text == ")" && parenDepth > 0) parenDepth--;
                else if (tok.Text == ";" && parenDepth == 0)
                    break;
            }

            if (parenDepth == 0 && tok.Kind == SqlTokenKind.Identifier)
            {
                for (int k = 0; k < kwStop.Length; k++)
                {
                    if (IsKeyword(tok, kwStop[k]))
                        return SetSpanToChildren(node);
                }

                // GROUP BY / ORDER BY (stop is GROUP/ORDER, but we must not stop on its own BY)
            }

            if (tok.Kind != SqlTokenKind.Whitespace && IsKeyword(tok, "OVER"))
            {
                node.Add(ParseOver());
                continue;
            }

            node.Add(new SqlNode(SqlNodeKind.Token, tok.Span, tok.Text));
            Advance();
        }

        return SetSpanToChildren(node);
    }

    private SqlNode ParseOver()
    {
        var start = Peek().Span.Start;
        var over = new SqlNode(SqlNodeKind.Over, new SqlTextSpan(start, 0));
        over.Add(TakeTokenOrCommentNode());

        SkipTrivia(over);

        if (MatchNonTrivia("("))
        {
            over.Add(ConsumeNonTriviaAsToken());

            int depth = 1;
            while (!IsEof() && depth > 0)
            {
                var tok = Peek();
                if (IsComment(tok))
                {
                    over.Add(new SqlNode(SqlNodeKind.Comment, tok.Span, tok.Text));
                    Advance();
                    continue;
                }

                if (tok.Kind == SqlTokenKind.Symbol)
                {
                    if (tok.Text == "(") depth++;
                    else if (tok.Text == ")") depth--;
                }

                if (tok.Kind != SqlTokenKind.Whitespace && IsKeyword(tok, "PARTITION"))
                {
                    over.Add(ParsePartitionBy());
                    continue;
                }

                over.Add(new SqlNode(SqlNodeKind.Token, tok.Span, tok.Text));
                Advance();
            }
        }

        return SetSpanToChildren(over);
    }

    private SqlNode ParsePartitionBy()
    {
        var start = Peek().Span.Start;
        var node = new SqlNode(SqlNodeKind.PartitionBy, new SqlTextSpan(start, 0));

        // PARTITION BY
        node.Add(new SqlNode(SqlNodeKind.Token, Peek().Span, Peek().Text));
        Advance();
        SkipTrivia(node);

        if (IsKeyword(PeekNonTrivia(), "BY"))
            ConsumeNonTrivia(node);

        // then keep tokens until we hit ORDER/ROWS/RANGE/GROUPS/)
        while (!IsEof())
        {
            var tok = Peek();
            if (IsComment(tok))
            {
                node.Add(new SqlNode(SqlNodeKind.Comment, tok.Span, tok.Text));
                Advance();
                continue;
            }

            if (tok.Kind != SqlTokenKind.Whitespace && tok.Kind == SqlTokenKind.Identifier)
            {
                if (IsKeyword(tok, "ORDER") || IsKeyword(tok, "ROWS") || IsKeyword(tok, "RANGE") || IsKeyword(tok, "GROUPS"))
                    break;
            }

            if (tok.Kind == SqlTokenKind.Symbol && tok.Text == ")")
                break;

            node.Add(new SqlNode(SqlNodeKind.Token, tok.Span, tok.Text));
            Advance();
        }

        return SetSpanToChildren(node);
    }

    private SqlNode ParseWindowClause()
    {
        var start = Peek().Span.Start;
        var node = new SqlNode(SqlNodeKind.Window, new SqlTextSpan(start, 0));

        // WINDOW <name> AS ( ... ), ...
        node.Add(new SqlNode(SqlNodeKind.Token, Peek().Span, Peek().Text));
        Advance();

        while (!IsEof())
        {
            var tok = Peek();
            if (IsComment(tok))
            {
                node.Add(new SqlNode(SqlNodeKind.Comment, tok.Span, tok.Text));
                Advance();
                continue;
            }

            node.Add(new SqlNode(SqlNodeKind.Token, tok.Span, tok.Text));
            Advance();

            if (tok.Kind == SqlTokenKind.Symbol && tok.Text == ";")
                break;

            // stop when another major clause begins at depth 0 (rough), but keep it simple
            if (tok.Kind == SqlTokenKind.Identifier && IsKeyword(tok, "FROM"))
                break;
        }

        return SetSpanToChildren(node);
    }

    private void SkipTrivia(SqlNode owner)
    {
        while (!IsEof())
        {
            var t = Peek();
            if (t.Kind == SqlTokenKind.Whitespace)
            {
                Advance();
                continue;
            }
            if (IsComment(t))
            {
                owner.Add(new SqlNode(SqlNodeKind.Comment, t.Span, t.Text));
                Advance();
                continue;
            }
            break;
        }
    }

    private bool IsComment(SqlToken t) => t.Kind is SqlTokenKind.CommentLine or SqlTokenKind.CommentBlock;

    private SqlNode TakeTokenOrCommentNode()
    {
        var t = Peek();
        Advance();
        return IsComment(t)
            ? new SqlNode(SqlNodeKind.Comment, t.Span, t.Text)
            : new SqlNode(SqlNodeKind.Token, t.Span, t.Text);
    }

    private SqlNode ConsumeNonTriviaAsToken()
    {
        SkipTrivia(new SqlNode(SqlNodeKind.Unknown, new SqlTextSpan(0, 0)));
        var t = Peek();
        Advance();
        return new SqlNode(SqlNodeKind.Token, t.Span, t.Text);
    }

    private void ConsumeNonTrivia(SqlNode owner)
    {
        SkipTrivia(owner);
        var t = Peek();
        owner.Add(new SqlNode(SqlNodeKind.Token, t.Span, t.Text));
        Advance();
    }

    private bool MatchNonTrivia(string symbol)
    {
        var t = PeekNonTrivia();
        return t.Kind == SqlTokenKind.Symbol && t.Text == symbol;
    }

    private SqlToken PeekNonTrivia()
    {
        var j = _i;
        while (j < _tokens.Count)
        {
            var t = _tokens[j];
            if (t.Kind == SqlTokenKind.Whitespace)
            {
                j++;
                continue;
            }
            if (IsComment(t))
            {
                j++;
                continue;
            }
            return t;
        }
        return _tokens[^1];
    }

    private bool IsIdentifierLike(SqlToken t)
        => t.Kind == SqlTokenKind.Identifier || (t.Kind == SqlTokenKind.Symbol && t.Text == "[") || t.Kind == SqlTokenKind.String;

    private bool IsKeyword(SqlToken t, string kw)
        => t.Kind == SqlTokenKind.Identifier && t.TextSpan.Equals(kw.AsSpan(), StringComparison.OrdinalIgnoreCase);

    private SqlToken Peek() => _tokens[_i];

    private void Advance()
    {
        if (_i < _tokens.Count - 1)
            _i++;
    }

    private bool IsEof() => Peek().Kind == SqlTokenKind.EndOfFile;

    private static SqlNode SetSpanToChildren(SqlNode node)
    {
        if (node.Children.Count == 0)
            return node;

        var start = node.Children.Min(c => c.Span.Start);
        var end = node.Children.Max(c => c.Span.End);
        return new SqlNode(node.Kind, new SqlTextSpan(start, end - start), node.Text)
            .WithChildrenFrom(node);
    }
}

internal static class SqlNodeExtensions
{
    public static WorkParser.SqlAst.SqlNode WithChildrenFrom(this WorkParser.SqlAst.SqlNode newNode, WorkParser.SqlAst.SqlNode oldNode)
    {
        foreach (var c in oldNode.Children)
            newNode.Add(c);
        return newNode;
    }
}
