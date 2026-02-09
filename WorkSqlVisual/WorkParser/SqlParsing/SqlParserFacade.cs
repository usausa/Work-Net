using WorkParser.SqlAst;

namespace WorkParser.SqlParsing;

public static class SqlParserFacade
{
    public static SqlParseResult Parse(string sql)
    {
        var parser = new SqlParser(sql);
        var root = parser.Parse();
        return new SqlParseResult(sql, root);
    }

    public static IEnumerable<SqlToken> Lex(string sql, bool includeWhitespace = false)
        => new SqlLexer(sql).Lex(includeWhitespace);
}
