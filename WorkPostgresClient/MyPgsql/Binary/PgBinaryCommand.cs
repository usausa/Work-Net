using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace MyPgsql.Binary;

/// <summary>
/// PostgreSQLコマンド (DbCommand実装) - バイナリプロトコル版
/// パラメーターはサーバー側で展開（Extended Query Protocol）
/// </summary>
public sealed class PgBinaryCommand : DbCommand
{
    private PgBinaryConnection? _connection;
    private PgBinaryTransaction? _transaction;
    private string _commandText = "";
    private readonly PgBinaryParameterCollection _parameters = new();
    private CommandType _commandType = CommandType.Text;
    private int _commandTimeout = 30;

    public PgBinaryCommand() { }

    public PgBinaryCommand(string commandText)
    {
        _commandText = commandText;
    }

    public PgBinaryCommand(string commandText, PgBinaryConnection connection)
    {
        _commandText = commandText;
        _connection = connection;
    }

    [AllowNull]
    public override string CommandText
    {
        get => _commandText;
        set => _commandText = value ?? "";
    }

    public override int CommandTimeout
    {
        get => _commandTimeout;
        set => _commandTimeout = value;
    }

    public override CommandType CommandType
    {
        get => _commandType;
        set => _commandType = value;
    }

    public override bool DesignTimeVisible { get; set; }

    public override UpdateRowSource UpdatedRowSource { get; set; }

    public new PgBinaryConnection? Connection
    {
        get => _connection;
        set => _connection = value;
    }

    protected override DbConnection? DbConnection
    {
        get => _connection;
        set => _connection = value as PgBinaryConnection;
    }

    public new PgBinaryTransaction? Transaction
    {
        get => _transaction;
        set => _transaction = value;
    }

    protected override DbTransaction? DbTransaction
    {
        get => _transaction;
        set => _transaction = value as PgBinaryTransaction;
    }

    public new PgBinaryParameterCollection Parameters => _parameters;
    protected override DbParameterCollection DbParameterCollection => _parameters;

    public override void Cancel()
    {
    }

    public override int ExecuteNonQuery()
    {
        return ExecuteNonQueryAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        ValidateCommand();

        if (_parameters.Count == 0)
        {
            return await _connection!.Protocol.ExecuteNonQueryAsync(_commandText, cancellationToken);
        }
        else
        {
            // パラメーターをProtocolHandlerに渡してサーバー側で展開
            return await _connection!.Protocol.ExecuteNonQueryWithParametersAsync(
                _commandText,
                _parameters.GetParametersInternal(),
                cancellationToken);
        }
    }

    public override object? ExecuteScalar()
    {
        return ExecuteScalarAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        await using var reader = await ExecuteDbDataReaderAsync(CommandBehavior.Default, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return reader.IsDBNull(0) ? null : reader.GetValue(0);
        }
        return null;
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        return ExecuteDbDataReaderAsync(behavior, CancellationToken.None).GetAwaiter().GetResult();
    }

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        ValidateCommand();

        if (_parameters.Count == 0)
        {
            await _connection!.Protocol.SendExtendedQueryAsync(_commandText, cancellationToken);
        }
        else
        {
            // パラメーターをProtocolHandlerに渡してサーバー側で展開
            await _connection!.Protocol.SendExtendedQueryWithParametersAsync(
                _commandText,
                _parameters.GetParametersInternal(),
                cancellationToken);
        }

        return new PgBinaryDataReader(_connection!.Protocol, _connection, behavior, cancellationToken);
    }

    public override void Prepare()
    {
    }

    protected override DbParameter CreateDbParameter()
    {
        return new PgBinaryParameter();
    }

    private void ValidateCommand()
    {
        if (_connection == null)
            throw new InvalidOperationException("Connectionが設定されていません");

        if (_connection.State != ConnectionState.Open)
            throw new InvalidOperationException("接続が開かれていません");

        if (string.IsNullOrEmpty(_commandText))
            throw new InvalidOperationException("CommandTextが設定されていません");
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await Task.CompletedTask;
    }
}
