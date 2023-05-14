
using System.Diagnostics;
using Microsoft.SqlServer.TransactSql.ScriptDom;

var parser = new TSql160Parser(true, SqlEngineType.All);

IList<ParseError> errors = new List<ParseError>();
var reader = new StringReader(File.ReadAllText("test.sql"));
var tree = parser.Parse(reader, out errors);
if (errors.Count > 0)
{
    foreach (var error in errors)
    {
        Debug.WriteLine(error.Message);
    }
}
else
{
    foreach (var token in tree.ScriptTokenStream)
    {
        Debug.WriteLine("--");
        Debug.WriteLine($"Type: {token.TokenType}");
        Debug.WriteLine($"Text: {token.Text}");
        Debug.WriteLine($"Position: {token.Line}, {token.Column}, {token.Offset}");
        Debug.WriteLine($"Text: {token.IsKeyword()}");
    }
}

public class Visitor : TSqlFragmentVisitor
{
    public override void Visit(TSqlFragment fragment)
    {
        base.Visit(fragment);
    }

    public override void Visit(StatementList node)
    {
        base.Visit(node);
    }

    public override void Visit(ExecuteStatement node)
    {
        base.Visit(node);
    }

    public override void Visit(ExecuteOption node)
    {
        base.Visit(node);
    }

    public override void Visit(ResultSetsExecuteOption node)
    {
        base.Visit(node);
    }

    public override void Visit(ResultSetDefinition node)
    {
        base.Visit(node);
    }

    public override void Visit(InlineResultSetDefinition node)
    {
        base.Visit(node);
    }

    public override void Visit(ResultColumnDefinition node)
    {
        base.Visit(node);
    }

    public override void Visit(SchemaObjectResultSetDefinition node)
    {
        base.Visit(node);
    }

    public override void Visit(ExecuteSpecification node)
    {
        base.Visit(node);
    }

    public override void Visit(ExecuteContext node)
    {
        base.Visit(node);
    }

    public override void Visit(ExecuteParameter node)
    {
        base.Visit(node);
    }

    public override void Visit(ExecutableEntity node)
    {
        base.Visit(node);
    }

    public override void Visit(ProcedureReferenceName node)
    {
        base.Visit(node);
    }

    public override void Visit(ExecutableProcedureReference node)
    {
        base.Visit(node);
    }

    public override void Visit(ExecutableStringList node)
    {
        base.Visit(node);
    }

    public override void Visit(AdHocDataSource node)
    {
        base.Visit(node);
    }

    public override void Visit(ViewOption node)
    {
        base.Visit(node);
    }

    public override void Visit(AlterViewStatement node)
    {
        base.Visit(node);
    }

    public override void Visit(CreateViewStatement node)
    {
        base.Visit(node);
    }

    public override void Visit(CreateOrAlterViewStatement node)
    {
        base.Visit(node);
    }

    public override void Visit(ViewStatementBody node)
    {
        base.Visit(node);
    }

    public override void Visit(ViewForAppendOption node)
    {
        base.Visit(node);
    }

    public override void Visit(ViewDistributionOption node)
    {
        base.Visit(node);
    }

    public override void Visit(ViewDistributionPolicy node)
    {
        base.Visit(node);
    }

    public override void Visit(ViewRoundRobinDistributionPolicy node)
    {
        base.Visit(node);
    }

    public override void Visit(ViewHashDistributionPolicy node)
    {
        base.Visit(node);
    }

    public override void Visit(TriggerObject node)
    {
        base.Visit(node);
    }

    public override void Visit(TriggerOption node)
    {
        base.Visit(node);
    }

    public override void Visit(ExecuteAsTriggerOption node)
    {
        base.Visit(node);
    }

    public override void Visit(TriggerAction node)
    {
        base.Visit(node);
    }

    public override void Visit(AlterTriggerStatement node)
    {
        base.Visit(node);
    }

    public override void Visit(CreateTriggerStatement node)
    {
        base.Visit(node);
    }

    public override void Visit(CreateOrAlterTriggerStatement node)
    {
        base.Visit(node);
    }

    public override void Visit(TriggerStatementBody node)
    {
        base.Visit(node);
    }

    public override void Visit(Identifier node)
    {
        base.Visit(node);
    }
}