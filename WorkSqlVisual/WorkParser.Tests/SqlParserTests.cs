using WorkParser.SqlAst;
using WorkParser.SqlParsing;
using Xunit;

namespace WorkParser.Tests;

public sealed class SqlParserTests
{
    [Fact]
    public void Parse_Keeps_Comments_As_Nodes()
    {
        var sql = "/*head*/ SELECT -- mid\n 1";
        var result = SqlParserFacade.Parse(sql);

        var comments = result.Comments.ToList();
        Assert.Equal(2, comments.Count);
        Assert.All(comments, c => Assert.Equal(SqlNodeKind.Comment, c.Kind));
    }

    [Fact]
    public void Parse_With_Cte_Select_Is_Parsed()
    {
        var sql = "WITH cte AS (SELECT 1 AS x) SELECT x FROM cte";
        var result = SqlParserFacade.Parse(sql);

        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.With);
        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.Cte);
        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.Select);
    }

    [Fact]
    public void Parse_Multiple_Ctes_And_Comments()
    {
        var sql = @"WITH
/*c1*/ c1 AS (SELECT 1),
-- c2
c2(a) AS (SELECT 2)
SELECT * FROM c1";

        var result = SqlParserFacade.Parse(sql);

        Assert.Equal(2, result.Nodes.Count(n => n.Kind == SqlNodeKind.Cte));
        Assert.True(result.Comments.Any());
    }

    [Fact]
    public void Parse_Window_Over_PartitionBy_Is_Parsed()
    {
        var sql = "SELECT SUM(x) OVER (PARTITION BY y ORDER BY z) FROM t";
        var result = SqlParserFacade.Parse(sql);

        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.Over);
        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.PartitionBy);
    }

    [Fact]
    public void Parse_Window_Clause_Is_Parsed()
    {
        var sql = "SELECT x FROM t WINDOW w AS (PARTITION BY y)";
        var result = SqlParserFacade.Parse(sql);

        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.Window);
    }

    [Fact]
    public void Parse_Does_Not_Break_On_Comment_Like_Text_In_String()
    {
        var sql = "SELECT '--not a comment' AS x, '/*no*/' AS y";
        var result = SqlParserFacade.Parse(sql);

        Assert.Empty(result.Comments);
        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.Select);
    }

    [Fact]
    public void Parse_Select_Clause_Nodes_Are_Created()
    {
        var sql = "SELECT * FROM t WHERE a = 1 GROUP BY b HAVING COUNT(*) > 1 ORDER BY b";
        var result = SqlParserFacade.Parse(sql);

        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.From);
        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.Where);
        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.GroupBy);
        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.Having);
        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.OrderBy);
    }

    [Fact]
    public void Parse_Does_Not_Split_Clauses_Inside_Parentheses()
    {
        var sql = "SELECT (SELECT 1 FROM t2 WHERE x = 1) AS v FROM t1 WHERE y = 2";
        var result = SqlParserFacade.Parse(sql);

        // top-level FROM/WHERE should exist
        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.From);
        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.Where);
    }

    [Fact]
    public void Parse_With_Two_Ctes_With_Column_List_And_Recursive_Keyword()
    {
        var sql = "WITH RECURSIVE c1(a,b) AS (SELECT 1,2), c2 AS (SELECT 3) SELECT * FROM c1";
        var result = SqlParserFacade.Parse(sql);

        Assert.Equal(2, result.Nodes.Count(n => n.Kind == SqlNodeKind.Cte));
        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.With);
        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.Select);
    }

    [Fact]
    public void Parse_Over_With_Nested_Parentheses_And_Comments()
    {
        var sql = "SELECT SUM(x) OVER (PARTITION BY (y + 1) /*p*/ ORDER BY z) FROM t";
        var result = SqlParserFacade.Parse(sql);

        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.Over);
        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.PartitionBy);
        Assert.True(result.Comments.Any());
    }

    [Fact]
    public void Parse_Window_Clause_With_Multiple_Definitions()
    {
        var sql = "SELECT x FROM t WINDOW w1 AS (PARTITION BY y), w2 AS (ORDER BY z)";
        var result = SqlParserFacade.Parse(sql);

        Assert.Contains(result.Nodes, n => n.Kind == SqlNodeKind.Window);
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

    [Fact]
    public void Parse_Semicolon_Terminates_Select_Node()
    {
        var sql = "SELECT 1; SELECT 2";
        var result = SqlParserFacade.Parse(sql);

        Assert.True(result.Nodes.Count(n => n.Kind == SqlNodeKind.Select) >= 2);
    }
}
