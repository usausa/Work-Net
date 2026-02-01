using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace MyPgsql.Binary;

/// <summary>
/// PostgreSQLパラメータ (DbParameter実装) - バイナリプロトコル版
/// </summary>
public sealed class PgBinaryParameter : DbParameter
{
    private string _parameterName = "";
    private object? _value;

    public PgBinaryParameter() { }

    public PgBinaryParameter(string parameterName, DbType dbType)
    {
        _parameterName = parameterName;
        DbType = dbType;
    }

    public PgBinaryParameter(string parameterName, object? value)
    {
        _parameterName = parameterName;
        _value = value;
    }

    public override DbType DbType { get; set; } = DbType.String;
    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public override bool IsNullable { get; set; } = true;

    [AllowNull]
    public override string ParameterName
    {
        get => _parameterName;
        set => _parameterName = value ?? "";
    }

    public override int Size { get; set; }

    [AllowNull]
    public override string SourceColumn { get; set; } = "";

    public override bool SourceColumnNullMapping { get; set; }

    public override object? Value
    {
        get => _value;
        set => _value = value;
    }

    public override void ResetDbType()
    {
        DbType = DbType.String;
    }
}
