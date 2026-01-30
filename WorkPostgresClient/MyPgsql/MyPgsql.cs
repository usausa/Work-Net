using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace MyPgsql;

public sealed class PostgresException(string message) : Exception(message);

#region ADO.NET Implementation

/// <summary>
/// PostgreSQL接続文字列ビルダー
/// </summary>
public sealed class PgConnectionStringBuilder
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    public PgConnectionStringBuilder() { }

    public PgConnectionStringBuilder(string connectionString)
    {
        Parse(connectionString);
    }

    private void Parse(string connectionString)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0) continue;

            var key = part[..idx].Trim().ToLowerInvariant();
            var value = part[(idx + 1)..].Trim();

            switch (key)
            {
                case "host" or "server":
                    Host = value;
                    break;
                case "port":
                    Port = int.Parse(value);
                    break;
                case "database" or "db":
                    Database = value;
                    break;
                case "username" or "user" or "uid":
                    Username = value;
                    break;
                case "password" or "pwd":
                    Password = value;
                    break;
            }
        }
    }

    public override string ToString()
    {
        return $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password}";
    }
}

/// <summary>
/// PostgreSQL接続 (DbConnection実装)
/// </summary>
public sealed class PgConnection : DbConnection
{
    private PgConnectionStringBuilder _connectionStringBuilder = new();
    private string _connectionString = "";
    private ConnectionState _state = ConnectionState.Closed;
    private PgProtocolHandler? _protocol;
    private PgTransaction? _currentTransaction;

    public PgConnection() { }

    public PgConnection(string connectionString)
    {
        ConnectionString = connectionString;
    }

    [AllowNull]
    public override string ConnectionString
    {
        get => _connectionString;
        set
        {
            _connectionString = value ?? "";
            _connectionStringBuilder = new PgConnectionStringBuilder(_connectionString);
        }
    }

    public override string Database => _connectionStringBuilder.Database;
    public override string DataSource => $"{_connectionStringBuilder.Host}:{_connectionStringBuilder.Port}";
    public override string ServerVersion => "PostgreSQL";
    public override ConnectionState State => _state;

    internal PgProtocolHandler Protocol => _protocol ?? throw new InvalidOperationException("接続が開かれていません");
    internal PgTransaction? CurrentTransaction => _currentTransaction;

    public override void Open()
    {
        OpenAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task OpenAsync(CancellationToken cancellationToken)
    {
        if (_state == ConnectionState.Open)
            return;

        _state = ConnectionState.Connecting;
        try
        {
            _protocol = new PgProtocolHandler();
            await _protocol.ConnectAsync(
                _connectionStringBuilder.Host,
                _connectionStringBuilder.Port,
                _connectionStringBuilder.Database,
                _connectionStringBuilder.Username,
                _connectionStringBuilder.Password,
                cancellationToken);

            _state = ConnectionState.Open;
        }
        catch
        {
            _state = ConnectionState.Closed;
            _protocol?.Dispose();
            _protocol = null;
            throw;
        }
    }

    public override void Close()
    {
        CloseAsync().GetAwaiter().GetResult();
    }

    public override async Task CloseAsync()
    {
        if (_state == ConnectionState.Closed)
            return;

        if (_protocol != null)
        {
            await _protocol.DisposeAsync();
            _protocol = null;
        }

        _currentTransaction = null;
        _state = ConnectionState.Closed;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        return BeginTransactionAsync(isolationLevel, CancellationToken.None).GetAwaiter().GetResult();
    }

    public new async Task<PgTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
    }

    public async Task<PgTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        if (_state != ConnectionState.Open)
            throw new InvalidOperationException("接続が開かれていません");

        if (_currentTransaction != null)
            throw new InvalidOperationException("既にトランザクションが開始されています");

        var isolationLevelSql = isolationLevel switch
        {
            IsolationLevel.ReadUncommitted => "READ UNCOMMITTED",
            IsolationLevel.ReadCommitted => "READ COMMITTED",
            IsolationLevel.RepeatableRead => "REPEATABLE READ",
            IsolationLevel.Serializable => "SERIALIZABLE",
            _ => "READ COMMITTED"
        };

