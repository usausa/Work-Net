using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace MyPgsql.Binary;

#region ADO.NET Implementation - Binary Protocol

/// <summary>
/// PostgreSQL接続 (DbConnection実装) - バイナリプロトコル版
/// Extended Query Protocolを使用して高速なバイナリフォーマットでデータを取得
/// </summary>
public sealed class PgBinaryConnection : DbConnection
{
    private PgConnectionStringBuilder _connectionStringBuilder = new();
    private string _connectionString = "";
    private ConnectionState _state = ConnectionState.Closed;
    private PgBinaryProtocolHandler? _protocol;
    private PgBinaryTransaction? _currentTransaction;

    public PgBinaryConnection() { }

    public PgBinaryConnection(string connectionString)
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

    internal PgBinaryProtocolHandler Protocol => _protocol ?? throw new InvalidOperationException("接続が開かれていません");
    internal PgBinaryTransaction? CurrentTransaction => _currentTransaction;

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
            _protocol = new PgBinaryProtocolHandler();
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

    public new async Task<PgBinaryTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
    }

    public async Task<PgBinaryTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
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

        await Protocol.ExecuteSimpleQueryAsync($"BEGIN ISOLATION LEVEL {isolationLevelSql}", cancellationToken);
        _currentTransaction = new PgBinaryTransaction(this, isolationLevel);
        return _currentTransaction;
    }

    internal void ClearTransaction()
    {
        _currentTransaction = null;
    }

    public new PgBinaryCommand CreateCommand()
    {
        return new PgBinaryCommand { Connection = this };
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
/// PostgreSQLトランザクション (DbTransaction実装) - バイナリプロトコル版
/// </summary>
public sealed class PgBinaryTransaction : DbTransaction
{
    private readonly PgBinaryConnection _connection;
    private readonly IsolationLevel _isolationLevel;
    private bool _completed;

    internal PgBinaryTransaction(PgBinaryConnection connection, IsolationLevel isolationLevel)
    {
        _connection = connection;
        _isolationLevel = isolationLevel;
    }

    public new PgBinaryConnection Connection => _connection;
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

        await _connection.Protocol.ExecuteSimpleQueryAsync("COMMIT", cancellationToken);
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

        await _connection.Protocol.ExecuteSimpleQueryAsync("ROLLBACK", cancellationToken);
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
/// PostgreSQLコマンド (DbCommand実装) - バイナリプロトコル版
/// </summary>
public sealed class PgBinaryCommand : DbCommand
{
    private PgBinaryConnection? _connection;
    private PgBinaryTransaction? _transaction;
    private string _commandText = "";
    private readonly PgParameterCollection _parameters = new();
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

    public new PgParameterCollection Parameters => _parameters;
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
        return await _connection!.Protocol.ExecuteNonQueryAsync(_commandText, cancellationToken);
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
        await _connection!.Protocol.SendExtendedQueryAsync(_commandText, cancellationToken);
        return new PgBinaryDataReader(_connection.Protocol, _connection, behavior, cancellationToken);
    }

    public override void Prepare()
    {
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

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await Task.CompletedTask;
    }
}

/// <summary>
/// カラム情報（軽量構造体）- バイナリプロトコル用
/// </summary>
internal readonly record struct PgBinaryColumnInfo(string Name, int TypeOid, short FormatCode);

/// <summary>
/// PostgreSQLデータリーダー (DbDataReader実装) - バイナリプロトコル版
/// </summary>
public sealed class PgBinaryDataReader : DbDataReader
{
    // PostgreSQL epoch (2000-01-01 00:00:00 UTC)
    private static readonly DateTime PostgresEpoch = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly PgBinaryProtocolHandler _protocol;
    private readonly PgBinaryConnection _connection;
    private readonly CommandBehavior _behavior;
    private readonly CancellationToken _cancellationToken;
    private PgBinaryColumnInfo[]? _columns;
    private int _columnCount;
    private bool _hasRows;
    private bool _firstRowRead;
    private bool _isClosed;
    private bool _completed;

    // 行データへの直接参照
    private byte[]? _rowBuffer;
    private int _rowBaseOffset;
    private int[]? _offsets;
    private int[]? _lengths;

    internal PgBinaryDataReader(PgBinaryProtocolHandler protocol, PgBinaryConnection connection, CommandBehavior behavior, CancellationToken cancellationToken)
    {
        _protocol = protocol;
        _connection = connection;
        _behavior = behavior;
        _cancellationToken = cancellationToken;
    }

    public override int FieldCount => _columnCount;
    public override int RecordsAffected => -1;
    public override bool HasRows => _hasRows;
    public override bool IsClosed => _isClosed;
    public override int Depth => 0;

    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => GetValue(GetOrdinal(name));

    public override bool Read()
    {
        return ReadAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        return ReadAsyncCore(cancellationToken).AsTask();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ValueTask<bool> ReadAsyncCore(CancellationToken cancellationToken)
    {
        if (_completed)
            return new ValueTask<bool>(false);

        // 同期パス: バッファに十分なデータがある場合
        var result = TryReadSync();
        if (result.HasValue)
            return new ValueTask<bool>(result.Value);

        // 非同期パス
        return ReadAsyncCoreInternal(cancellationToken);
    }

    /// <summary>
    /// バッファ内のデータで同期的に読み取りを試行
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool? TryReadSync()
    {
        var buffer = _protocol.StreamBuffer;
        ref var pos = ref _protocol.StreamBufferPos;
        var len = _protocol.StreamBufferLen;

        while (true)
        {
            var available = len - pos;

            // ヘッダー（5バイト）が読めるか確認
            if (available < 5)
                return null;

            var messageType = (char)buffer[pos];
            var payloadLength = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(pos + 1)) - 4;

            // ペイロード全体が読めるか確認
            var totalMessageSize = 5 + payloadLength;
            if (available < totalMessageSize)
                return null;

            pos += 5;
            var payloadOffset = pos;

            switch (messageType)
            {
                case 'D': // DataRow - 最も頻繁なケースを最初に
                    ParseDataRow(buffer.AsSpan(payloadOffset, payloadLength), payloadOffset);
                    pos += payloadLength;
                    if (!_firstRowRead)
                    {
                        _hasRows = true;
                        _firstRowRead = true;
                    }
                    return true;

                case 'T': // RowDescription
                    ParseRowDescription(buffer.AsSpan(payloadOffset, payloadLength));
                    pos += payloadLength;
                    break;

                case 'C': // CommandComplete
                    pos += payloadLength;
                    break;

                case 'Z': // ReadyForQuery
                    pos += payloadLength;
                    _completed = true;
                    return false;

                case 'E': // Error
                    var error = ParseErrorMessage(buffer.AsSpan(payloadOffset, payloadLength));
                    pos += payloadLength;
                    throw new PostgresException($"クエリエラー: {error}");

                case '1': // ParseComplete (Extended Query Protocol)
                case '2': // BindComplete (Extended Query Protocol)
                case 'n': // NoData (Extended Query Protocol)
                    pos += payloadLength;
                    break;

                default:
                    pos += payloadLength;
                    break;
            }
        }
    }

    /// <summary>
    /// 非同期読み取り（バッファ不足時）
    /// </summary>
    private async ValueTask<bool> ReadAsyncCoreInternal(CancellationToken cancellationToken)
    {
        while (true)
        {
            // ヘッダーを読み取り
            await _protocol.EnsureBufferedAsync(5, cancellationToken).ConfigureAwait(false);

            var buffer = _protocol.StreamBuffer;
            var pos = _protocol.StreamBufferPos;

            var messageType = (char)buffer[pos];
            var payloadLength = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(pos + 1)) - 4;

            // ヘッダー+ペイロードを一度に確保
            await _protocol.EnsureBufferedAsync(5 + payloadLength, cancellationToken).ConfigureAwait(false);

            // EnsureBufferedAsync後にバッファが変わる可能性があるため再取得
            buffer = _protocol.StreamBuffer;
            pos = _protocol.StreamBufferPos;

            _protocol.StreamBufferPos = pos + 5;
            var payloadOffset = pos + 5;
            var payload = buffer.AsSpan(payloadOffset, payloadLength);

            switch (messageType)
            {
                case 'D': // DataRow
                    ParseDataRow(payload, payloadOffset);
                    _protocol.StreamBufferPos += payloadLength;
                    if (!_firstRowRead)
                    {
                        _hasRows = true;
                        _firstRowRead = true;
                    }
                    return true;

                case 'T': // RowDescription
                    ParseRowDescription(payload);
                    _protocol.StreamBufferPos += payloadLength;
                    // 同期パスで継続を試行
                    var syncResult = TryReadSync();
                    if (syncResult.HasValue)
                        return syncResult.Value;
                    break;

                case 'C': // CommandComplete
                    _protocol.StreamBufferPos += payloadLength;
                    // 同期パスで継続を試行
                    syncResult = TryReadSync();
                    if (syncResult.HasValue)
                        return syncResult.Value;
                    break;

                case 'Z': // ReadyForQuery
                    _protocol.StreamBufferPos += payloadLength;
                    _completed = true;
                    return false;

                case 'E': // Error
                    var error = ParseErrorMessage(payload);
                    _protocol.StreamBufferPos += payloadLength;
                    throw new PostgresException($"クエリエラー: {error}");

                case '1': // ParseComplete (Extended Query Protocol)
                case '2': // BindComplete (Extended Query Protocol)
                case 'n': // NoData (Extended Query Protocol)
                    _protocol.StreamBufferPos += payloadLength;
                    // 同期パスで継続を試行
                    syncResult = TryReadSync();
                    if (syncResult.HasValue)
                        return syncResult.Value;
                    break;

                default:
                    _protocol.StreamBufferPos += payloadLength;
                    // 同期パスで継続を試行
                    syncResult = TryReadSync();
                    if (syncResult.HasValue)
                        return syncResult.Value;
                    break;
            }
        }
    }

    private void ParseRowDescription(ReadOnlySpan<byte> payload)
    {
        var fieldCount = BinaryPrimitives.ReadInt16BigEndian(payload);
        _columnCount = fieldCount;
        _columns = ArrayPool<PgBinaryColumnInfo>.Shared.Rent(fieldCount);
        _offsets = ArrayPool<int>.Shared.Rent(fieldCount);
        _lengths = ArrayPool<int>.Shared.Rent(fieldCount);

        var offset = 2;
        for (int i = 0; i < fieldCount; i++)
        {
            var nameEnd = payload.Slice(offset).IndexOf((byte)0);
            var name = Encoding.UTF8.GetString(payload.Slice(offset, nameEnd));
            offset += nameEnd + 1;

            // テーブルOID (4), 列番号 (2), 型OID (4), 型サイズ (2), 型修飾子 (4), フォーマット (2)
            var typeOid = BinaryPrimitives.ReadInt32BigEndian(payload.Slice(offset + 6));
            var formatCode = BinaryPrimitives.ReadInt16BigEndian(payload.Slice(offset + 16));
            offset += 18;

            _columns[i] = new PgBinaryColumnInfo(name, typeOid, formatCode);
        }
    }

    private void ParseDataRow(ReadOnlySpan<byte> payload, int payloadOffset)
    {
        var columnCount = BinaryPrimitives.ReadInt16BigEndian(payload);
        _rowBuffer = _protocol.StreamBuffer;
        _rowBaseOffset = payloadOffset;

        var currentOffset = 2;
        for (int i = 0; i < columnCount; i++)
        {
            var len = BinaryPrimitives.ReadInt32BigEndian(payload.Slice(currentOffset));
            currentOffset += 4;

            _offsets![i] = currentOffset;
            _lengths![i] = len;

            if (len > 0)
                currentOffset += len;
        }
    }

    public override bool NextResult()
    {
        return false;
    }

    public override Task<bool> NextResultAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public override void Close()
    {
        if (_isClosed) return;
        _isClosed = true;

        // プールからのバッファを返却
        if (_columns != null)
        {
            ArrayPool<PgBinaryColumnInfo>.Shared.Return(_columns);
            _columns = null;
        }
        if (_offsets != null)
        {
            ArrayPool<int>.Shared.Return(_offsets);
            _offsets = null;
        }
        if (_lengths != null)
        {
            ArrayPool<int>.Shared.Return(_lengths);
            _lengths = null;
        }

        _rowBuffer = null;

        if ((_behavior & CommandBehavior.CloseConnection) != 0)
        {
            _connection.Close();
        }
    }

    public override Task CloseAsync()
    {
        Close();
        return Task.CompletedTask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> GetValueSpan(int ordinal)
    {
        var length = _lengths![ordinal];
        if (length == -1)
            throw new InvalidCastException("値がNULLです");
        return _rowBuffer.AsSpan(_rowBaseOffset + _offsets![ordinal], length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsBinaryColumn(int ordinal) => _columns![ordinal].FormatCode == 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool IsDBNull(int ordinal) => _lengths![ordinal] == -1;

    public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken)
    {
        return Task.FromResult(IsDBNull(ordinal));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool GetBoolean(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        if (IsBinaryColumn(ordinal))
        {
            return span[0] != 0;
        }
        return span.Length > 0 && (span[0] == 't' || span[0] == '1');
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte GetByte(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        if (IsBinaryColumn(ordinal))
        {
            return span[0];
        }
        Utf8Parser.TryParse(span, out byte value, out _);
        return value;
    }

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        throw new NotSupportedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override char GetChar(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        return (char)span[0];
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override DateTime GetDateTime(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        if (IsBinaryColumn(ordinal))
        {
            // PostgreSQLバイナリ形式: 2000-01-01からのマイクロ秒数 (Int64)
            var microseconds = BinaryPrimitives.ReadInt64BigEndian(span);
            return PostgresEpoch.AddTicks(microseconds * 10); // 1 microsecond = 10 ticks
        }
        // テキスト形式
        Span<char> chars = stackalloc char[span.Length];
        var charCount = Encoding.UTF8.GetChars(span, chars);
        return DateTime.Parse(chars.Slice(0, charCount));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override decimal GetDecimal(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        // PostgreSQLのnumericバイナリ形式は複雑なので、テキストパースを使用
        Utf8Parser.TryParse(span, out decimal value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override double GetDouble(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        if (IsBinaryColumn(ordinal))
        {
            var bits = BinaryPrimitives.ReadInt64BigEndian(span);
            return BitConverter.Int64BitsToDouble(bits);
        }
        Utf8Parser.TryParse(span, out double value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override float GetFloat(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        if (IsBinaryColumn(ordinal))
        {
            var bits = BinaryPrimitives.ReadInt32BigEndian(span);
            return BitConverter.Int32BitsToSingle(bits);
        }
        Utf8Parser.TryParse(span, out float value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Guid GetGuid(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        if (IsBinaryColumn(ordinal))
        {
            return new Guid(span);
        }
        Utf8Parser.TryParse(span, out Guid value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override short GetInt16(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        if (IsBinaryColumn(ordinal))
        {
            return BinaryPrimitives.ReadInt16BigEndian(span);
        }
        Utf8Parser.TryParse(span, out short value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetInt32(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        if (IsBinaryColumn(ordinal))
        {
            return BinaryPrimitives.ReadInt32BigEndian(span);
        }
        Utf8Parser.TryParse(span, out int value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long GetInt64(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        if (IsBinaryColumn(ordinal))
        {
            return BinaryPrimitives.ReadInt64BigEndian(span);
        }
        Utf8Parser.TryParse(span, out long value, out _);
        return value;
    }

    public override string GetName(int ordinal)
    {
        return _columns![ordinal].Name;
    }

    public override int GetOrdinal(string name)
    {
        var columns = _columns!;
        for (int i = 0; i < _columnCount; i++)
        {
            if (columns[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        throw new IndexOutOfRangeException($"カラム '{name}' が見つかりません");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string GetString(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        return Encoding.UTF8.GetString(span);
    }

    public string? GetStringOrNull(int ordinal)
    {
        if (IsDBNull(ordinal))
            return null;
        return GetString(ordinal);
    }

    public override object GetValue(int ordinal)
    {
        if (IsDBNull(ordinal))
            return DBNull.Value;
        return GetString(ordinal);
    }

    public override int GetValues(object[] values)
    {
        var count = Math.Min(values.Length, _columnCount);
        for (int i = 0; i < count; i++)
        {
            values[i] = IsDBNull(i) ? DBNull.Value : GetString(i);
        }
        return count;
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

    public override ValueTask DisposeAsync()
    {
        Close();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
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
}

#endregion

#region Binary Protocol Handler

/// <summary>
/// PostgreSQLプロトコルハンドラー - バイナリプロトコル版
/// Extended Query Protocolを使用
/// </summary>
internal sealed class PgBinaryProtocolHandler : IAsyncDisposable
{
    private const int DefaultBufferSize = 8192;
    private const int StreamBufferSize = 65536 * 4;

    private Socket? _socket;
    private string _user = "";
    private string _password = "";

    private byte[]? _writeBuffer;
    private byte[]? _readBuffer;
    private byte[] _streamBuffer = null!;
    private int _streamBufferPos;
    private int _streamBufferLen;

    public async Task ConnectAsync(string host, int port, string database, string user, string password, CancellationToken cancellationToken)
    {
        _user = user;
        _password = password;

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true,
            ReceiveBufferSize = 65536 * 4,
            SendBufferSize = 65536
        };

        await _socket.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);

        _streamBuffer = ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        _writeBuffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
        _readBuffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);

        await SendStartupMessageAsync(database, user, cancellationToken).ConfigureAwait(false);
        await HandleAuthenticationAsync(cancellationToken).ConfigureAwait(false);
    }

    internal byte[] StreamBuffer => _streamBuffer;
    internal ref int StreamBufferPos => ref _streamBufferPos;
    internal int StreamBufferLen => _streamBufferLen;

    /// <summary>
    /// Extended Query Protocol でクエリを送信（バイナリフォーマット）
    /// </summary>
    public async ValueTask SendExtendedQueryAsync(string sql, CancellationToken cancellationToken)
    {
        var sqlBytes = Encoding.UTF8.GetBytes(sql);

        // 必要なバッファサイズを計算
        // Parse: 1 + 4 + 1(name) + sqlLen + 1 + 2(param count)
        // Bind: 1 + 4 + 1(portal) + 1(stmt) + 2(format count) + 2(param count) + 2(result format count) + 2(result format=1)
        // Describe: 1 + 4 + 1(type) + 1(name)
        // Execute: 1 + 4 + 1(portal) + 4(max rows)
        // Sync: 1 + 4
        var parseLen = 1 + 4 + 1 + sqlBytes.Length + 1 + 2;
        var bindLen = 1 + 4 + 1 + 1 + 2 + 2 + 2 + 2;
        var describeLen = 1 + 4 + 1 + 1;
        var executeLen = 1 + 4 + 1 + 4;
        var syncLen = 1 + 4;
        var totalLen = parseLen + bindLen + describeLen + executeLen + syncLen;

        var buffer = ArrayPool<byte>.Shared.Rent(totalLen);
        try
        {
            var offset = 0;

            // Parse message ('P')
            buffer[offset++] = (byte)'P';
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset), parseLen - 1);
            offset += 4;
            buffer[offset++] = 0; // unnamed statement
            sqlBytes.CopyTo(buffer.AsSpan(offset));
            offset += sqlBytes.Length;
            buffer[offset++] = 0; // null terminator
            BinaryPrimitives.WriteInt16BigEndian(buffer.AsSpan(offset), 0); // no parameter types
            offset += 2;

            // Bind message ('B')
            buffer[offset++] = (byte)'B';
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset), bindLen - 1);
            offset += 4;
            buffer[offset++] = 0; // unnamed portal
            buffer[offset++] = 0; // unnamed statement
            BinaryPrimitives.WriteInt16BigEndian(buffer.AsSpan(offset), 0); // no parameter format codes
            offset += 2;
            BinaryPrimitives.WriteInt16BigEndian(buffer.AsSpan(offset), 0); // no parameters
            offset += 2;
            BinaryPrimitives.WriteInt16BigEndian(buffer.AsSpan(offset), 1); // one result format code
            offset += 2;
            BinaryPrimitives.WriteInt16BigEndian(buffer.AsSpan(offset), 1); // binary format for all columns
            offset += 2;

            // Describe message ('D')
            buffer[offset++] = (byte)'D';
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset), describeLen - 1);
            offset += 4;
            buffer[offset++] = (byte)'P'; // describe portal
            buffer[offset++] = 0; // unnamed portal

            // Execute message ('E')
            buffer[offset++] = (byte)'E';
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset), executeLen - 1);
            offset += 4;
            buffer[offset++] = 0; // unnamed portal
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset), 0); // no row limit
            offset += 4;

            // Sync message ('S')
            buffer[offset++] = (byte)'S';
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset), 4);
            offset += 4;

            await _socket!.SendAsync(buffer.AsMemory(0, offset), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, CancellationToken cancellationToken)
    {
        await SendExtendedQueryAsync(sql, cancellationToken);

        var affectedRows = 0;

        while (true)
        {
            await EnsureBufferedAsync(5, cancellationToken).ConfigureAwait(false);
            var messageType = (char)_streamBuffer[_streamBufferPos];
            var length = BinaryPrimitives.ReadInt32BigEndian(_streamBuffer.AsSpan(_streamBufferPos + 1)) - 4;
            _streamBufferPos += 5;

            await EnsureBufferedAsync(length, cancellationToken).ConfigureAwait(false);
            var payload = _streamBuffer.AsSpan(_streamBufferPos, length);

            switch (messageType)
            {
                case 'C':
                    affectedRows = ParseCommandComplete(payload);
                    _streamBufferPos += length;
                    break;

                case 'Z':
                    _streamBufferPos += length;
                    return affectedRows;

                case 'E':
                    var error = ParseErrorMessage(payload);
                    _streamBufferPos += length;
                    throw new PostgresException($"クエリエラー: {error}");

                default:
                    _streamBufferPos += length;
                    break;
            }
        }
    }

    /// <summary>
    /// Simple Query Protocol でクエリを実行（トランザクション制御用）
    /// </summary>
    public async Task<int> ExecuteSimpleQueryAsync(string sql, CancellationToken cancellationToken)
    {
        await SendSimpleQueryAsync(sql, cancellationToken);

        var affectedRows = 0;

        while (true)
        {
            await EnsureBufferedAsync(5, cancellationToken).ConfigureAwait(false);
            var messageType = (char)_streamBuffer[_streamBufferPos];
            var length = BinaryPrimitives.ReadInt32BigEndian(_streamBuffer.AsSpan(_streamBufferPos + 1)) - 4;
            _streamBufferPos += 5;

            await EnsureBufferedAsync(length, cancellationToken).ConfigureAwait(false);
            var payload = _streamBuffer.AsSpan(_streamBufferPos, length);

            switch (messageType)
            {
                case 'C':
                    affectedRows = ParseCommandComplete(payload);
                    _streamBufferPos += length;
                    break;

                case 'Z':
                    _streamBufferPos += length;
                    return affectedRows;

                case 'E':
                    var error = ParseErrorMessage(payload);
                    _streamBufferPos += length;
                    throw new PostgresException($"クエリエラー: {error}");

                default:
                    _streamBufferPos += length;
                    break;
            }
        }
    }

    private async ValueTask SendSimpleQueryAsync(string sql, CancellationToken cancellationToken)
    {
        var sqlByteCount = Encoding.UTF8.GetByteCount(sql);
        var queryByteCount = sqlByteCount + 1; // +1 for null terminator
        var totalLength = 1 + 4 + queryByteCount;

        var buffer = totalLength <= _writeBuffer!.Length
            ? _writeBuffer
            : ArrayPool<byte>.Shared.Rent(totalLength);

        try
        {
            buffer[0] = (byte)'Q';
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(1), 4 + queryByteCount);
            Encoding.UTF8.GetBytes(sql, buffer.AsSpan(5));
            buffer[5 + sqlByteCount] = 0; // null terminator

            await _socket!.SendAsync(buffer.AsMemory(0, totalLength), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (!ReferenceEquals(buffer, _writeBuffer))
                ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ValueTask EnsureBufferedAsync(int count, CancellationToken cancellationToken)
    {
        var available = _streamBufferLen - _streamBufferPos;
        if (available >= count)
            return ValueTask.CompletedTask;

        return EnsureBufferedAsyncCore(count, available, cancellationToken);
    }

    private async ValueTask EnsureBufferedAsyncCore(int count, int available, CancellationToken cancellationToken)
    {
        var needed = count - available;
        var freeSpace = _streamBuffer.Length - _streamBufferLen;

        // 空き容量が足りない場合のみシフトまたは拡張
        if (freeSpace < needed)
        {
            // バッファ全体で足りるならシフト
            if (_streamBuffer.Length >= count)
            {
                if (available > 0)
                {
                    _streamBuffer.AsSpan(_streamBufferPos, available).CopyTo(_streamBuffer);
                }
                _streamBufferPos = 0;
                _streamBufferLen = available;
                freeSpace = _streamBuffer.Length - available;
            }
            else
            {
                // バッファ拡張が必要
                var newSize = Math.Max(_streamBuffer.Length * 2, count);
                var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
                if (available > 0)
                {
                    _streamBuffer.AsSpan(_streamBufferPos, available).CopyTo(newBuffer);
                }
                ArrayPool<byte>.Shared.Return(_streamBuffer);
                _streamBuffer = newBuffer;
                _streamBufferPos = 0;
                _streamBufferLen = available;
                freeSpace = newBuffer.Length - available;
            }
        }

        // 必要な量を読み取る（可能な限り多く読み取る）
        do
        {
            var read = await _socket!.ReceiveAsync(
                _streamBuffer.AsMemory(_streamBufferLen, freeSpace),
                cancellationToken).ConfigureAwait(false);

            if (read == 0)
                throw new PostgresException("接続が閉じられました");

            _streamBufferLen += read;
            freeSpace -= read;
        }
        while (_streamBufferLen - _streamBufferPos < count);

        // 追加で利用可能なデータがあれば貪欲に読み取る（ノンブロッキング）
        while (freeSpace > 0)
        {
            var socketAvailable = _socket!.Available;
            if (socketAvailable <= 0)
                break;

            var toRead = Math.Min(socketAvailable, freeSpace);
            var extraRead = await _socket.ReceiveAsync(
                _streamBuffer.AsMemory(_streamBufferLen, toRead),
                cancellationToken).ConfigureAwait(false);

            if (extraRead == 0)
                break;

            _streamBufferLen += extraRead;
            freeSpace -= extraRead;
        }
    }

    private async ValueTask SendStartupMessageAsync(string database, string user, CancellationToken cancellationToken)
    {
        var buffer = _writeBuffer!;
        var offset = 4;

        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset), 196608);
        offset += 4;

        offset += WriteNullTerminatedString(buffer.AsSpan(offset), "user");
        offset += WriteNullTerminatedString(buffer.AsSpan(offset), user);
        offset += WriteNullTerminatedString(buffer.AsSpan(offset), "database");
        offset += WriteNullTerminatedString(buffer.AsSpan(offset), database);
        offset += WriteNullTerminatedString(buffer.AsSpan(offset), "client_encoding");
        offset += WriteNullTerminatedString(buffer.AsSpan(offset), "UTF8");
        buffer[offset++] = 0;

        BinaryPrimitives.WriteInt32BigEndian(buffer, offset);

        await _socket!.SendAsync(buffer.AsMemory(0, offset), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask HandleAuthenticationAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var (messageType, payload, payloadLength) = await ReadMessageAsync(cancellationToken).ConfigureAwait(false);

            switch (messageType)
            {
                case 'R':
                    await HandleAuthResponseAsync(payload, payloadLength, cancellationToken).ConfigureAwait(false);
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

    private async ValueTask HandleAuthResponseAsync(byte[] payload, int length, CancellationToken cancellationToken)
    {
        var authType = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan());

        switch (authType)
        {
            case 0: // AuthenticationOk
                break;

            case 3: // AuthenticationCleartextPassword
                await SendPasswordMessageAsync(_password, cancellationToken).ConfigureAwait(false);
                break;

            case 5: // AuthenticationMD5Password
                var salt = payload.AsSpan(4, 4).ToArray();
                ComputeMd5Password(salt, out var md5Password);
                await SendPasswordMessageAsync(md5Password, cancellationToken).ConfigureAwait(false);
                break;

            case 10: // AuthenticationSASL
                await HandleSaslAuthAsync(cancellationToken).ConfigureAwait(false);
                break;

            default:
                throw new PostgresException($"未対応の認証方式: {authType}");
        }
    }

    private async ValueTask SendPasswordMessageAsync(string password, CancellationToken cancellationToken)
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

            await _socket!.SendAsync(buffer.AsMemory(0, totalLength), cancellationToken).ConfigureAwait(false);
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

    private async ValueTask HandleSaslAuthAsync(CancellationToken cancellationToken)
    {
        var clientNonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
        var clientFirstBare = $"n=,r={clientNonce}";
        var clientFirstMessage = $"n,,{clientFirstBare}";

        await SendSaslInitialResponseAsync(clientFirstMessage, cancellationToken).ConfigureAwait(false);

        var (msgType1, serverFirstPayload, serverFirstLength) = await ReadMessageAsync(cancellationToken).ConfigureAwait(false);
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
        await SendSaslResponseAsync(clientFinalMessage, cancellationToken).ConfigureAwait(false);

        var (msgType2, serverFinalPayload, _) = await ReadMessageAsync(cancellationToken).ConfigureAwait(false);
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

    private async ValueTask SendSaslInitialResponseAsync(string clientFirstMessage, CancellationToken cancellationToken)
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

            await _socket!.SendAsync(buffer.AsMemory(0, totalLength), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async ValueTask SendSaslResponseAsync(string response, CancellationToken cancellationToken)
    {
        var responseBytes = Encoding.UTF8.GetBytes(response);
        var totalLength = 1 + 4 + responseBytes.Length;

        var buffer = ArrayPool<byte>.Shared.Rent(totalLength);
        try
        {
            buffer[0] = (byte)'p';
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(1), 4 + responseBytes.Length);
            responseBytes.CopyTo(buffer.AsSpan(5));

            await _socket!.SendAsync(buffer.AsMemory(0, totalLength), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task<(char type, byte[] payload, int length)> ReadMessageAsync(CancellationToken cancellationToken)
    {
        await ReadExactAsync(_readBuffer.AsMemory(0, 5), cancellationToken).ConfigureAwait(false);

        var type = (char)_readBuffer![0];
        var length = BinaryPrimitives.ReadInt32BigEndian(_readBuffer.AsSpan(1)) - 4;

        if (length == 0)
            return (type, Array.Empty<byte>(), 0);

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        await ReadExactAsync(buffer.AsMemory(0, length), cancellationToken).ConfigureAwait(false);

        return (type, buffer, length);
    }

    private async ValueTask ReadExactAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await _socket!.ReceiveAsync(buffer.Slice(offset), cancellationToken).ConfigureAwait(false);
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

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_socket != null && _socket.Connected)
        {
            var terminate = new byte[5];
            terminate[0] = (byte)'X';
            BinaryPrimitives.WriteInt32BigEndian(terminate.AsSpan(1), 4);

            try
            {
                await _socket.SendAsync(terminate).ConfigureAwait(false);
            }
            catch
            {
                // 無視
            }

            _socket.Dispose();
            _socket = null;
        }

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

        if (_streamBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(_streamBuffer);
            _streamBuffer = null!;
        }
    }
}

#endregion
