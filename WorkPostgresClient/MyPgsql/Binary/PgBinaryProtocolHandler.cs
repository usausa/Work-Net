using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Data;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MyPgsql.Binary;

/// <summary>
/// PostgreSQLプロトコルハンドラー - バイナリプロトコル版
/// Extended Query Protocolを使用
/// </summary>
internal sealed partial class PgBinaryProtocolHandler : IAsyncDisposable
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
    /// パラメーター名を抽出する正規表現
    /// </summary>
    [GeneratedRegex(@"@\w+", RegexOptions.Compiled)]
    private static partial Regex ParameterNameRegex();

    /// <summary>
    /// SQL内のパラメーター名をPostgreSQL位置パラメーター ($1, $2, ...) に変換
    /// </summary>
    private static (string convertedSql, List<string> parameterOrder) ConvertToPositionalParameters(string sql, IReadOnlyList<PgBinaryParameter> parameters)
    {
        var parameterOrder = new List<string>();
        var paramNameToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var convertedSql = ParameterNameRegex().Replace(sql, match =>
        {
            var paramName = match.Value;
            if (!paramNameToIndex.TryGetValue(paramName, out var index))
            {
                index = parameterOrder.Count + 1;
                parameterOrder.Add(paramName);
                paramNameToIndex[paramName] = index;
            }
            return $"${index}";
        });

        return (convertedSql, parameterOrder);
    }

    /// <summary>
    /// パラメーターをバイナリ形式でエンコード
    /// </summary>
    private static (byte[] data, int length, int oid) EncodeParameter(PgBinaryParameter parameter)
    {
        var value = parameter.Value;
        if (value == null || value == DBNull.Value)
        {
            return (Array.Empty<byte>(), -1, 0); // NULL
        }

        switch (parameter.DbType)
        {
            case DbType.Int16:
                {
                    var buffer = new byte[2];
                    BinaryPrimitives.WriteInt16BigEndian(buffer, Convert.ToInt16(value));
                    return (buffer, 2, 21); // int2
                }
            case DbType.Int32:
                {
                    var buffer = new byte[4];
                    BinaryPrimitives.WriteInt32BigEndian(buffer, Convert.ToInt32(value));
                    return (buffer, 4, 23); // int4
                }
            case DbType.Int64:
                {
                    var buffer = new byte[8];
                    BinaryPrimitives.WriteInt64BigEndian(buffer, Convert.ToInt64(value));
                    return (buffer, 8, 20); // int8
                }
            case DbType.Single:
                {
                    var buffer = new byte[4];
                    BinaryPrimitives.WriteInt32BigEndian(buffer, BitConverter.SingleToInt32Bits(Convert.ToSingle(value)));
                    return (buffer, 4, 700); // float4
                }
            case DbType.Double:
                {
                    var buffer = new byte[8];
                    BinaryPrimitives.WriteInt64BigEndian(buffer, BitConverter.DoubleToInt64Bits(Convert.ToDouble(value)));
                    return (buffer, 8, 701); // float8
                }
            case DbType.Boolean:
                {
                    var buffer = new byte[1];
                    buffer[0] = (bool)value ? (byte)1 : (byte)0;
                    return (buffer, 1, 16); // bool
                }
            case DbType.DateTime or DbType.DateTime2 or DbType.DateTimeOffset:
                {
                    // PostgreSQLのtimestamp形式: 2000-01-01からのマイクロ秒数
                    var dt = value is DateTimeOffset dto ? dto.UtcDateTime : Convert.ToDateTime(value);
                    var postgresEpoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    var microseconds = (dt.ToUniversalTime() - postgresEpoch).Ticks / 10;
                    var buffer = new byte[8];
                    BinaryPrimitives.WriteInt64BigEndian(buffer, microseconds);
                    return (buffer, 8, 1114); // timestamp
                }
            case DbType.Date:
                {
                    // PostgreSQLのdate形式: 2000-01-01からの日数
                    var dt = Convert.ToDateTime(value);
                    var postgresEpoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    var days = (int)(dt.Date - postgresEpoch).TotalDays;
                    var buffer = new byte[4];
                    BinaryPrimitives.WriteInt32BigEndian(buffer, days);
                    return (buffer, 4, 1082); // date
                }
            case DbType.Guid:
                {
                    var guid = (Guid)value;
                    var buffer = guid.ToByteArray();
                    // PostgreSQLのUUID形式に変換（バイトオーダーの調整）
                    // .NET Guidはリトルエンディアンなのでビッグエンディアンに変換
                    Array.Reverse(buffer, 0, 4);
                    Array.Reverse(buffer, 4, 2);
                    Array.Reverse(buffer, 6, 2);
                    return (buffer, 16, 2950); // uuid
                }
            case DbType.Binary:
                {
                    var bytes = (byte[])value;
                    return (bytes, bytes.Length, 17); // bytea
                }
            default:
                {
                    // テキスト形式としてエンコード
                    var strValue = value?.ToString() ?? "";
                    var bytes = Encoding.UTF8.GetBytes(strValue);
                    return (bytes, bytes.Length, 0); // text (OID 0 means server should infer)
                }
        }
    }

    /// <summary>
    /// Extended Query Protocol でパラメーター付きクエリを送信（バイナリフォーマット）
    /// パラメーターはサーバー側で展開
    /// </summary>
    public async ValueTask SendExtendedQueryWithParametersAsync(
        string sql,
        IReadOnlyList<PgBinaryParameter> parameters,
        CancellationToken cancellationToken)
    {
        // SQL内のパラメーター名を位置パラメーターに変換
        var (convertedSql, parameterOrder) = ConvertToPositionalParameters(sql, parameters);
        var sqlBytes = Encoding.UTF8.GetBytes(convertedSql);

        // パラメーターをエンコード
        var parameterLookup = parameters.ToDictionary(p => p.ParameterName, StringComparer.OrdinalIgnoreCase);
        var encodedParams = new List<(byte[] data, int length, int oid)>();
        foreach (var paramName in parameterOrder)
        {
            if (parameterLookup.TryGetValue(paramName, out var param))
            {
                encodedParams.Add(EncodeParameter(param));
            }
            else
            {
                throw new InvalidOperationException($"パラメーター '{paramName}' が見つかりません");
            }
        }

        var paramCount = encodedParams.Count;

        // バッファサイズ計算
        // Parse: 1 + 4 + 1(name) + sqlLen + 1 + 2(param count) + 4 * paramCount (param OIDs)
        var parsePayloadLen = 1 + sqlBytes.Length + 1 + 2 + 4 * paramCount;
        var parseLen = 1 + 4 + parsePayloadLen;

        // Bind: 1 + 4 + 1(portal) + 1(stmt) + 2(format count) + 2 * paramCount (format codes)
        //       + 2(param count) + sum(4 + param data length) + 2(result format count) + 2(result format)
        var bindPayloadLen = 1 + 1 + 2 + 2 * paramCount + 2;
        foreach (var (data, length, _) in encodedParams)
        {
            bindPayloadLen += 4 + (length > 0 ? length : 0);
        }
        bindPayloadLen += 2 + 2; // result format count + result format
        var bindLen = 1 + 4 + bindPayloadLen;

        // Describe: 1 + 4 + 1(type) + 1(name)
        var describeLen = 1 + 4 + 1 + 1;

        // Execute: 1 + 4 + 1(portal) + 4(max rows)
        var executeLen = 1 + 4 + 1 + 4;

        // Sync: 1 + 4
        var syncLen = 1 + 4;

        var totalLen = parseLen + bindLen + describeLen + executeLen + syncLen;

        var buffer = ArrayPool<byte>.Shared.Rent(totalLen);
        try
        {
            var offset = 0;

            // Parse message ('P')
            buffer[offset++] = (byte)'P';
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset), parsePayloadLen + 4);
            offset += 4;
            buffer[offset++] = 0; // unnamed statement
            sqlBytes.CopyTo(buffer.AsSpan(offset));
            offset += sqlBytes.Length;
            buffer[offset++] = 0; // null terminator
            BinaryPrimitives.WriteInt16BigEndian(buffer.AsSpan(offset), (short)paramCount);
            offset += 2;
            // パラメーターOIDを書き込み
            foreach (var (_, _, oid) in encodedParams)
            {
                BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset), oid);
                offset += 4;
            }

            // Bind message ('B')
            buffer[offset++] = (byte)'B';
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset), bindPayloadLen + 4);
            offset += 4;
            buffer[offset++] = 0; // unnamed portal
            buffer[offset++] = 0; // unnamed statement

            // パラメーターフォーマットコード
            BinaryPrimitives.WriteInt16BigEndian(buffer.AsSpan(offset), (short)paramCount);
            offset += 2;
            foreach (var (_, _, oid) in encodedParams)
            {
                // バイナリフォーマット (1) を指定
                BinaryPrimitives.WriteInt16BigEndian(buffer.AsSpan(offset), (short)1);
                offset += 2;
            }

            // パラメーター値
            BinaryPrimitives.WriteInt16BigEndian(buffer.AsSpan(offset), (short)paramCount);
            offset += 2;
            foreach (var (data, length, _) in encodedParams)
            {
                BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset), length);
                offset += 4;
                if (length > 0)
                {
                    data.CopyTo(buffer.AsSpan(offset));
                    offset += length;
                }
            }

            // 結果フォーマット
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

    /// <summary>
    /// Extended Query Protocol でクエリを送信（パラメーターなし、バイナリフォーマット）
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

    /// <summary>
    /// パラメーター付きでNonQueryを実行
    /// </summary>
    public async Task<int> ExecuteNonQueryWithParametersAsync(
        string sql,
        IReadOnlyList<PgBinaryParameter> parameters,
        CancellationToken cancellationToken)
    {
        await SendExtendedQueryWithParametersAsync(sql, parameters, cancellationToken);
        return await WaitForCommandCompleteAsync(cancellationToken);
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, CancellationToken cancellationToken)
    {
        await SendExtendedQueryAsync(sql, cancellationToken);
        return await WaitForCommandCompleteAsync(cancellationToken);
    }

    private async Task<int> WaitForCommandCompleteAsync(CancellationToken cancellationToken)
    {
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
                    throw new PostgresBinaryException($"クエリエラー: {error}");

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
                    throw new PostgresBinaryException($"クエリエラー: {error}");

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
                throw new PostgresBinaryException("接続が閉じられました");

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
                    throw new PostgresBinaryException($"認証エラー: {error}");

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
                throw new PostgresBinaryException($"未対応の認証方式: {authType}");
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
            throw new PostgresBinaryException($"SASL認証エラー: {error}");
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
            throw new PostgresBinaryException("SCRAM認証失敗");
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
                throw new PostgresBinaryException("接続が閉じられました");
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
