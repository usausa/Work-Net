using WorkParser.SqlAst;

namespace WorkParser.SqlParsing;

public sealed record SqlParseResult(string Sql, SqlNode Root)
{
    public IEnumerable<SqlNode> Nodes => Root.DescendantsAndSelf();

    public IEnumerable<SqlNode> Comments => Nodes.Where(n => n.Kind == SqlNodeKind.Comment);
}
