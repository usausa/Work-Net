using WorkParser.SqlParsing;
using Xunit;

namespace WorkParser.Tests;

public sealed class SqlLexerTests
{
    [Fact]
    public void Lex_Collects_Line_And_Block_Comments()
    {
        var sql = "-- a\nSELECT 1 /*b*/";
        var tokens = SqlParserFacade.Lex(sql, includeWhitespace: true).ToList();

        Assert.Contains(tokens, t => t.Kind == SqlTokenKind.CommentLine && t.Text.StartsWith("--"));
        Assert.Contains(tokens, t => t.Kind == SqlTokenKind.CommentBlock && t.Text.StartsWith("/*"));
    }

    [Fact]
    public void Lex_Handles_Bracket_Quoted_Identifiers()
    {
        var sql = "SELECT [a b] FROM [t-1]";
        var tokens = SqlParserFacade.Lex(sql).ToList();

        Assert.Contains(tokens, t => t.Kind == SqlTokenKind.Identifier && t.Text == "[a b]");
        Assert.Contains(tokens, t => t.Kind == SqlTokenKind.Identifier && t.Text == "[t-1]");
    }

    [Fact]
    public void Lex_Handles_Escaped_Single_Quote_In_String()
    {
        var sql = "SELECT 'a''b'";
        var tokens = SqlParserFacade.Lex(sql).ToList();

        Assert.Contains(tokens, t => t.Kind == SqlTokenKind.String && t.Text == "'a''b'");
    }
}