        await Protocol.ExecuteNonQueryAsync($"BEGIN ISOLATION LEVEL {isolationLevelSql}", cancellationToken);
        _currentTransaction = new PgTransaction(this, isolationLevel);
        return _currentTransaction;
    }

    internal void ClearTransaction()
    {
        _currentTransaction = null;
    }

    public new PgCommand CreateCommand()
    {
        return new PgCommand { Connection = this };
    }

    protected override DbCommand CreateDbCommand()
    {
        return CreateCommand();
    }

    public override void ChangeDatabase(string databaseName)
    {
        throw new NotSupportedException("データベースの変更はサポートされていません");
    }

    public override async ValueTask DisposeAsync()
    {
        await CloseAsync();
        GC.SuppressFinalize(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Close();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// PostgreSQLトランザクション (DbTransaction実装)
/// </summary>
public sealed class PgTransaction : DbTransaction
{
    private readonly PgConnection _connection;
    private readonly IsolationLevel _isolationLevel;
    private bool _completed;

    internal PgTransaction(PgConnection connection, IsolationLevel isolationLevel)
    {
        _connection = connection;
        _isolationLevel = isolationLevel;
    }

    public new PgConnection Connection => _connection;
    protected override DbConnection DbConnection => _connection;
    public override IsolationLevel IsolationLevel => _isolationLevel;

    public override void Commit()
    {
        CommitAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_completed)
            throw new InvalidOperationException("トランザクションは既に完了しています");

        await _connection.Protocol.ExecuteNonQueryAsync("COMMIT", cancellationToken);
        _completed = true;
        _connection.ClearTransaction();
    }

    public override void Rollback()
    {
        RollbackAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_completed)
            throw new InvalidOperationException("トランザクションは既に完了しています");

        await _connection.Protocol.ExecuteNonQueryAsync("ROLLBACK", cancellationToken);
        _completed = true;
        _connection.ClearTransaction();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_completed)
        {
            try
            {
                Rollback();
            }
            catch
            {
                // 無視
            }
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            try
            {
                await RollbackAsync();
            }
            catch
            {
                // 無視
            }
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// PostgreSQLコマンド (DbCommand実装)
/// </summary>
public sealed class PgCommand : DbCommand
{
    private PgConnection? _connection;
    private PgTransaction? _transaction;
    private string _commandText = "";
    private readonly PgParameterCollection _parameters = new();
    private CommandType _commandType = CommandType.Text;
    private int _commandTimeout = 30;

    public PgCommand() { }

    public PgCommand(string commandText)
    {
        _commandText = commandText;
    }

    public PgCommand(string commandText, PgConnection connection)
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

    public new PgConnection? Connection
    {
        get => _connection;
        set => _connection = value;
    }

    protected override DbConnection? DbConnection
    {
        get => _connection;
        set => _connection = value as PgConnection;
    }

    public new PgTransaction? Transaction
    {
        get => _transaction;
        set => _transaction = value;
    }

    protected override DbTransaction? DbTransaction
    {
        get => _transaction;
        set => _transaction = value as PgTransaction;
    }

    public new PgParameterCollection Parameters => _parameters;
    protected override DbParameterCollection DbParameterCollection => _parameters;

    public override void Cancel()
    {
        // 未実装
    }

    public override int ExecuteNonQuery()
    {
        return ExecuteNonQueryAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        ValidateCommand();
        var sql = BuildSql();
        return await _connection!.Protocol.ExecuteNonQueryAsync(sql, cancellationToken);
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
        var sql = BuildSql();
        var result = await _connection!.Protocol.ExecuteQueryAsync(sql, cancellationToken);
        return new PgDataReader(result, _connection, behavior);
    }

    public override void Prepare()
    {
        // Simple Queryプロトコルでは不要
    }

    protected override DbParameter CreateDbParameter()
    {
        return new PgParameter();
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

    private string BuildSql()
    {
        if (_parameters.Count == 0)
            return _commandText;

        var sql = _commandText;
        foreach (PgParameter param in _parameters)
        {
            var value = FormatParameterValue(param);
            sql = sql.Replace(param.ParameterName, value);
        }
        return sql;
    }

    private static string FormatParameterValue(PgParameter param)
    {
        if (param.Value == null || param.Value == DBNull.Value)
            return "NULL";

        return param.DbType switch
        {
            DbType.Int16 or DbType.Int32 or DbType.Int64 or
            DbType.UInt16 or DbType.UInt32 or DbType.UInt64 or
            DbType.Single or DbType.Double or DbType.Decimal
                => Convert.ToString(param.Value, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL",

            DbType.Boolean
                => (bool)param.Value ? "TRUE" : "FALSE",

            DbType.DateTime or DbType.DateTime2 or DbType.DateTimeOffset
                => $"'{((DateTime)param.Value):yyyy-MM-dd HH:mm:ss.ffffff}'",

            DbType.Date
                => $"'{((DateTime)param.Value):yyyy-MM-dd}'",

            DbType.Time
                => $"'{((TimeSpan)param.Value):hh\\:mm\\:ss\\.ffffff}'",

            DbType.Guid
                => $"'{param.Value}'",

            DbType.Binary
                => $"'\\x{Convert.ToHexString((byte[])param.Value)}'",

            _
                => $"'{param.Value.ToString()?.Replace("'", "''")}'"
        };
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await Task.CompletedTask;
    }
}

/// <summary>
/// PostgreSQLパラメータ (DbParameter実装)
/// </summary>
public sealed class PgParameter : DbParameter
{
    private string _parameterName = "";
    private object? _value;

    public PgParameter() { }

    public PgParameter(string parameterName, DbType dbType)
    {
        _parameterName = parameterName;
        DbType = dbType;
    }

    public PgParameter(string parameterName, object? value)
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

/// <summary>
/// PostgreSQLパラメータコレクション (DbParameterCollection実装)
/// </summary>
public sealed class PgParameterCollection : DbParameterCollection
{
    private readonly List<PgParameter> _parameters = [];
    private readonly object _syncRoot = new();

    public override int Count => _parameters.Count;
    public override object SyncRoot => _syncRoot;

    public override int Add(object value)
    {
        _parameters.Add((PgParameter)value);
        return _parameters.Count - 1;
    }

    public PgParameter Add(PgParameter parameter)
    {
        _parameters.Add(parameter);
        return parameter;
    }

    public override void AddRange(Array values)
    {
        foreach (PgParameter param in values)
        {
            _parameters.Add(param);
        }
    }

    public override void Clear()
    {
        _parameters.Clear();
    }

    public override bool Contains(object value)
    {
        return _parameters.Contains((PgParameter)value);
    }

    public override bool Contains(string value)
    {
        return _parameters.Exists(p => p.ParameterName == value);
    }

    public override void CopyTo(Array array, int index)
    {
        ((ICollection)_parameters).CopyTo(array, index);
    }

    public override IEnumerator GetEnumerator()
    {
        return _parameters.GetEnumerator();
    }

    public override int IndexOf(object value)
    {
        return _parameters.IndexOf((PgParameter)value);
    }

    public override int IndexOf(string parameterName)
    {
        return _parameters.FindIndex(p => p.ParameterName == parameterName);
    }

    public override void Insert(int index, object value)
    {
        _parameters.Insert(index, (PgParameter)value);
    }

    public override void Remove(object value)
    {
        _parameters.Remove((PgParameter)value);
    }

    public override void RemoveAt(int index)
    {
        _parameters.RemoveAt(index);
    }

    public override void RemoveAt(string parameterName)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
            _parameters.RemoveAt(index);
    }

    protected override DbParameter GetParameter(int index)
    {
        return _parameters[index];
    }

    protected override DbParameter GetParameter(string parameterName)
    {
        return _parameters.Find(p => p.ParameterName == parameterName)
            ?? throw new ArgumentException($"パラメータ '{parameterName}' が見つかりません");
    }

    protected override void SetParameter(int index, DbParameter value)
    {
        _parameters[index] = (PgParameter)value;
    }

    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
            _parameters[index] = (PgParameter)value;
        else
            _parameters.Add((PgParameter)value);
    }
}

/// <summary>
/// PostgreSQLデータリーダー (DbDataReader実装)
/// </summary>
public sealed class PgDataReader : DbDataReader
{
    private readonly PgQueryResult _result;
    private readonly PgConnection _connection;
    private readonly CommandBehavior _behavior;
    private int _currentRow = -1;
    private bool _isClosed;

    internal PgDataReader(PgQueryResult result, PgConnection connection, CommandBehavior behavior)
    {
        _result = result;
        _connection = connection;
        _behavior = behavior;
    }

    public override int FieldCount => _result.Columns.Count;
    public override int RecordsAffected => -1;
    public override bool HasRows => _result.Rows.Count > 0;
    public override bool IsClosed => _isClosed;
    public override int Depth => 0;

    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => GetValue(GetOrdinal(name));

    public override bool Read()
    {
        if (_isClosed)
            return false;

        _currentRow++;
        return _currentRow < _result.Rows.Count;
    }

    public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return Read();
    }

    public override bool NextResult()
    {
        return false;
    }

    public override async Task<bool> NextResultAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return false;
    }

    public override void Close()
    {
        _isClosed = true;

        if ((_behavior & CommandBehavior.CloseConnection) != 0)
        {
            _connection.Close();
        }
    }

    public override async Task CloseAsync()
    {
        Close();
        await Task.CompletedTask;
    }

    public override bool GetBoolean(int ordinal)
    {
        var value = GetString(ordinal);
        return value == "t" || value == "true" || value == "1";
    }

    public override byte GetByte(int ordinal)
    {
        return byte.Parse(GetString(ordinal));
    }

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        throw new NotSupportedException();
    }

    public override char GetChar(int ordinal)
    {
        return GetString(ordinal)[0];
    }

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        var str = GetString(ordinal);
        if (buffer == null)
            return str.Length;

        var copyLength = Math.Min(length, str.Length - (int)dataOffset);
        str.CopyTo((int)dataOffset, buffer, bufferOffset, copyLength);
        return copyLength;
    }

    public override DateTime GetDateTime(int ordinal)
    {
        return DateTime.Parse(GetString(ordinal));
    }

    public override decimal GetDecimal(int ordinal)
    {
        return decimal.Parse(GetString(ordinal), System.Globalization.CultureInfo.InvariantCulture);
    }

    public override double GetDouble(int ordinal)
    {
        return double.Parse(GetString(ordinal), System.Globalization.CultureInfo.InvariantCulture);
    }

    public override float GetFloat(int ordinal)
    {
        return float.Parse(GetString(ordinal), System.Globalization.CultureInfo.InvariantCulture);
    }

    public override Guid GetGuid(int ordinal)
    {
        return Guid.Parse(GetString(ordinal));
    }

    public override short GetInt16(int ordinal)
    {
        return short.Parse(GetString(ordinal));
    }

    public override int GetInt32(int ordinal)
    {
        return int.Parse(GetString(ordinal));
    }

    public override long GetInt64(int ordinal)
    {
        return long.Parse(GetString(ordinal));
    }

    public override string GetName(int ordinal)
    {
        return _result.Columns[ordinal].Name;
    }

    public override int GetOrdinal(string name)
    {
        for (int i = 0; i < _result.Columns.Count; i++)
        {
            if (_result.Columns[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        throw new IndexOutOfRangeException($"カラム '{name}' が見つかりません");
    }

    public override string GetString(int ordinal)
    {
        return GetCurrentRow()[ordinal] ?? throw new InvalidCastException("値がNULLです");
    }

    public override object GetValue(int ordinal)
    {
        var value = GetCurrentRow()[ordinal];
        return value == null ? DBNull.Value : value;
    }

    public override int GetValues(object[] values)
    {
        var row = GetCurrentRow();
        var count = Math.Min(values.Length, row.Count);
        for (int i = 0; i < count; i++)
        {
            values[i] = row[i] == null ? DBNull.Value : row[i]!;
        }
        return count;
    }

    public override bool IsDBNull(int ordinal)
    {
        return GetCurrentRow()[ordinal] == null;
    }

    public override async Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return IsDBNull(ordinal);
    }

    public override string GetDataTypeName(int ordinal)
    {
        return "text";
    }

    public override Type GetFieldType(int ordinal)
    {
        return typeof(string);
    }

    public override IEnumerator GetEnumerator()
    {
        return new DbEnumerator(this, closeReader: false);
    }

    private List<string?> GetCurrentRow()
    {
        if (_currentRow < 0 || _currentRow >= _result.Rows.Count)
            throw new InvalidOperationException("データがありません");

        return _result.Rows[_currentRow];
    }

    public override async ValueTask DisposeAsync()
    {
        Close();
        await Task.CompletedTask;
        GC.SuppressFinalize(this);
    }
}

#endregion

#region Protocol Handler

/// <summary>
/// PostgreSQLプロトコルハンドラー (低レベル通信)
/// </summary>
internal sealed class PgProtocolHandler : IAsyncDisposable
{
    private const int DefaultBufferSize = 8192;

    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private byte[]? _readBuffer;
    private byte[]? _writeBuffer;

    private string _user = "";
    private string _password = "";

    public async Task ConnectAsync(string host, int port, string database, string user, string password, CancellationToken cancellationToken)
    {
        _user = user;
        _password = password;

        _tcpClient = new TcpClient { NoDelay = true };
        await _tcpClient.ConnectAsync(host, port, cancellationToken);
        _stream = _tcpClient.GetStream();

        _readBuffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
        _writeBuffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);

        await SendStartupMessageAsync(database, user, cancellationToken);
        await HandleAuthenticationAsync(cancellationToken);
    }

    private async Task SendStartupMessageAsync(string database, string user, CancellationToken cancellationToken)
    {
        var buffer = _writeBuffer!;
        var offset = 4;

        WriteInt32BigEndian(buffer.AsSpan(), ref offset, 196608);

        offset += WriteNullTerminatedString(buffer.AsSpan(offset), "user");
        offset += WriteNullTerminatedString(buffer.AsSpan(offset), user);
        offset += WriteNullTerminatedString(buffer.AsSpan(offset), "database");
        offset += WriteNullTerminatedString(buffer.AsSpan(offset), database);
        offset += WriteNullTerminatedString(buffer.AsSpan(offset), "client_encoding");
        offset += WriteNullTerminatedString(buffer.AsSpan(offset), "UTF8");
        buffer[offset++] = 0;

        BinaryPrimitives.WriteInt32BigEndian(buffer, offset);

        await _stream!.WriteAsync(buffer.AsMemory(0, offset), cancellationToken);
    }

    private async Task HandleAuthenticationAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var (messageType, payload, payloadLength) = await ReadMessageAsync(cancellationToken);

            switch (messageType)
            {
                case 'R':
                    await HandleAuthResponseAsync(payload, payloadLength, cancellationToken);
                    ReturnBuffer(payload);
                    break;

                case 'K':
                case 'S':
                    ReturnBuffer(payload);
                    break;

                case 'Z':
                    ReturnBuffer(payload);
                    return;

                case 'E':
                    var error = ParseErrorMessage(payload.AsSpan(0, payloadLength));
                    ReturnBuffer(payload);
                    throw new PostgresException($"認証エラー: {error}");

                default:
                    ReturnBuffer(payload);
                    break;
            }
        }
    }

    private async Task HandleAuthResponseAsync(byte[] payload, int length, CancellationToken cancellationToken)
    {
        var authType = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan());

        switch (authType)
        {
            case 0:
                break;

            case 3:
                await SendPasswordMessageAsync(_password, cancellationToken);
                break;

            case 5:
                var salt = payload.AsSpan(4, 4).ToArray();
                ComputeMd5Password(salt, out var md5Password);
                await SendPasswordMessageAsync(md5Password, cancellationToken);
                break;

            case 10:
                await HandleSaslAuthAsync(cancellationToken);
                break;

            default:
                throw new PostgresException($"未対応の認証方式: {authType}");
        }
    }

    private async Task SendPasswordMessageAsync(string password, CancellationToken cancellationToken)
    {
        var passwordByteCount = Encoding.UTF8.GetByteCount(password) + 1;
        var totalLength = 1 + 4 + passwordByteCount;

        var buffer = ArrayPool<byte>.Shared.Rent(totalLength);
        try
        {
            buffer[0] = (byte)'p';
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(1), 4 + passwordByteCount);
            Encoding.UTF8.GetBytes(password, buffer.AsSpan(5));
            buffer[totalLength - 1] = 0;

            await _stream!.WriteAsync(buffer.AsMemory(0, totalLength), cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private void ComputeMd5Password(ReadOnlySpan<byte> salt, out string result)
    {
        Span<byte> innerHash = stackalloc byte[16];
        Span<byte> outerHash = stackalloc byte[16];

        var innerInput = Encoding.UTF8.GetBytes(_password + _user);
        MD5.HashData(innerInput, innerHash);

        Span<byte> innerHex = stackalloc byte[32];
        HexEncode(innerHash, innerHex);

        Span<byte> outerInput = stackalloc byte[36];
        innerHex.CopyTo(outerInput);
        salt.CopyTo(outerInput.Slice(32));
        MD5.HashData(outerInput, outerHash);

        Span<byte> outerHex = stackalloc byte[32];
        HexEncode(outerHash, outerHex);

        Span<char> passwordChars = stackalloc char[35];
        "md5".CopyTo(passwordChars);
        Encoding.ASCII.GetChars(outerHex, passwordChars.Slice(3));

        result = new string(passwordChars);
    }

    private async Task HandleSaslAuthAsync(CancellationToken cancellationToken)
    {
        var clientNonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
        var clientFirstBare = $"n=,r={clientNonce}";
        var clientFirstMessage = $"n,,{clientFirstBare}";

        await SendSaslInitialResponseAsync(clientFirstMessage, cancellationToken);

        var (msgType1, serverFirstPayload, serverFirstLength) = await ReadMessageAsync(cancellationToken);
        if (msgType1 == 'E')
        {
            var error = ParseErrorMessage(serverFirstPayload.AsSpan(0, serverFirstLength));
            ReturnBuffer(serverFirstPayload);
            throw new PostgresException($"SASL認証エラー: {error}");
        }

        var serverFirstStr = Encoding.UTF8.GetString(serverFirstPayload.AsSpan(0, serverFirstLength));
        ReturnBuffer(serverFirstPayload);

        var serverParams = ParseScramParams(serverFirstStr);
        var serverNonce = serverParams["r"];
        var salt = Convert.FromBase64String(serverParams["s"]);
        var iterations = int.Parse(serverParams["i"]);

        var clientFinalWithoutProof = $"c=biws,r={serverNonce}";
        var clientFinalMessage = ComputeScramClientFinal(clientFirstBare, serverFirstStr, clientFinalWithoutProof, salt, iterations);
        await SendSaslResponseAsync(clientFinalMessage, cancellationToken);

        var (msgType2, serverFinalPayload, _) = await ReadMessageAsync(cancellationToken);
        ReturnBuffer(serverFinalPayload);
        if (msgType2 == 'E')
            throw new PostgresException("SCRAM認証失敗");
    }

    private string ComputeScramClientFinal(string clientFirstBare, string serverFirstStr, string clientFinalWithoutProof, byte[] salt, int iterations)
    {
        Span<byte> saltedPassword = stackalloc byte[32];
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(_password), salt, saltedPassword, iterations, HashAlgorithmName.SHA256);

        Span<byte> clientKey = stackalloc byte[32];
        HMACSHA256.HashData(saltedPassword, "Client Key"u8, clientKey);

        Span<byte> storedKey = stackalloc byte[32];
        SHA256.HashData(clientKey, storedKey);

        var authMessage = $"{clientFirstBare},{serverFirstStr},{clientFinalWithoutProof}";

        Span<byte> clientSignature = stackalloc byte[32];
        HMACSHA256.HashData(storedKey, Encoding.UTF8.GetBytes(authMessage), clientSignature);

        Span<byte> clientProof = stackalloc byte[32];
        for (int i = 0; i < 32; i++)
            clientProof[i] = (byte)(clientKey[i] ^ clientSignature[i]);

        return $"{clientFinalWithoutProof},p={Convert.ToBase64String(clientProof)}";
    }

    private async Task SendSaslInitialResponseAsync(string clientFirstMessage, CancellationToken cancellationToken)
    {
        var mechanism = "SCRAM-SHA-256"u8;
        var clientFirstBytes = Encoding.UTF8.GetBytes(clientFirstMessage);
        var totalLength = 1 + 4 + mechanism.Length + 1 + 4 + clientFirstBytes.Length;

        var buffer = ArrayPool<byte>.Shared.Rent(totalLength);
        try
        {
            var offset = 0;
            buffer[offset++] = (byte)'p';
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset), totalLength - 1);
            offset += 4;
            mechanism.CopyTo(buffer.AsSpan(offset));
            offset += mechanism.Length;
            buffer[offset++] = 0;
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset), clientFirstBytes.Length);
            offset += 4;
            clientFirstBytes.CopyTo(buffer.AsSpan(offset));

            await _stream!.WriteAsync(buffer.AsMemory(0, totalLength), cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task SendSaslResponseAsync(string response, CancellationToken cancellationToken)
    {
        var responseBytes = Encoding.UTF8.GetBytes(response);
        var totalLength = 1 + 4 + responseBytes.Length;

        var buffer = ArrayPool<byte>.Shared.Rent(totalLength);
        try
        {
            buffer[0] = (byte)'p';
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(1), 4 + responseBytes.Length);
            responseBytes.CopyTo(buffer.AsSpan(5));

            await _stream!.WriteAsync(buffer.AsMemory(0, totalLength), cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public async Task<PgQueryResult> ExecuteQueryAsync(string query, CancellationToken cancellationToken)
    {
        var result = new PgQueryResult();

        await SendQueryMessageAsync(query, cancellationToken);

        while (true)
        {
            var (messageType, payload, payloadLength) = await ReadMessageAsync(cancellationToken);

            switch (messageType)
            {
                case 'T':
                    result.Columns = ParseRowDescription(payload.AsSpan(0, payloadLength));
                    ReturnBuffer(payload);
                    break;

                case 'D':
                    result.Rows.Add(ParseDataRow(payload.AsSpan(0, payloadLength), result.Columns.Count));
                    ReturnBuffer(payload);
                    break;

                case 'C':
                    ReturnBuffer(payload);
                    break;

                case 'Z':
                    ReturnBuffer(payload);
                    return result;

                case 'E':
                    var error = ParseErrorMessage(payload.AsSpan(0, payloadLength));
                    ReturnBuffer(payload);
                    throw new PostgresException($"クエリエラー: {error}");

                case 'N':
                    ReturnBuffer(payload);
                    break;

                default:
                    ReturnBuffer(payload);
                    break;
            }
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        await SendQueryMessageAsync(query, cancellationToken);

        var affectedRows = 0;

        while (true)
        {
            var (messageType, payload, payloadLength) = await ReadMessageAsync(cancellationToken);

            switch (messageType)
            {
                case 'C':
                    affectedRows = ParseCommandComplete(payload.AsSpan(0, payloadLength));
                    ReturnBuffer(payload);
                    break;

                case 'Z':
                    ReturnBuffer(payload);
                    return affectedRows;

                case 'E':
                    var error = ParseErrorMessage(payload.AsSpan(0, payloadLength));
                    ReturnBuffer(payload);
                    throw new PostgresException($"クエリエラー: {error}");

                default:
                    ReturnBuffer(payload);
                    break;
            }
        }
    }

    private async Task SendQueryMessageAsync(string query, CancellationToken cancellationToken)
    {
        var queryByteCount = Encoding.UTF8.GetByteCount(query) + 1;
        var totalLength = 1 + 4 + queryByteCount;

        var buffer = totalLength <= _writeBuffer!.Length
            ? _writeBuffer
            : ArrayPool<byte>.Shared.Rent(totalLength);

        try
        {
            buffer[0] = (byte)'Q';
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(1), 4 + queryByteCount);
            Encoding.UTF8.GetBytes(query, buffer.AsSpan(5));
            buffer[5 + queryByteCount - 1] = 0;

            await _stream!.WriteAsync(buffer.AsMemory(0, totalLength), cancellationToken);
        }
        finally
        {
            if (!ReferenceEquals(buffer, _writeBuffer))
                ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task<(char type, byte[] payload, int length)> ReadMessageAsync(CancellationToken cancellationToken)
    {
        await ReadExactAsync(_readBuffer.AsMemory(0, 5), cancellationToken);

        var type = (char)_readBuffer![0];
        var length = BinaryPrimitives.ReadInt32BigEndian(_readBuffer.AsSpan(1)) - 4;

        if (length == 0)
            return (type, Array.Empty<byte>(), 0);

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        await ReadExactAsync(buffer.AsMemory(0, length), cancellationToken);

        return (type, buffer, length);
    }

    private async Task ReadExactAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await _stream!.ReadAsync(buffer.Slice(offset), cancellationToken);
            if (read == 0)
                throw new PostgresException("接続が閉じられました");
            offset += read;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReturnBuffer(byte[] buffer)
    {
        if (buffer.Length > 0)
            ArrayPool<byte>.Shared.Return(buffer);
    }

    private static List<PgColumnInfo> ParseRowDescription(ReadOnlySpan<byte> payload)
    {
        var fieldCount = BinaryPrimitives.ReadInt16BigEndian(payload);
        var columns = new List<PgColumnInfo>(fieldCount);
        var offset = 2;

        for (int i = 0; i < fieldCount; i++)
        {
            var nameEnd = payload.Slice(offset).IndexOf((byte)0);
            var name = Encoding.UTF8.GetString(payload.Slice(offset, nameEnd));
            offset += nameEnd + 1;

            // テーブルOID (4), 列番号 (2), 型OID (4), 型サイズ (2), 型修飾子 (4), フォーマット (2)
            var typeOid = BinaryPrimitives.ReadInt32BigEndian(payload.Slice(offset + 6));
            offset += 18;

            columns.Add(new PgColumnInfo(name, typeOid));
        }

        return columns;
    }

    private static List<string?> ParseDataRow(ReadOnlySpan<byte> payload, int columnCount)
    {
        var values = new List<string?>(columnCount);
        var offset = 2;

        for (int i = 0; i < columnCount; i++)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(payload.Slice(offset));
            offset += 4;

            if (length == -1)
            {
                values.Add(null);
            }
            else
            {
                values.Add(Encoding.UTF8.GetString(payload.Slice(offset, length)));
                offset += length;
            }
        }

        return values;
    }

    private static int ParseCommandComplete(ReadOnlySpan<byte> payload)
    {
        var message = Encoding.UTF8.GetString(payload.TrimEnd((byte)0));
        var lastSpace = message.LastIndexOf(' ');
        if (lastSpace >= 0 && int.TryParse(message.AsSpan(lastSpace + 1), out var count))
            return count;
        return 0;
    }

    private static string ParseErrorMessage(ReadOnlySpan<byte> payload)
    {
        var offset = 0;
        while (offset < payload.Length && payload[offset] != 0)
        {
            var fieldType = (char)payload[offset++];
            var end = payload.Slice(offset).IndexOf((byte)0);
            if (fieldType == 'M')
                return Encoding.UTF8.GetString(payload.Slice(offset, end));
            offset += end + 1;
        }
        return "Unknown error";
    }

    private static Dictionary<string, string> ParseScramParams(string message)
    {
        var result = new Dictionary<string, string>(3);
        foreach (var part in message.Split(','))
        {
            var idx = part.IndexOf('=');
            if (idx > 0)
                result[part[..idx]] = part[(idx + 1)..];
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteInt32BigEndian(Span<byte> buffer, ref int offset, int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(offset), value);
        offset += 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WriteNullTerminatedString(Span<byte> buffer, ReadOnlySpan<char> value)
    {
        var bytesWritten = Encoding.UTF8.GetBytes(value, buffer);
        buffer[bytesWritten] = 0;
        return bytesWritten + 1;
    }

    private static ReadOnlySpan<byte> HexChars => "0123456789abcdef"u8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void HexEncode(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        for (int i = 0; i < source.Length; i++)
        {
            destination[i * 2] = HexChars[source[i] >> 4];
            destination[i * 2 + 1] = HexChars[source[i] & 0xF];
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_stream != null)
        {
            var terminate = new byte[5];
            terminate[0] = (byte)'X';
            BinaryPrimitives.WriteInt32BigEndian(terminate.AsSpan(1), 4);

            try
            {
                await _stream.WriteAsync(terminate);
            }
            catch
            {
                // 無視
            }

            await _stream.DisposeAsync();
        }

        _tcpClient?.Dispose();

        if (_readBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(_readBuffer);
            _readBuffer = null;
        }

        if (_writeBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(_writeBuffer);
            _writeBuffer = null;
        }
    }
}

internal readonly record struct PgColumnInfo(string Name, int TypeOid);

internal sealed class PgQueryResult
{
    public List<PgColumnInfo> Columns { get; set; } = [];
    public List<List<string?>> Rows { get; set; } = [];
}

#endregion
