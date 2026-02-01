using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;

namespace MyPgsql.Binary;

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
                    throw new PostgresBinaryException($"クエリエラー: {error}");

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
                    throw new PostgresBinaryException($"クエリエラー: {error}");

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
        CloseAsync().GetAwaiter().GetResult();
    }

    public override async Task CloseAsync()
    {
        if (_isClosed) return;
        _isClosed = true;

        // 未完了の場合、ReadyForQueryまで読み飛ばす
        if (!_completed)
        {
            await DrainToReadyForQueryAsync().ConfigureAwait(false);
        }

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

    /// <summary>
    /// ReadyForQuery ('Z') メッセージまで全てのメッセージを読み飛ばす
    /// </summary>
    private async ValueTask DrainToReadyForQueryAsync()
    {
        while (true)
        {
            await _protocol.EnsureBufferedAsync(5, _cancellationToken).ConfigureAwait(false);

            var buffer = _protocol.StreamBuffer;
            var pos = _protocol.StreamBufferPos;

            var messageType = (char)buffer[pos];
            var payloadLength = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(pos + 1)) - 4;

            await _protocol.EnsureBufferedAsync(5 + payloadLength, _cancellationToken).ConfigureAwait(false);

            _protocol.StreamBufferPos += 5 + payloadLength;

            if (messageType == 'Z')
            {
                _completed = true;
                return;
            }

            if (messageType == 'E')
            {
                // エラーは無視して継続（すでにcloseしているため）
            }
        }
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
        System.Buffers.Text.Utf8Parser.TryParse(span, out byte value, out _);
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
        System.Buffers.Text.Utf8Parser.TryParse(span, out decimal value, out _);
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
        System.Buffers.Text.Utf8Parser.TryParse(span, out double value, out _);
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
        System.Buffers.Text.Utf8Parser.TryParse(span, out float value, out _);
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
        System.Buffers.Text.Utf8Parser.TryParse(span, out Guid value, out _);
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
        System.Buffers.Text.Utf8Parser.TryParse(span, out short value, out _);
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
        System.Buffers.Text.Utf8Parser.TryParse(span, out int value, out _);
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
        System.Buffers.Text.Utf8Parser.TryParse(span, out long value, out _);
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
