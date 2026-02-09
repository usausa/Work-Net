using WorkParser.SqlAst;

namespace WorkParser.SqlParsing;

public sealed class SqlLexer
{
    private readonly string _text;
    private int _pos;

    public SqlLexer(string text)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _pos = 0;
    }

    public IEnumerable<SqlToken> Lex(bool includeWhitespace = false)
    {
        while (true)
        {
            var t = NextToken();
            if (t.Kind == SqlTokenKind.Whitespace && !includeWhitespace)
                continue;

            yield return t;

            if (t.Kind == SqlTokenKind.EndOfFile)
                yield break;
        }
    }

    private SqlToken NextToken()
    {
        if (_pos >= _text.Length)
            return new SqlToken(SqlTokenKind.EndOfFile, new SqlTextSpan(_pos, 0), string.Empty);

        var start = _pos;
        var ch = _text[_pos];

        if (char.IsWhiteSpace(ch))
        {
            _pos++;
            while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos]))
                _pos++;
            return Make(SqlTokenKind.Whitespace, start, _pos);
        }

        if (ch == '-' && Peek(1) == '-')
        {
            _pos += 2;
            while (_pos < _text.Length)
            {
                var c = _text[_pos];
                if (c == '\n' || c == '\r')
                    break;
                _pos++;
            }
            return Make(SqlTokenKind.CommentLine, start, _pos);
        }

        if (ch == '/' && Peek(1) == '*')
        {
            _pos += 2;
            while (_pos < _text.Length)
            {
                if (_text[_pos] == '*' && Peek(1) == '/')
                {
                    _pos += 2;
                    break;
                }
                _pos++;
            }
            return Make(SqlTokenKind.CommentBlock, start, _pos);
        }

        if (ch == '[')
        {
            _pos++;
            while (_pos < _text.Length)
            {
                var c = _text[_pos];
                if (c == ']')
                {
                    _pos++;
                    break;
                }
                _pos++;
            }
            return Make(SqlTokenKind.Identifier, start, _pos);
        }

        if (ch == 'N' && Peek(1) == '\'' || ch == '\'')
        {
            if (ch == 'N' && Peek(1) == '\'')
                _pos++; // consume N

            _pos++; // consume '
            while (_pos < _text.Length)
            {
                var c = _text[_pos];
                if (c == '\'')
                {
                    if (Peek(1) == '\'')
                    {
                        _pos += 2; // escaped ''
                        continue;
                    }
                    _pos++;
                    break;
                }
                _pos++;
            }
            return Make(SqlTokenKind.String, start, _pos);
        }

        if (char.IsDigit(ch))
        {
            _pos++;
            while (_pos < _text.Length)
            {
                var c = _text[_pos];
                if (char.IsDigit(c) || c == '.')
                {
                    _pos++;
                    continue;
                }
                break;
            }
            return Make(SqlTokenKind.Number, start, _pos);
        }

        if (IsIdentStart(ch))
        {
            _pos++;
            while (_pos < _text.Length && IsIdentPart(_text[_pos]))
                _pos++;
            return Make(SqlTokenKind.Identifier, start, _pos);
        }

        _pos++;
        return Make(SqlTokenKind.Symbol, start, _pos);
    }

    private char Peek(int offset)
    {
        var i = _pos + offset;
        return i >= 0 && i < _text.Length ? _text[i] : '\0';
    }

    private SqlToken Make(SqlTokenKind kind, int start, int endExclusive)
        => new(kind, new SqlTextSpan(start, endExclusive - start), _text.Substring(start, endExclusive - start));

    private static bool IsIdentStart(char c)
        => char.IsLetter(c) || c == '_' || c == '@' || c == '#';

    private static bool IsIdentPart(char c)
        => char.IsLetterOrDigit(c) || c == '_' || c == '$' || c == '@' || c == '#';
}
