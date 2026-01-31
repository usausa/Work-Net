using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace RawPgsql;

/// <summary>
/// PostgreSQL例外
/// </summary>
public sealed class RawPostgresException(string message) : Exception(message);

/// <summary>
/// DbConnection/DbCommand/DbDataReaderを一切使用しない、直接PostgreSQLプロトコルを実装した軽量クライアント
/// ADO.NETのオーバーヘッドを排除し、最高性能を目指す
/// </summary>
public sealed class RawPgClient : IAsyncDisposable
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

    public async Task ConnectAsync(string host, int port, string database, string user, string password, CancellationToken cancellationToken = default)
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

    public static async Task<RawPgClient> CreateAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var client = new RawPgClient();
        var (host, port, database, user, password) = ParseConnectionString(connectionString);
        await client.ConnectAsync(host, port, database, user, password, cancellationToken);
        return client;
    }

    private static (string host, int port, string database, string user, string password) ParseConnectionString(string connectionString)
    {
        var host = "localhost";
        var port = 5432;
        var database = "";
        var user = "";
        var password = "";

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0) continue;

            var key = part[..idx].Trim().ToLowerInvariant();
            var value = part[(idx + 1)..].Trim();

            switch (key)
            {
                case "host" or "server":
                    host = value;
                    break;
                case "port":
                    port = int.Parse(value);
                    break;
                case "database" or "db":
                    database = value;
                    break;
                case "username" or "user" or "uid":
                    user = value;
                    break;
                case "password" or "pwd":
                    password = value;
                    break;
            }
        }

        return (host, port, database, user, password);
    }

    /// <summary>
    /// SELECTクエリを実行し、結果をRawResultReaderで読み取る
    /// </summary>
    public async Task<RawResultReader> ExecuteQueryAsync(string sql, CancellationToken cancellationToken = default)
    {
        await SendQueryMessageAsync(sql, cancellationToken).ConfigureAwait(false);
        return new RawResultReader(this, cancellationToken);
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
                    throw new RawPostgresException($"認証エラー: {error}");

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
                throw new RawPostgresException($"未対応の認証方式: {authType}");
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
            throw new RawPostgresException($"SASL認証エラー: {error}");
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
            throw new RawPostgresException("SCRAM認証失敗");
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

    internal async ValueTask SendQueryMessageAsync(string sql, CancellationToken cancellationToken)
    {
        var sqlByteCount = Encoding.UTF8.GetByteCount(sql);
        var queryByteCount = sqlByteCount + 1;
        var totalLength = 1 + 4 + queryByteCount;

        var buffer = totalLength <= _writeBuffer!.Length
            ? _writeBuffer
            : ArrayPool<byte>.Shared.Rent(totalLength);

        try
        {
            buffer[0] = (byte)'Q';
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(1), 4 + queryByteCount);
            Encoding.UTF8.GetBytes(sql, buffer.AsSpan(5));
            buffer[5 + sqlByteCount] = 0;

            await _socket!.SendAsync(buffer.AsMemory(0, totalLength), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (!ReferenceEquals(buffer, _writeBuffer))
                ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    internal async Task<(char type, byte[] payload, int length)> ReadMessageAsync(CancellationToken cancellationToken)
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
                throw new RawPostgresException("接続が閉じられました");
            offset += read;
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
                throw new RawPostgresException("接続が閉じられました");

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

    internal byte[] StreamBuffer => _streamBuffer;
    internal ref int StreamBufferPos => ref _streamBufferPos;
    internal ref int StreamBufferLen => ref _streamBufferLen;

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

/// <summary>
/// カラム情報（軽量構造体）
/// </summary>
public readonly record struct RawColumnInfo(string Name, int TypeOid);

/// <summary>
/// クエリ結果を直接読み取るリーダー（DbDataReaderを使用しない）
/// </summary>
public sealed class RawResultReader : IAsyncDisposable
{
    private readonly RawPgClient _client;
    private readonly CancellationToken _cancellationToken;
    private RawColumnInfo[]? _columns;
    private int _columnCount;
    private bool _completed;

    // 行データへの直接参照
    private byte[]? _rowBuffer;
    private int _rowBaseOffset;
    private int[]? _offsets;
    private int[]? _lengths;

    internal RawResultReader(RawPgClient client, CancellationToken cancellationToken)
    {
        _client = client;
        _cancellationToken = cancellationToken;
    }

    public int FieldCount => _columnCount;

    /// <summary>
    /// 次の行を読み取る
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> ReadAsync()
    {
        if (_completed)
            return new ValueTask<bool>(false);

        // 同期パス: バッファに十分なデータがある場合
        var result = TryReadSync();
        if (result.HasValue)
            return new ValueTask<bool>(result.Value);

        // 非同期パス
        return ReadAsyncCore();
    }

    /// <summary>
    /// バッファ内のデータで同期的に読み取りを試行
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool? TryReadSync()
    {
        var buffer = _client.StreamBuffer;
        ref var pos = ref _client.StreamBufferPos;
        var len = _client.StreamBufferLen;

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
                    throw new RawPostgresException($"クエリエラー: {error}");

                default:
                    pos += payloadLength;
                    break;
            }
        }
    }

    /// <summary>
    /// 非同期読み取り（バッファ不足時）
    /// </summary>
    private async ValueTask<bool> ReadAsyncCore()
    {
        while (true)
        {
            // ヘッダーを読み取り
            await _client.EnsureBufferedAsync(5, _cancellationToken).ConfigureAwait(false);

            var buffer = _client.StreamBuffer;
            var pos = _client.StreamBufferPos;

            var messageType = (char)buffer[pos];
            var payloadLength = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(pos + 1)) - 4;

            // ヘッダー+ペイロードを一度に確保
            await _client.EnsureBufferedAsync(5 + payloadLength, _cancellationToken).ConfigureAwait(false);

            // EnsureBufferedAsync後にバッファが変わる可能性があるため再取得
            buffer = _client.StreamBuffer;
            pos = _client.StreamBufferPos;

            _client.StreamBufferPos = pos + 5;
            var payloadOffset = pos + 5;
            var payload = buffer.AsSpan(payloadOffset, payloadLength);

            switch (messageType)
            {
                case 'D': // DataRow
                    ParseDataRow(payload, payloadOffset);
                    _client.StreamBufferPos += payloadLength;
                    return true;

                case 'T': // RowDescription
                    ParseRowDescription(payload);
                    _client.StreamBufferPos += payloadLength;
                    // 同期パスで継続を試行
                    var syncResult = TryReadSync();
                    if (syncResult.HasValue)
                        return syncResult.Value;
                    break;

                case 'C': // CommandComplete
                    _client.StreamBufferPos += payloadLength;
                    // 同期パスで継続を試行
                    syncResult = TryReadSync();
                    if (syncResult.HasValue)
                        return syncResult.Value;
                    break;

                case 'Z': // ReadyForQuery
                    _client.StreamBufferPos += payloadLength;
                    _completed = true;
                    return false;

                case 'E': // Error
                    var error = ParseErrorMessage(payload);
                    _client.StreamBufferPos += payloadLength;
                    throw new RawPostgresException($"クエリエラー: {error}");

                default:
                    _client.StreamBufferPos += payloadLength;
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
        _columns = ArrayPool<RawColumnInfo>.Shared.Rent(fieldCount);
        _offsets = ArrayPool<int>.Shared.Rent(fieldCount);
        _lengths = ArrayPool<int>.Shared.Rent(fieldCount);

        var offset = 2;
        for (int i = 0; i < fieldCount; i++)
        {
            var nameEnd = payload.Slice(offset).IndexOf((byte)0);
            var name = Encoding.UTF8.GetString(payload.Slice(offset, nameEnd));
            offset += nameEnd + 1;

            var typeOid = BinaryPrimitives.ReadInt32BigEndian(payload.Slice(offset + 6));
            offset += 18;

            _columns[i] = new RawColumnInfo(name, typeOid);
        }
    }

    private void ParseDataRow(ReadOnlySpan<byte> payload, int payloadOffset)
    {
        var columnCount = BinaryPrimitives.ReadInt16BigEndian(payload);
        _rowBuffer = _client.StreamBuffer;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> GetValueSpan(int ordinal)
    {
        var length = _lengths![ordinal];
        if (length == -1)
            throw new InvalidCastException("値がNULLです");
        return _rowBuffer.AsSpan(_rowBaseOffset + _offsets![ordinal], length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsDBNull(int ordinal) => _lengths![ordinal] == -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetInt32(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        Utf8Parser.TryParse(span, out int value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetInt64(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        Utf8Parser.TryParse(span, out long value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetBoolean(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        return span.Length > 0 && (span[0] == 't' || span[0] == '1');
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetString(int ordinal)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime GetDateTime(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        Span<char> chars = stackalloc char[span.Length];
        var charCount = Encoding.UTF8.GetChars(span, chars);
        return DateTime.Parse(chars.Slice(0, charCount));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public decimal GetDecimal(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        Utf8Parser.TryParse(span, out decimal value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetDouble(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        Utf8Parser.TryParse(span, out double value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetFloat(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        Utf8Parser.TryParse(span, out float value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short GetInt16(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        Utf8Parser.TryParse(span, out short value, out _);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Guid GetGuid(int ordinal)
    {
        var span = GetValueSpan(ordinal);
        Utf8Parser.TryParse(span, out Guid value, out _);
        return value;
    }

    public string GetName(int ordinal) => _columns![ordinal].Name;

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

    public ValueTask DisposeAsync()
    {
        if (_columns != null)
        {
            ArrayPool<RawColumnInfo>.Shared.Return(_columns);
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

        return ValueTask.CompletedTask;
    }
}
