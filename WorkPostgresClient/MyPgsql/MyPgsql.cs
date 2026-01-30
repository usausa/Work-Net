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
        var (sqlBuffer, sqlLength) = BuildSqlUtf8();
        try
        {
            return await _connection!.Protocol.ExecuteNonQueryAsync(sqlBuffer, sqlLength, cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sqlBuffer);
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
        var (sqlBuffer, sqlLength) = BuildSqlUtf8();
        try
        {
            var context = await _connection!.Protocol.ExecuteQueryStreamingAsync(sqlBuffer, sqlLength, cancellationToken);
            return new PgDataReader(context, _connection, behavior);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sqlBuffer);
        }
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

    /// <summary>
    /// SQLをUTF8バイト配列として構築（プールから借用したバッファを返す）
    /// </summary>
    private (byte[] buffer, int length) BuildSqlUtf8()
    {
        if (_parameters.Count == 0)
        {
            // パラメータなしの場合：最大長で確保して直接エンコード
            // UTF8は1文字あたり最大3バイト（BMPの範囲、SQLでは十分）
            var maxByteCount = _commandText.Length * 3;
            var buffer = ArrayPool<byte>.Shared.Rent(maxByteCount);
            var actualLength = Encoding.UTF8.GetBytes(_commandText, buffer);
            return (buffer, actualLength);
        }

        // パラメータありの場合：位置とバイト長を収集
        var paramPositions = new List<(int charStart, int charLength, int paramIndex)>();
        var commandSpan = _commandText.AsSpan();

        for (int i = 0; i < _parameters.Count; i++)
        {
            var param = (PgParameter)_parameters[i]!;
            var paramName = param.ParameterName;
            var pos = 0;
            while (pos < commandSpan.Length)
            {
                var idx = commandSpan.Slice(pos).IndexOf(paramName.AsSpan(), StringComparison.Ordinal);
                if (idx < 0) break;
                paramPositions.Add((pos + idx, paramName.Length, i));
                pos += idx + paramName.Length;
            }
        }

        // 出現順にソート
        paramPositions.Sort((a, b) => a.charStart.CompareTo(b.charStart));

        // 必要なバッファサイズを推定（UTF8最大長 × 文字数 + パラメータ値の余裕）
        var estimatedSize = _commandText.Length * 3 + _parameters.Count * 64;
        var resultBuffer = ArrayPool<byte>.Shared.Rent(estimatedSize);
        var resultOffset = 0;

        var lastCharPos = 0;
        foreach (var (charStart, charLength, paramIndex) in paramPositions)
        {
            // パラメータの前のテキストを書き込み
            if (charStart > lastCharPos)
            {
                var textBefore = _commandText.AsSpan(lastCharPos, charStart - lastCharPos);
                // UTF8最大長で確保（GetByteCount呼び出しを回避）
                var maxNeeded = textBefore.Length * 3;
                EnsureBufferCapacity(ref resultBuffer, resultOffset + maxNeeded);
                resultOffset += Encoding.UTF8.GetBytes(textBefore, resultBuffer.AsSpan(resultOffset));
            }

            // パラメータ値を書き込み
            var param = (PgParameter)_parameters[paramIndex]!;
            var valueWritten = FormatParameterValueUtf8(param, ref resultBuffer, ref resultOffset);
            resultOffset += valueWritten;

            lastCharPos = charStart + charLength;
        }

        // 残りのテキストを書き込み
        if (lastCharPos < _commandText.Length)
        {
            var remaining = _commandText.AsSpan(lastCharPos);
            // UTF8最大長で確保
            var maxNeeded = remaining.Length * 3;
            EnsureBufferCapacity(ref resultBuffer, resultOffset + maxNeeded);
            resultOffset += Encoding.UTF8.GetBytes(remaining, resultBuffer.AsSpan(resultOffset));
        }

        return (resultBuffer, resultOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureBufferCapacity(ref byte[] buffer, int requiredCapacity)
    {
        if (requiredCapacity <= buffer.Length) return;

        var newSize = Math.Max(buffer.Length * 2, requiredCapacity);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        buffer.AsSpan(0, buffer.Length).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(buffer);
        buffer = newBuffer;
    }

    /// <summary>
    /// パラメータ値をUTF8バイトとして直接書き込み
    /// </summary>
    private static int FormatParameterValueUtf8(PgParameter param, ref byte[] buffer, ref int currentOffset)
    {
        // NULLチェック
        if (param.Value == null || param.Value == DBNull.Value)
        {
            EnsureBufferCapacity(ref buffer, currentOffset + 4);
            "NULL"u8.CopyTo(buffer.AsSpan(currentOffset));
            return 4;
        }

        switch (param.DbType)
        {
            case DbType.Int16:
                EnsureBufferCapacity(ref buffer, currentOffset + 8);
                Utf8Formatter.TryFormat(Convert.ToInt16(param.Value), buffer.AsSpan(currentOffset), out var written16);
                return written16;

            case DbType.Int32:
                EnsureBufferCapacity(ref buffer, currentOffset + 16);
                Utf8Formatter.TryFormat(Convert.ToInt32(param.Value), buffer.AsSpan(currentOffset), out var written32);
                return written32;

            case DbType.Int64:
                EnsureBufferCapacity(ref buffer, currentOffset + 24);
                Utf8Formatter.TryFormat(Convert.ToInt64(param.Value), buffer.AsSpan(currentOffset), out var written64);
                return written64;

            case DbType.UInt16:
                EnsureBufferCapacity(ref buffer, currentOffset + 8);
                Utf8Formatter.TryFormat(Convert.ToUInt16(param.Value), buffer.AsSpan(currentOffset), out var writtenu16);
                return writtenu16;

            case DbType.UInt32:
                EnsureBufferCapacity(ref buffer, currentOffset + 16);
                Utf8Formatter.TryFormat(Convert.ToUInt32(param.Value), buffer.AsSpan(currentOffset), out var writtenu32);
                return writtenu32;

            case DbType.UInt64:
                EnsureBufferCapacity(ref buffer, currentOffset + 24);
                Utf8Formatter.TryFormat(Convert.ToUInt64(param.Value), buffer.AsSpan(currentOffset), out var writtenu64);
                return writtenu64;

            case DbType.Single:
                EnsureBufferCapacity(ref buffer, currentOffset + 32);
                Utf8Formatter.TryFormat(Convert.ToSingle(param.Value), buffer.AsSpan(currentOffset), out var writtenF);
                return writtenF;

            case DbType.Double:
                EnsureBufferCapacity(ref buffer, currentOffset + 32);
                Utf8Formatter.TryFormat(Convert.ToDouble(param.Value), buffer.AsSpan(currentOffset), out var writtenD);
                return writtenD;

            case DbType.Decimal:
                EnsureBufferCapacity(ref buffer, currentOffset + 48);
                Utf8Formatter.TryFormat(Convert.ToDecimal(param.Value), buffer.AsSpan(currentOffset), out var writtenDec);
                return writtenDec;

            case DbType.Boolean:
                if ((bool)param.Value)
                {
                    EnsureBufferCapacity(ref buffer, currentOffset + 4);
                    "TRUE"u8.CopyTo(buffer.AsSpan(currentOffset));
                    return 4;
                }
                else
                {
                    EnsureBufferCapacity(ref buffer, currentOffset + 5);
                    "FALSE"u8.CopyTo(buffer.AsSpan(currentOffset));
                    return 5;
                }

            case DbType.DateTime or DbType.DateTime2 or DbType.DateTimeOffset:
                return FormatDateTimeUtf8((DateTime)param.Value, ref buffer, ref currentOffset, includeTime: true);

            case DbType.Date:
                return FormatDateTimeUtf8((DateTime)param.Value, ref buffer, ref currentOffset, includeTime: false);

            case DbType.Time:
                return FormatTimeSpanUtf8((TimeSpan)param.Value, ref buffer, ref currentOffset);

            case DbType.Guid:
                EnsureBufferCapacity(ref buffer, currentOffset + 40);
                buffer[currentOffset] = (byte)'\'';
                Utf8Formatter.TryFormat((Guid)param.Value, buffer.AsSpan(currentOffset + 1), out var writtenGuid);
                buffer[currentOffset + 1 + writtenGuid] = (byte)'\'';
                return writtenGuid + 2;

            case DbType.Binary:
                var bytes = (byte[])param.Value;
                // '\\x' + hex + '\''
                var hexLen = bytes.Length * 2;
                EnsureBufferCapacity(ref buffer, currentOffset + hexLen + 4);
                buffer[currentOffset] = (byte)'\'';
                buffer[currentOffset + 1] = (byte)'\\';
                buffer[currentOffset + 2] = (byte)'x';
                HexEncodeLower(bytes, buffer.AsSpan(currentOffset + 3));
                buffer[currentOffset + 3 + hexLen] = (byte)'\'';
                return hexLen + 4;

            default:
                // 文字列として処理（エスケープ付き）
                return FormatStringValueUtf8(param.Value?.ToString() ?? "", ref buffer, ref currentOffset);
        }
    }

    private static int FormatDateTimeUtf8(DateTime dt, ref byte[] buffer, ref int currentOffset, bool includeTime)
    {
        // 'yyyy-MM-dd HH:mm:ss.ffffff' 最大29文字
        EnsureBufferCapacity(ref buffer, currentOffset + 32);
        var span = buffer.AsSpan(currentOffset);
        var written = 0;

        span[written++] = (byte)'\'';

        // Year
        Utf8Formatter.TryFormat(dt.Year, span.Slice(written), out var w, new StandardFormat('D', 4));
        written += w;
        span[written++] = (byte)'-';

        // Month
        Utf8Formatter.TryFormat(dt.Month, span.Slice(written), out w, new StandardFormat('D', 2));
        written += w;
        span[written++] = (byte)'-';

        // Day
        Utf8Formatter.TryFormat(dt.Day, span.Slice(written), out w, new StandardFormat('D', 2));
        written += w;

        if (includeTime)
        {
            span[written++] = (byte)' ';

            // Hour
            Utf8Formatter.TryFormat(dt.Hour, span.Slice(written), out w, new StandardFormat('D', 2));
            written += w;
            span[written++] = (byte)':';

            // Minute
            Utf8Formatter.TryFormat(dt.Minute, span.Slice(written), out w, new StandardFormat('D', 2));
            written += w;
            span[written++] = (byte)':';

            // Second
            Utf8Formatter.TryFormat(dt.Second, span.Slice(written), out w, new StandardFormat('D', 2));
            written += w;

            // Microseconds
            var ticks = dt.Ticks % TimeSpan.TicksPerSecond;
            if (ticks > 0)
            {
                span[written++] = (byte)'.';
                var micros = ticks / 10; // ticks to microseconds
                Utf8Formatter.TryFormat(micros, span.Slice(written), out w, new StandardFormat('D', 6));
                written += w;
            }
        }

        span[written++] = (byte)'\'';
        return written;
    }

    private static int FormatTimeSpanUtf8(TimeSpan ts, ref byte[] buffer, ref int currentOffset)
    {
        // 'HH:mm:ss.ffffff' 最大17文字
        EnsureBufferCapacity(ref buffer, currentOffset + 20);
        var span = buffer.AsSpan(currentOffset);
        var written = 0;

        span[written++] = (byte)'\'';

        // Hours
        Utf8Formatter.TryFormat(ts.Hours, span.Slice(written), out var w, new StandardFormat('D', 2));
        written += w;
        span[written++] = (byte)':';

        // Minutes
        Utf8Formatter.TryFormat(ts.Minutes, span.Slice(written), out w, new StandardFormat('D', 2));
        written += w;
        span[written++] = (byte)':';

        // Seconds
        Utf8Formatter.TryFormat(ts.Seconds, span.Slice(written), out w, new StandardFormat('D', 2));
        written += w;

        // Microseconds
        var ticks = ts.Ticks % TimeSpan.TicksPerSecond;
        if (ticks > 0)
        {
            span[written++] = (byte)'.';
            var micros = ticks / 10;
            Utf8Formatter.TryFormat(micros, span.Slice(written), out w, new StandardFormat('D', 6));
            written += w;
        }

        span[written++] = (byte)'\'';
        return written;
    }

    private static int FormatStringValueUtf8(string value, ref byte[] buffer, ref int currentOffset)
    {
        // エスケープが必要な文字をカウント
        var escapeCount = 0;
        foreach (var c in value)
        {
            if (c == '\'') escapeCount++;
        }

        // UTF8最大長で確保（GetByteCount呼び出しを回避）
        // 1文字最大3バイト + エスケープ分 + クォート2文字
        var maxSize = value.Length * 3 + escapeCount + 2;
        EnsureBufferCapacity(ref buffer, currentOffset + maxSize);

        var span = buffer.AsSpan(currentOffset);
        var written = 0;

        span[written++] = (byte)'\'';

        if (escapeCount == 0)
        {
            // エスケープ不要：直接エンコード
            written += Encoding.UTF8.GetBytes(value, span.Slice(written));
        }
        else
        {
            // エスケープが必要：Encoder を使用して効率的に処理
            var encoder = Encoding.UTF8.GetEncoder();
            var chars = value.AsSpan();
            var dest = span.Slice(written);
            
            foreach (var c in chars)
            {
                if (c == '\'')
                {
                    dest[0] = (byte)'\'';
                    dest[1] = (byte)'\'';
                    dest = dest.Slice(2);
                    written += 2;
                }
                else
                {
                    ReadOnlySpan<char> singleChar = stackalloc char[1] { c };
                    var bytesWritten = Encoding.UTF8.GetBytes(singleChar, dest);
                    dest = dest.Slice(bytesWritten);
                    written += bytesWritten;
                }
            }
        }

        span[written++] = (byte)'\'';
        return written;
    }

    private static void HexEncodeLower(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        const string hexChars = "0123456789abcdef";
        for (int i = 0; i < source.Length; i++)
        {
            var b = source[i];
            destination[i * 2] = (byte)hexChars[b >> 4];
            destination[i * 2 + 1] = (byte)hexChars[b & 0xF];
        }
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
/// PostgreSQLデータリーダー (DbDataReader実装) - ストリーミング対応
/// </summary>
public sealed class PgDataReader : DbDataReader
{
    private readonly PgStreamingQueryContext _context;
    private readonly PgConnection _connection;
    private readonly CommandBehavior _behavior;
    private PgColumnInfo[]? _columns;
    private int _columnCount;
    private bool _hasRows;
    private bool _firstRowRead;
    private bool _isClosed;

    // ストリームバッファへの直接参照（コピー不要）
    private byte[]? _rowBuffer;
    private int _rowBaseOffset;
    private int[]? _offsets;
    private int[]? _lengths;

    internal PgDataReader(PgStreamingQueryContext context, PgConnection connection, CommandBehavior behavior)
    {
        _context = context;
        _connection = connection;
        _behavior = behavior;
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

    private async ValueTask<bool> ReadAsyncCore(CancellationToken cancellationToken)
    {
        if (_isClosed)
            return false;

        var result = await _context.ReadNextRowAsync(this, cancellationToken).ConfigureAwait(false);

        if (result.State == PgReadState.Columns)
        {
            _columns = result.Columns;
            // オフセット/長さ配列を確保（プールから取得）
            _columnCount = _columns!.Length;
            _offsets = ArrayPool<int>.Shared.Rent(_columnCount);
            _lengths = ArrayPool<int>.Shared.Rent(_columnCount);
            result = await _context.ReadNextRowAsync(this, cancellationToken).ConfigureAwait(false);
        }

        if (result.State == PgReadState.Row)
        {
            if (!_firstRowRead)
            {
                _hasRows = true;
                _firstRowRead = true;
            }
            return true;
        }

        return false;
    }

    public override bool NextResult()
    {
        return false;
    }

    public override Task<bool> NextResultAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    // ストリームバッファへの直接参照
    internal void SetRowDataReference(byte[] buffer, int baseOffset, int columnCount)
    {
        _rowBuffer = buffer;
        _rowBaseOffset = baseOffset;
    }

    internal Span<int> GetOffsetsSpan() => _offsets.AsSpan();
    internal Span<int> GetLengthsSpan() => _lengths.AsSpan();

    public override void Close()
    {
        if (_isClosed) return;
        _isClosed = true;
        _context.Complete();

        // プールからのバッファを返却
        if (_columns != null)
        {
            ArrayPool<PgColumnInfo>.Shared.Return(_columns);
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

        // ストリームバッファへの参照をクリア（所有していないので返却不要）
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
        // ストリームバッファ内のオフセットを直接参照
        return _rowBuffer.AsSpan(_rowBaseOffset + _offsets![ordinal], length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool GetBoolean(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        return span.Length > 0 && (span[0] == 't' || span[0] == '1');
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte GetByte(int ordinal)
    {
        var span = GetValueSpan(ordinal);
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
        // PostgreSQLのtimestamp形式をパース
        var span = GetValueSpan(ordinal);
        Span<char> chars = stackalloc char[span.Length];
        var charCount = Encoding.UTF8.GetChars(span, chars);
        return DateTime.Parse(chars.Slice(0, charCount));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override decimal GetDecimal(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        Utf8Parser.TryParse(span, out decimal value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override double GetDouble(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        Utf8Parser.TryParse(span, out double value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override float GetFloat(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        Utf8Parser.TryParse(span, out float value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Guid GetGuid(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        Utf8Parser.TryParse(span, out Guid value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override short GetInt16(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        Utf8Parser.TryParse(span, out short value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetInt32(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        Utf8Parser.TryParse(span, out int value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long GetInt64(int ordinal)
    {
        var span = GetValueSpan(ordinal);
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
        for (int i = 0; i < columns.Length; i++)
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

    public override object GetValue(int ordinal)
    {
        if (IsDBNull(ordinal))
            return DBNull.Value;
        return GetString(ordinal);
    }

    public override int GetValues(object[] values)
    {
        var count = Math.Min(values.Length, _columns?.Length ?? 0);
        for (int i = 0; i < count; i++)
        {
            values[i] = IsDBNull(i) ? DBNull.Value : GetString(i);
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool IsDBNull(int ordinal)
    {
        return _lengths![ordinal] == -1;
    }

    public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken)
    {
        return Task.FromResult(IsDBNull(ordinal));
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
}

#endregion

#region Protocol Handler

/// <summary>
/// PostgreSQLプロトコルハンドラー (低レベル通信)
/// </summary>
internal sealed class PgProtocolHandler : IAsyncDisposable
{
    private const int DefaultBufferSize = 8192;
    private const int StreamBufferSize = 65536; // 64KB - より大きなバッファでシフト頻度を減らす

    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private byte[]? _readBuffer;
    private byte[]? _writeBuffer;

    private string _user = "";
    private string _password = "";

    // ストリーミング読み込み用バッファ
    private byte[] _streamBuffer = null!;
    private int _streamBufferPos;
    private int _streamBufferLen;

    public async Task ConnectAsync(string host, int port, string database, string user, string password, CancellationToken cancellationToken)
    {
        _user = user;
        _password = password;

        _tcpClient = new TcpClient { NoDelay = true };
        await _tcpClient.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
        _stream = _tcpClient.GetStream();

        _readBuffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
        _writeBuffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
        _streamBuffer = ArrayPool<byte>.Shared.Rent(StreamBufferSize);

        await SendStartupMessageAsync(database, user, cancellationToken);
        await HandleAuthenticationAsync(cancellationToken);
    }

    private async ValueTask SendStartupMessageAsync(string database, string user, CancellationToken cancellationToken)
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

        await _stream!.WriteAsync(buffer.AsMemory(0, offset), cancellationToken).ConfigureAwait(false);
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
            case 0:
                break;

            case 3:
                await SendPasswordMessageAsync(_password, cancellationToken).ConfigureAwait(false);
                break;

            case 5:
                var salt = payload.AsSpan(4, 4).ToArray();
                ComputeMd5Password(salt, out var md5Password);
                await SendPasswordMessageAsync(md5Password, cancellationToken).ConfigureAwait(false);
                break;

            case 10:
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

            await _stream!.WriteAsync(buffer.AsMemory(0, totalLength), cancellationToken).ConfigureAwait(false);
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

            await _stream!.WriteAsync(buffer.AsMemory(0, totalLength), cancellationToken).ConfigureAwait(false);
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

            await _stream!.WriteAsync(buffer.AsMemory(0, totalLength), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public async Task<PgStreamingQueryContext> ExecuteQueryStreamingAsync(byte[] sqlBuffer, int sqlLength, CancellationToken cancellationToken)
    {
        await SendQueryMessageAsync(sqlBuffer, sqlLength, cancellationToken);
        return new PgStreamingQueryContext(this, cancellationToken);
    }

    internal ValueTask<PgStreamingReadResult> ReadNextRowStreamingAsync(PgDataReader reader, CancellationToken cancellationToken)
    {
        // バッファに十分なデータがある場合は同期的に処理
        var available = _streamBufferLen - _streamBufferPos;
        if (available >= 5)
        {
            var messageType = (char)_streamBuffer[_streamBufferPos];
            var length = BinaryPrimitives.ReadInt32BigEndian(_streamBuffer.AsSpan(_streamBufferPos + 1)) - 4;

            if (available >= 5 + length)
            {
                // 同期的に処理可能
                var result = ProcessMessageSync(reader, ref available);
                if (result.HasValue)
                    return new ValueTask<PgStreamingReadResult>(result.Value);
            }
        }

        // 非同期処理が必要
        return ReadNextRowStreamingAsyncCore(reader, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PgStreamingReadResult? ProcessMessageSync(PgDataReader reader, ref int available)
    {
        while (available >= 5)
        {
            var messageType = (char)_streamBuffer[_streamBufferPos];
            var length = BinaryPrimitives.ReadInt32BigEndian(_streamBuffer.AsSpan(_streamBufferPos + 1)) - 4;

            if (available < 5 + length)
                return null; // バッファ不足

            _streamBufferPos += 5;
            available -= 5;
            var payloadOffset = _streamBufferPos;
            var payload = _streamBuffer.AsSpan(_streamBufferPos, length);

            switch (messageType)
            {
                case 'T':
                    var columns = ParseRowDescriptionArray(payload);
                    _streamBufferPos += length;
                    available -= length;
                    return PgStreamingReadResult.CreateColumns(columns);

                case 'D':
                    ParseDataRowIntoReader(payload, payloadOffset, _streamBuffer, reader);
                    _streamBufferPos += length;
                    available -= length;
                    return PgStreamingReadResult.CreateRow();

                case 'C':
                    _streamBufferPos += length;
                    available -= length;
                    // 次のメッセージを処理（ループで継続）
                    break;

                case 'Z':
                    _streamBufferPos += length;
                    available -= length;
                    return PgStreamingReadResult.CreateEnd();

                case 'E':
                    var error = ParseErrorMessage(payload);
                    _streamBufferPos += length;
                    throw new PostgresException($"クエリエラー: {error}");

                default:
                    _streamBufferPos += length;
                    available -= length;
                    // 次のメッセージを処理（ループで継続）
                    break;
            }
        }
        return null;
    }

    private async ValueTask<PgStreamingReadResult> ReadNextRowStreamingAsyncCore(PgDataReader reader, CancellationToken cancellationToken)
    {
        while (true)
        {
            // ヘッダー読み込み (1バイトタイプ + 4バイト長さ)
            await EnsureBufferedAsync(5, cancellationToken).ConfigureAwait(false);
            var messageType = (char)_streamBuffer[_streamBufferPos];
            var length = BinaryPrimitives.ReadInt32BigEndian(_streamBuffer.AsSpan(_streamBufferPos + 1)) - 4;
            _streamBufferPos += 5;

            // ペイロード読み込み
            await EnsureBufferedAsync(length, cancellationToken).ConfigureAwait(false);
            var payloadOffset = _streamBufferPos;
            var payload = _streamBuffer.AsSpan(_streamBufferPos, length);

            switch (messageType)
            {
                case 'T':
                    var columns = ParseRowDescriptionArray(payload);
                    _streamBufferPos += length;
                    return PgStreamingReadResult.CreateColumns(columns);

                case 'D':
                    ParseDataRowIntoReader(payload, payloadOffset, _streamBuffer, reader);
                    _streamBufferPos += length;
                    return PgStreamingReadResult.CreateRow();

                case 'C':
                    _streamBufferPos += length;
                    break;

                case 'Z':
                    _streamBufferPos += length;
                    return PgStreamingReadResult.CreateEnd();

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ParseDataRowIntoReader(ReadOnlySpan<byte> payload, int payloadOffset, byte[] buffer, PgDataReader reader)
    {
        var columnCount = BinaryPrimitives.ReadInt16BigEndian(payload);
        var offsets = reader.GetOffsetsSpan();
        var lengths = reader.GetLengthsSpan();

        // ストリームバッファへの直接参照を設定（コピー不要）
        reader.SetRowDataReference(buffer, payloadOffset, columnCount);

        // オフセットと長さを設定（ペイロード先頭からの相対オフセット）
        var currentOffset = 2;
        for (int i = 0; i < columnCount; i++)
        {
            var len = BinaryPrimitives.ReadInt32BigEndian(payload.Slice(currentOffset));
            currentOffset += 4;


            offsets[i] = currentOffset;
            lengths[i] = len;

            if (len > 0)
                currentOffset += len;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ValueTask EnsureBufferedAsync(int count, CancellationToken cancellationToken)
    {
        var available = _streamBufferLen - _streamBufferPos;
        if (available >= count)
            return ValueTask.CompletedTask;

        return EnsureBufferedAsyncCore(count, cancellationToken);
    }

    private async ValueTask EnsureBufferedAsyncCore(int count, CancellationToken cancellationToken)
    {
        var available = _streamBufferLen - _streamBufferPos;

        // 残りデータを先頭に移動（Spanを使用）
        if (available > 0)
        {
            _streamBuffer.AsSpan(_streamBufferPos, available).CopyTo(_streamBuffer);
        }
        _streamBufferPos = 0;
        _streamBufferLen = available;

        // 必要なサイズまでバッファを拡張
        if (count > _streamBuffer.Length)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(count);
            _streamBuffer.AsSpan(0, available).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_streamBuffer);
            _streamBuffer = newBuffer;
        }

        // 必要な分を読み込み
        while (_streamBufferLen < count)
        {
            var read = await _stream!.ReadAsync(_streamBuffer.AsMemory(_streamBufferLen), cancellationToken).ConfigureAwait(false);
            if (read == 0)
                throw new PostgresException("接続が閉じられました");
            _streamBufferLen += read;
        }
    }

    public async Task<PgQueryResult> ExecuteQueryAsync(string query, CancellationToken cancellationToken)
    {
        var result = new PgQueryResult();

        var byteCount = Encoding.UTF8.GetByteCount(query);
        var sqlBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            Encoding.UTF8.GetBytes(query, sqlBuffer);
            await SendQueryMessageAsync(sqlBuffer, byteCount, cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sqlBuffer);
        }

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

    public async Task<int> ExecuteNonQueryAsync(byte[] sqlBuffer, int sqlLength, CancellationToken cancellationToken = default)
    {
        await SendQueryMessageAsync(sqlBuffer, sqlLength, cancellationToken);

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

    public async Task<int> ExecuteNonQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        var byteCount = Encoding.UTF8.GetByteCount(query);
        var sqlBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            Encoding.UTF8.GetBytes(query, sqlBuffer);
            return await ExecuteNonQueryAsync(sqlBuffer, byteCount, cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sqlBuffer);
        }
    }

    private async ValueTask SendQueryMessageAsync(byte[] sqlBuffer, int sqlLength, CancellationToken cancellationToken)
    {
        var queryByteCount = sqlLength + 1; // +1 for null terminator
        var totalLength = 1 + 4 + queryByteCount;

        var buffer = totalLength <= _writeBuffer!.Length
            ? _writeBuffer
            : ArrayPool<byte>.Shared.Rent(totalLength);

        try
        {
            buffer[0] = (byte)'Q';
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(1), 4 + queryByteCount);
            sqlBuffer.AsSpan(0, sqlLength).CopyTo(buffer.AsSpan(5));
            buffer[5 + sqlLength] = 0; // null terminator

            await _stream!.WriteAsync(buffer.AsMemory(0, totalLength), cancellationToken).ConfigureAwait(false);
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

    private async ValueTask ReadExactAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await _stream!.ReadAsync(buffer.Slice(offset), cancellationToken).ConfigureAwait(false);
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

    private static PgColumnInfo[] ParseRowDescriptionArray(ReadOnlySpan<byte> payload)
    {
        var fieldCount = BinaryPrimitives.ReadInt16BigEndian(payload);
        var columns = ArrayPool<PgColumnInfo>.Shared.Rent(fieldCount);
        var offset = 2;

        for (int i = 0; i < fieldCount; i++)
        {
            var nameEnd = payload.Slice(offset).IndexOf((byte)0);
            var name = Encoding.UTF8.GetString(payload.Slice(offset, nameEnd));
            offset += nameEnd + 1;

            var typeOid = BinaryPrimitives.ReadInt32BigEndian(payload.Slice(offset + 6));
            offset += 18;

            columns[i] = new PgColumnInfo(name, typeOid);
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

        if (_streamBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(_streamBuffer);
            _streamBuffer = null!;
        }
    }
}


internal readonly record struct PgColumnInfo(string Name, int TypeOid);

internal sealed class PgQueryResult
{
    public List<PgColumnInfo> Columns { get; set; } = [];
    public List<List<string?>> Rows { get; set; } = [];
}

/// <summary>
/// 読み込み状態
/// </summary>
internal enum PgReadState
{
    Columns,
    Row,
    End
}

/// <summary>
/// ストリーミング読み込み結果（軽量版）
/// </summary>
internal readonly struct PgStreamingReadResult
{
    public readonly PgReadState State;
    public readonly PgColumnInfo[]? Columns;

    private PgStreamingReadResult(PgReadState state, PgColumnInfo[]? columns = null)
    {
        State = state;
        Columns = columns;
    }

    public static PgStreamingReadResult CreateColumns(PgColumnInfo[] columns) => new(PgReadState.Columns, columns);
    public static PgStreamingReadResult CreateRow() => new(PgReadState.Row);
    public static PgStreamingReadResult CreateEnd() => new(PgReadState.End);
}

/// <summary>
/// ストリーミングクエリコンテキスト
/// </summary>
internal sealed class PgStreamingQueryContext
{
    private readonly PgProtocolHandler _handler;
    private bool _completed;

    public PgStreamingQueryContext(PgProtocolHandler handler, CancellationToken cancellationToken)
    {
        _handler = handler;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<PgStreamingReadResult> ReadNextRowAsync(PgDataReader reader, CancellationToken cancellationToken)
    {
        if (_completed)
            return new ValueTask<PgStreamingReadResult>(PgStreamingReadResult.CreateEnd());

        return ReadNextRowAsyncCore(reader, cancellationToken);
    }

    private async ValueTask<PgStreamingReadResult> ReadNextRowAsyncCore(PgDataReader reader, CancellationToken cancellationToken)
    {
        var result = await _handler.ReadNextRowStreamingAsync(reader, cancellationToken).ConfigureAwait(false);
        if (result.State == PgReadState.End)
            _completed = true;

        return result;
    }

    public void Complete()
    {
        _completed = true;
    }
}

#endregion
