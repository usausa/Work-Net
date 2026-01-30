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
using MyPgsql;

namespace WorkPostgresClient;

// ユーザーエンティティ
public readonly record struct User(int Id, string Name, string Email, DateTime CreatedAt);

// PostgreSQLクライアント本体
public sealed class PostgresClient : IAsyncDisposable
{
    private const int DefaultBufferSize = 8192;

    private readonly string _host;
    private readonly int _port;
    private readonly string _database;
    private readonly string _user;
    private readonly string _password;

    private TcpClient? _tcpClient;
    private NetworkStream? _stream;

    // 再利用可能なバッファ
    private byte[]? _readBuffer;
    private byte[]? _writeBuffer;

    public PostgresClient(string host, int port, string database, string user, string password)
    {
        _host = host;
        _port = port;
        _database = database;
        _user = user;
        _password = password;
    }

    public async Task ConnectAsync()
    {
        _tcpClient = new TcpClient { NoDelay = true };
        await _tcpClient.ConnectAsync(_host, _port);
        _stream = _tcpClient.GetStream();

        _readBuffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
        _writeBuffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);

        await SendStartupMessageAsync();
        await HandleAuthenticationAsync();
    }

    #region Users CRUD Operations

    public async Task<int> InsertUserAsync(User user)
    {
        var query = $"INSERT INTO users (id, name, email, created_at) VALUES ({user.Id}, {EscapeString(user.Name)}, {EscapeString(user.Email)}, {EscapeTimestamp(user.CreatedAt)})";
        return await ExecuteNonQueryAsync(query);
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        var query = $"SELECT id, name, email, created_at FROM users WHERE id = {id}";
        var result = await ExecuteQueryAsync(query);
        if (result.Rows.Count == 0) return null;

        var row = result.Rows[0];
        return new User(
            int.Parse(row[0]!),
            row[1]!,
            row[2]!,
            DateTime.Parse(row[3]!)
        );
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        var result = await ExecuteQueryAsync("SELECT id, name, email, created_at FROM users ORDER BY id");
        var users = new List<User>(result.Rows.Count);

        foreach (var row in result.Rows)
        {
            users.Add(new User(
                int.Parse(row[0]!),
                row[1]!,
                row[2]!,
                DateTime.Parse(row[3]!)
            ));
        }

        return users;
    }

    public async Task<int> UpdateUserAsync(int id, string name, string email)
    {
        var query = $"UPDATE users SET name = {EscapeString(name)}, email = {EscapeString(email)} WHERE id = {id}";
        return await ExecuteNonQueryAsync(query);
    }

    public async Task<int> DeleteUserAsync(int id)
    {
        var query = $"DELETE FROM users WHERE id = {id}";
        return await ExecuteNonQueryAsync(query);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string EscapeString(string value)
    {
        return $"'{value.Replace("'", "''")}'";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string EscapeTimestamp(DateTime value)
    {
        return $"'{value:yyyy-MM-dd HH:mm:ss.ffffff}'";
    }

    #endregion

    #region Protocol Implementation

    private async Task SendStartupMessageAsync()
    {
        var buffer = _writeBuffer!;
        var offset = 4;

        // プロトコルバージョン 3.0
        WriteInt32BigEndian(buffer.AsSpan(), ref offset, 196608);

        // パラメータ
        offset += WriteNullTerminatedString(buffer.AsSpan(offset), "user");
        offset += WriteNullTerminatedString(buffer.AsSpan(offset), _user);
        offset += WriteNullTerminatedString(buffer.AsSpan(offset), "database");
        offset += WriteNullTerminatedString(buffer.AsSpan(offset), _database);
        offset += WriteNullTerminatedString(buffer.AsSpan(offset), "client_encoding");
        offset += WriteNullTerminatedString(buffer.AsSpan(offset), "UTF8");
        buffer[offset++] = 0;

        BinaryPrimitives.WriteInt32BigEndian(buffer, offset);

        await _stream!.WriteAsync(buffer.AsMemory(0, offset));
    }

    private async Task HandleAuthenticationAsync()
    {
        while (true)
        {
            var (messageType, payload, payloadLength) = await ReadMessageAsync();

            switch (messageType)
            {
                case 'R':
                    await HandleAuthResponseAsync(payload, payloadLength);
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

    private async Task HandleAuthResponseAsync(byte[] payload, int length)
    {
        var authType = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan());

        switch (authType)
        {
            case 0: // AuthenticationOk
                break;

            case 3: // CleartextPassword
                await SendPasswordMessageAsync(_password);
                break;

            case 5: // MD5Password
                var salt = payload.AsSpan(4, 4).ToArray();
                SendMd5Password(salt, out var md5Password);
                await SendPasswordMessageAsync(md5Password);
                break;

            case 10: // SASL
                await HandleSaslAuthAsync();
                break;

            default:
                throw new PostgresException($"未対応の認証方式: {authType}");
        }
    }

    private async Task SendPasswordMessageAsync(string password)
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

            await _stream!.WriteAsync(buffer.AsMemory(0, totalLength));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private void SendMd5Password(ReadOnlySpan<byte> salt, out string result)
    {
        // MD5(MD5(password + user) + salt)
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

    private async Task HandleSaslAuthAsync()
    {
        // SCRAM-SHA-256
        var clientNonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
        var clientFirstBare = $"n=,r={clientNonce}";
        var clientFirstMessage = $"n,,{clientFirstBare}";

        await SendSaslInitialResponseAsync(clientFirstMessage);

        var (msgType1, serverFirstPayload, serverFirstLength) = await ReadMessageAsync();
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

        // SCRAM計算（同期メソッドで実行）
        var clientFinalMessage = ComputeScramClientFinal(clientFirstBare, serverFirstStr, clientFinalWithoutProof, salt, iterations);
        await SendSaslResponseAsync(clientFinalMessage);

        var (msgType2, serverFinalPayload, _) = await ReadMessageAsync();
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

    private async Task SendSaslInitialResponseAsync(string clientFirstMessage)
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

            await _stream!.WriteAsync(buffer.AsMemory(0, totalLength));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task SendSaslResponseAsync(string response)
    {
        var responseBytes = Encoding.UTF8.GetBytes(response);
        var totalLength = 1 + 4 + responseBytes.Length;

        var buffer = ArrayPool<byte>.Shared.Rent(totalLength);
        try
        {
            buffer[0] = (byte)'p';
            BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(1), 4 + responseBytes.Length);
            responseBytes.CopyTo(buffer.AsSpan(5));

            await _stream!.WriteAsync(buffer.AsMemory(0, totalLength));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public async Task<QueryResult> ExecuteQueryAsync(string query)
    {
        var result = new QueryResult();

        await SendQueryMessageAsync(query);

        while (true)
        {
            var (messageType, payload, payloadLength) = await ReadMessageAsync();

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

    public async Task<int> ExecuteNonQueryAsync(string query)
    {
        await SendQueryMessageAsync(query);

        var affectedRows = 0;

        while (true)
        {
            var (messageType, payload, payloadLength) = await ReadMessageAsync();

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

    private async Task SendQueryMessageAsync(string query)
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

            await _stream!.WriteAsync(buffer.AsMemory(0, totalLength));
        }
        finally
        {
            if (!ReferenceEquals(buffer, _writeBuffer))
                ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task<(char type, byte[] payload, int length)> ReadMessageAsync()
    {
        await ReadExactAsync(_readBuffer.AsMemory(0, 5));

        var type = (char)_readBuffer![0];
        var length = BinaryPrimitives.ReadInt32BigEndian(_readBuffer.AsSpan(1)) - 4;

        if (length == 0)
            return (type, Array.Empty<byte>(), 0);

        var buffer = ArrayPool<byte>.Shared.Rent(length);
        await ReadExactAsync(buffer.AsMemory(0, length));

        return (type, buffer, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReturnBuffer(byte[] buffer)
    {
        if (buffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task ReadExactAsync(Memory<byte> buffer)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await _stream!.ReadAsync(buffer.Slice(offset));
            if (read == 0)
                throw new PostgresException("接続が閉じられました");
            offset += read;
        }
    }

    #endregion

    #region Parsing Helpers

    private static List<ColumnInfo> ParseRowDescription(ReadOnlySpan<byte> payload)
    {
        var fieldCount = BinaryPrimitives.ReadInt16BigEndian(payload);
        var columns = new List<ColumnInfo>(fieldCount);
        var offset = 2;

        for (int i = 0; i < fieldCount; i++)
        {
            var nameEnd = payload.Slice(offset).IndexOf((byte)0);
            var name = Encoding.UTF8.GetString(payload.Slice(offset, nameEnd));
            offset += nameEnd + 1 + 18;

            columns.Add(new ColumnInfo(name));
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

    #endregion

    #region Utility Methods

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

    #endregion

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

public readonly record struct ColumnInfo(string Name);

public sealed class QueryResult
{
    public List<ColumnInfo> Columns { get; set; } = [];
    public List<List<string?>> Rows { get; set; } = [];
}

