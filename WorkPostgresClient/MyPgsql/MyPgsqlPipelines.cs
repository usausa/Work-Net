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

namespace MyPgsql.Pipelines;

/// <summary>
/// Socket直接操作による PostgreSQL プロトコルハンドラー
/// 低レイテンシ、GC削減を目指した実装（NetworkStreamバイパス版）
/// </summary>
internal sealed class PgPipeProtocolHandler : IAsyncDisposable
{
    private const int DefaultBufferSize = 8192;
    private const int StreamBufferSize = 65536; // 64KB

    private Socket? _socket;

    private string _user = "";
    private string _password = "";

    // バッファ（ArrayPoolから借用）
    private byte[]? _writeBuffer;
    private byte[]? _readBuffer;

    // ストリーミング読み込み用バッファ
    private byte[] _streamBuffer = null!;
    private int _streamBufferPos;
    private int _streamBufferLen;



    public async Task ConnectAsync(string host, int port, string database, string user, string password, CancellationToken cancellationToken)
    {
        _user = user;
        _password = password;

        // Socket を直接使用
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true,  // Nagle アルゴリズム無効化
            ReceiveBufferSize = 65536,
            SendBufferSize = 65536
        };

        await _socket.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);

        // ストリーム用バッファを確保
        _streamBuffer = ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        _writeBuffer = ArrayPool<byte>.Shared.Rent(8192);


        _readBuffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);

        await SendStartupMessageAsync(database, user, cancellationToken).ConfigureAwait(false);
        await HandleAuthenticationAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SendStartupMessageAsync(string database, string user, CancellationToken cancellationToken)
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

    private async Task HandleAuthenticationAsync(CancellationToken cancellationToken)
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
                    var error = ParseErrorMessageSpan(payload.AsSpan(0, payloadLength));
                    ReturnBuffer(payload);
                    throw new PostgresException($"認証エラー: {error}");

                default:
                    ReturnBuffer(payload);
                    break;
            }
        }
    }

    private async Task HandleAuthResponseAsync(byte[] payloadBuffer, int payloadLength, CancellationToken cancellationToken)
    {
        var payload = payloadBuffer.AsSpan(0, payloadLength);
        var authType = BinaryPrimitives.ReadInt32BigEndian(payload);

        switch (authType)
        {
            case 0: // AuthenticationOk
                break;

            case 3: // AuthenticationCleartextPassword
                await SendPasswordMessageAsync(_password, cancellationToken).ConfigureAwait(false);
                break;

            case 5: // AuthenticationMD5Password
                var salt = payload.Slice(4, 4).ToArray();
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

    private async Task HandleSaslAuthAsync(CancellationToken cancellationToken)
    {
        var clientNonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
        var clientFirstBare = $"n=,r={clientNonce}";
        var clientFirstMessage = $"n,,{clientFirstBare}";

        await SendSaslInitialResponseAsync(clientFirstMessage, cancellationToken).ConfigureAwait(false);

        var (msgType1, serverFirstPayload, serverFirstLength) = await ReadMessageAsync(cancellationToken).ConfigureAwait(false);
        if (msgType1 == 'E')
        {
            var error = ParseErrorMessageSpan(serverFirstPayload.AsSpan(0, serverFirstLength));
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

            await _socket!.SendAsync(buffer.AsMemory(0, totalLength), cancellationToken).ConfigureAwait(false);
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

            await _socket!.SendAsync(buffer.AsMemory(0, totalLength), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        await SendQueryMessageAsync(query, cancellationToken).ConfigureAwait(false);

        var affectedRows = 0;

        while (true)
        {
            var (messageType, payload, payloadLength) = await ReadMessageAsync(cancellationToken).ConfigureAwait(false);

            switch (messageType)
            {
                case 'C': // CommandComplete
                    affectedRows = ParseCommandCompleteSpan(payload.AsSpan(0, payloadLength));
                    ReturnBuffer(payload);
                    break;

                case 'Z': // ReadyForQuery
                    ReturnBuffer(payload);
                    return affectedRows;

                case 'E':
                    var error = ParseErrorMessageSpan(payload.AsSpan(0, payloadLength));
                    ReturnBuffer(payload);
                    throw new PostgresException($"クエリエラー: {error}");

                default:
                    ReturnBuffer(payload);
                    break;
            }
        }
    }

    public async Task<PgPipeStreamingQueryContext> ExecuteQueryStreamingAsync(string query, CancellationToken cancellationToken)
    {
        await SendQueryMessageAsync(query, cancellationToken).ConfigureAwait(false);
        return new PgPipeStreamingQueryContext(this, cancellationToken);
    }

    internal ValueTask<PgPipeStreamingReadResult> ReadNextRowStreamingAsync(PgPipeDataReader reader, CancellationToken cancellationToken)
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
                    return new ValueTask<PgPipeStreamingReadResult>(result.Value);
            }
        }

        // 非同期処理が必要
        return ReadNextRowStreamingAsyncCore(reader, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PgPipeStreamingReadResult? ProcessMessageSync(PgPipeDataReader reader, ref int available)
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
                    var columns = ParseRowDescriptionArraySpan(payload);
                    _streamBufferPos += length;
                    available -= length;
                    return PgPipeStreamingReadResult.CreateColumns(columns);

                case 'D':
                    ParseDataRowIntoReaderSpan(payload, payloadOffset, _streamBuffer, reader);
                    _streamBufferPos += length;
                    available -= length;
                    return PgPipeStreamingReadResult.CreateRow();

                case 'C':
                    _streamBufferPos += length;
                    available -= length;
                    break;

                case 'Z':
                    _streamBufferPos += length;
                    available -= length;
                    return PgPipeStreamingReadResult.CreateEnd();

                case 'E':
                    var error = ParseErrorMessageSpan(payload);
                    _streamBufferPos += length;
                    throw new PostgresException($"クエリエラー: {error}");

                default:
                    _streamBufferPos += length;
                    available -= length;
                    break;
            }
        }
        return null;
    }

    private async ValueTask<PgPipeStreamingReadResult> ReadNextRowStreamingAsyncCore(PgPipeDataReader reader, CancellationToken cancellationToken)
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
                    var columns = ParseRowDescriptionArraySpan(payload);
                    _streamBufferPos += length;
                    return PgPipeStreamingReadResult.CreateColumns(columns);

                case 'D':
                    ParseDataRowIntoReaderSpan(payload, payloadOffset, _streamBuffer, reader);
                    _streamBufferPos += length;
                    return PgPipeStreamingReadResult.CreateRow();

                case 'C':
                    _streamBufferPos += length;
                    break;

                case 'Z':
                    _streamBufferPos += length;
                    return PgPipeStreamingReadResult.CreateEnd();

                case 'E':
                    var error = ParseErrorMessageSpan(payload);
                    _streamBufferPos += length;
                    throw new PostgresException($"クエリエラー: {error}");

                default:
                    _streamBufferPos += length;
                    break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ParseDataRowIntoReaderSpan(ReadOnlySpan<byte> payload, int payloadOffset, byte[] buffer, PgPipeDataReader reader)
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

        // 残りデータを先頭に移動
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
            var read = await _socket!.ReceiveAsync(_streamBuffer.AsMemory(_streamBufferLen), cancellationToken).ConfigureAwait(false);
            if (read == 0)
                throw new PostgresException("接続が閉じられました");
            _streamBufferLen += read;
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

            await _socket!.SendAsync(buffer.AsMemory(0, totalLength), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (!ReferenceEquals(buffer, _writeBuffer))
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

    private async Task ReadExactAsync(Memory<byte> buffer, CancellationToken cancellationToken)
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

    private static PgColumnInfo[] ParseRowDescriptionArraySpan(ReadOnlySpan<byte> payload)
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

    private static int ParseCommandCompleteSpan(ReadOnlySpan<byte> payload)
    {
        // NULL終端を除いた文字列を取得
        var end = payload.IndexOf((byte)0);
        if (end >= 0)
            payload = payload.Slice(0, end);


        Span<char> chars = stackalloc char[payload.Length];
        var charCount = Encoding.UTF8.GetChars(payload, chars);
        var message = chars.Slice(0, charCount);

        var lastSpace = message.LastIndexOf(' ');
        if (lastSpace >= 0 && int.TryParse(message.Slice(lastSpace + 1), out var count))
            return count;
        return 0;
    }

    private static string ParseErrorMessageSpan(ReadOnlySpan<byte> payload)
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
    private static int WriteNullTerminatedString(Span<byte> buffer, ReadOnlySpan<byte> value)
    {
        value.CopyTo(buffer);
        buffer[value.Length] = 0;
        return value.Length + 1;
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

    public async ValueTask DisposeAsync()
    {
        if (_socket != null)
        {
            // Terminate メッセージを送信（_writeBufferを再利用）
            if (_writeBuffer != null)
            {
                _writeBuffer[0] = (byte)'X';
                BinaryPrimitives.WriteInt32BigEndian(_writeBuffer.AsSpan(1), 4);

                try
                {
                    await _socket.SendAsync(_writeBuffer.AsMemory(0, 5)).ConfigureAwait(false);
                }
                catch
                {
                    // 無視
                }
            }

            _socket.Dispose();
        }

        if (_writeBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(_writeBuffer);
            _writeBuffer = null;
        }

        if (_readBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(_readBuffer);
            _readBuffer = null;
        }

        if (_streamBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(_streamBuffer);
            _streamBuffer = null!;
        }
    }
}

/// <summary>
/// ストリーミング読み込み結果
/// </summary>
internal readonly struct PgPipeStreamingReadResult
{
    public readonly PgReadState State;
    public readonly PgColumnInfo[]? Columns;

    private PgPipeStreamingReadResult(PgReadState state, PgColumnInfo[]? columns = null)
    {
        State = state;
        Columns = columns;
    }

    public static PgPipeStreamingReadResult CreateColumns(PgColumnInfo[] columns) => new(PgReadState.Columns, columns);
    public static PgPipeStreamingReadResult CreateRow() => new(PgReadState.Row);
    public static PgPipeStreamingReadResult CreateEnd() => new(PgReadState.End);
}

/// <summary>
/// ストリーミングクエリコンテキスト
/// </summary>
internal sealed class PgPipeStreamingQueryContext
{
    private readonly PgPipeProtocolHandler _handler;
    private bool _completed;

    public PgPipeStreamingQueryContext(PgPipeProtocolHandler handler, CancellationToken cancellationToken)
    {
        _handler = handler;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<PgPipeStreamingReadResult> ReadNextRowAsync(PgPipeDataReader reader, CancellationToken cancellationToken)
    {
        if (_completed)
            return new ValueTask<PgPipeStreamingReadResult>(PgPipeStreamingReadResult.CreateEnd());

        return ReadNextRowAsyncCore(reader, cancellationToken);
    }


    private async ValueTask<PgPipeStreamingReadResult> ReadNextRowAsyncCore(PgPipeDataReader reader, CancellationToken cancellationToken)
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

/// <summary>
/// PostgreSQL 接続 (Pipelines版)
/// </summary>
public sealed class PgPipeConnection : DbConnection
{
    private PgConnectionStringBuilder _connectionStringBuilder = new();
    private string _connectionString = "";
    private ConnectionState _state = ConnectionState.Closed;
    private PgPipeProtocolHandler? _protocol;

    public PgPipeConnection() { }

    public PgPipeConnection(string connectionString)
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

    internal PgPipeProtocolHandler Protocol => _protocol ?? throw new InvalidOperationException("接続が開かれていません");

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
            _protocol = new PgPipeProtocolHandler();
            await _protocol.ConnectAsync(
                _connectionStringBuilder.Host,
                _connectionStringBuilder.Port,
                _connectionStringBuilder.Database,
                _connectionStringBuilder.Username,
                _connectionStringBuilder.Password,
                cancellationToken).ConfigureAwait(false);

            _state = ConnectionState.Open;
        }
        catch
        {
            _state = ConnectionState.Closed;
            if (_protocol != null)
            {
                await _protocol.DisposeAsync().ConfigureAwait(false);
                _protocol = null;
            }
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
            await _protocol.DisposeAsync().ConfigureAwait(false);
            _protocol = null;
        }

        _state = ConnectionState.Closed;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        throw new NotImplementedException();
    }

    public new PgPipeCommand CreateCommand()
    {
        return new PgPipeCommand { Connection = this };
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
        await CloseAsync().ConfigureAwait(false);
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
/// PostgreSQL コマンド (Pipelines版)
/// </summary>
public sealed class PgPipeCommand : DbCommand
{
    private PgPipeConnection? _connection;
    private string _commandText = "";
    private readonly PgParameterCollection _parameters = new();

    [AllowNull]
    public override string CommandText
    {
        get => _commandText;
        set => _commandText = value ?? "";
    }

    public override int CommandTimeout { get; set; } = 30;
    public override CommandType CommandType { get; set; } = CommandType.Text;
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }

    public new PgPipeConnection? Connection
    {
        get => _connection;
        set => _connection = value;
    }

    protected override DbConnection? DbConnection
    {
        get => _connection;
        set => _connection = value as PgPipeConnection;
    }

    protected override DbTransaction? DbTransaction { get; set; }
    protected override DbParameterCollection DbParameterCollection => _parameters;

    public new PgParameterCollection Parameters => _parameters;

    public override void Cancel() { }

    public override int ExecuteNonQuery()
    {
        return ExecuteNonQueryAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        ValidateCommand();
        var sql = BuildSql();
        return await _connection!.Protocol.ExecuteNonQueryAsync(sql, cancellationToken).ConfigureAwait(false);
    }

    public override object? ExecuteScalar()
    {
        return ExecuteScalarAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        await using var reader = await ExecuteDbDataReaderAsync(CommandBehavior.Default, cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
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
        var context = await _connection!.Protocol.ExecuteQueryStreamingAsync(sql, cancellationToken).ConfigureAwait(false);
        return new PgPipeDataReader(context, _connection, behavior);
    }

    public override void Prepare() { }

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
/// PostgreSQL データリーダー (Pipelines版)
/// </summary>
public sealed class PgPipeDataReader : DbDataReader
{
    private readonly PgPipeStreamingQueryContext _context;
    private readonly PgPipeConnection _connection;
    private readonly CommandBehavior _behavior;
    private PgColumnInfo[]? _columns;
    private int _columnCount;
    private bool _hasRows;
    private bool _firstRowRead;
    private bool _isClosed;

    // 行データ - ストリームバッファへの直接参照
    private byte[]? _rowBuffer;
    private int _rowBaseOffset;
    private int[]? _offsets;
    private int[]? _lengths;

    internal PgPipeDataReader(PgPipeStreamingQueryContext context, PgPipeConnection connection, CommandBehavior behavior)
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

    // ストリームバッファへの直接参照を設定
    internal void SetRowDataReference(byte[] buffer, int baseOffset, int columnCount)
    {
        _rowBuffer = buffer;
        _rowBaseOffset = baseOffset;
    }

    internal Span<int> GetOffsetsSpan() => _offsets.AsSpan();
    internal Span<int> GetLengthsSpan() => _lengths.AsSpan();

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

    public override bool NextResult() => false;
    public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => Task.FromResult(false);

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

    public override string GetName(int ordinal) => _columns![ordinal].Name;

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
    public override bool IsDBNull(int ordinal) => _lengths![ordinal] == -1;

    public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken)
        => Task.FromResult(IsDBNull(ordinal));

    public override string GetDataTypeName(int ordinal) => "text";
    public override Type GetFieldType(int ordinal) => typeof(string);

    public override IEnumerator GetEnumerator() => new DbEnumerator(this, closeReader: false);

    public override ValueTask DisposeAsync()
    {
        Close();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
