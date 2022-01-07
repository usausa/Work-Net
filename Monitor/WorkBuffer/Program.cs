namespace WorkBuffer;

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

public static class Program
{
    public static void Main()
    {
        TestResponse1();
        TestResponse2();
        TestRequest();
    }

    private static void TestResponse1()
    {
        using var buffer = new ResponseBuilder(16);
        Add(buffer, 128);
    }

    private static void TestResponse2()
    {
        using var buffer = new ResponseBuilder(16);
        Add(buffer, 16);
        buffer.Add(1.2345678901234567890f);
    }

    private static void Add(ResponseBuilder buffer, int size)
    {
        Span<byte> temp = stackalloc byte[size];
        buffer.Add(temp);
    }

    private static void TestRequest()
    {
        using var request = new RequestBuffer(16);

        // Receive
        var read = Receive(request.GetReceiveMemory(), "list\r\n");
        Debug.Assert(read == 6, "Receive");

        request.Advance(read);

        Debug.Assert(request.TryGetLine(out var line), "TryGetLine");
        Debug.Assert(line.Length == 6, "TryGetLine");

        request.Flip(line.Length);

        Debug.Assert(request.HasRemaining, "HasRemaining");

        // Split
        read = Receive(request.GetReceiveMemory(), "lis");
        Debug.Assert(read == 3, "Receive");

        request.Advance(read);

        Debug.Assert(!request.TryGetLine(out line), "TryGetLine");

        read = Receive(request.GetReceiveMemory(), "t\r\n");
        Debug.Assert(read == 3, "Receive");

        request.Advance(read);

        Debug.Assert(request.TryGetLine(out line), "TryGetLine");
        Debug.Assert(line.Length == 6, "TryGetLine");

        request.Flip(line.Length);

        Debug.Assert(request.HasRemaining, "HasRemaining");

        // Chunk
        read = Receive(request.GetReceiveMemory(), "list\r\nlist\r\n");
        Debug.Assert(read == 12, "Receive");

        request.Advance(read);

        Debug.Assert(request.TryGetLine(out line), "TryGetLine");
        Debug.Assert(line.Length == 6, "TryGetLine");

        request.Flip(line.Length);

        Debug.Assert(request.TryGetLine(out line), "TryGetLine");
        Debug.Assert(line.Length == 6, "TryGetLine");

        request.Flip(line.Length);

        Debug.Assert(request.HasRemaining, "HasRemaining");

        // Overflow
        read = Receive(request.GetReceiveMemory(), "********");
        Debug.Assert(read == 8, "Receive");

        request.Advance(read);

        Debug.Assert(!request.TryGetLine(out line), "TryGetLine");

        read = Receive(request.GetReceiveMemory(), "********");
        Debug.Assert(read == 8, "Receive");

        request.Advance(read);

        Debug.Assert(!request.TryGetLine(out line), "TryGetLine");

        Debug.Assert(!request.HasRemaining, "HasRemaining");
    }

    private static int Receive(Memory<byte> buffer, string data)
    {
        var span = Encoding.ASCII.GetBytes(data);
        if (buffer.Length < span.Length)
        {
            span[..buffer.Length].CopyTo(buffer.Span);
            return buffer.Length;
        }

        span.CopyTo(buffer.Span);
        return span.Length;
    }
}

public sealed class ResponseBuilder : IDisposable
{
    private static readonly Encoding Ascii = Encoding.ASCII;

    private byte[] buffer;

    private int length;

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => length == 0;
    }

    public ResponseBuilder(int bufferSize)
    {
        buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        length = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => length = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<byte> GetSendMemory() => new(buffer, 0, length);

    private void Grow(int size)
    {
        var newSize = buffer.Length;
        do
        {
            newSize *= 2;
        }
        while (newSize < length + size);

        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        buffer.CopyTo(newBuffer, 0);
        ArrayPool<byte>.Shared.Return(buffer);
        buffer = newBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(ReadOnlySpan<byte> value)
    {
        if ((buffer.Length - length) < value.Length)
        {
            Grow(value.Length);
        }

        value.CopyTo(buffer.AsSpan(length));
        length += value.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(ReadOnlySpan<char> value)
    {
        if ((buffer.Length - length) < value.Length)
        {
            Grow(value.Length);
        }

        length += Ascii.GetBytes(value, buffer.AsSpan(length));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(byte value)
    {
        if ((buffer.Length - length) < 1)
        {
            Grow(1);
        }

        buffer[length++] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(int value)
    {
        if (!Utf8Formatter.TryFormat(value, buffer.AsSpan(length), out var written))
        {
            Grow(written);
            if (!Utf8Formatter.TryFormat(value, buffer.AsSpan(length), out written))
            {
                throw new InvalidOperationException();
            }
        }

        length += written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(float value)
    {
        if (!Utf8Formatter.TryFormat(value, buffer.AsSpan(length), out var written))
        {
            Grow(written);
            if (!Utf8Formatter.TryFormat(value, buffer.AsSpan(length), out written))
            {
                throw new InvalidOperationException();
            }
        }

        length += written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddLineFeed()
    {
        Add((byte)'\n');
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEndLine()
    {
        Add(stackalloc byte[] { (byte)'.', (byte)'\n' });
    }
}

public struct RequestBuffer : IDisposable
{
    private readonly byte[] buffer;

    private int start;

    private int length;

    public bool HasRemaining
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (buffer.Length - length) > 0;
    }

    public RequestBuffer(int bufferSize)
    {
        buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        start = 0;
        length = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<byte> GetReceiveMemory() => buffer.AsMemory(length, buffer.Length - length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetLine(out Memory<byte> line)
    {
        var index = buffer.AsSpan(start, length - start).IndexOf((byte)'\n');
        if (index < 0)
        {
            line = default;
            return false;
        }

        line = buffer.AsMemory(0, start + index + 1);
        return true;
    }

    public void Advance(int size)
    {
        start = length;
        length += size;
    }

    public void Flip(int offset)
    {
        if (offset < length)
        {
            var nextSize = length - offset;
            buffer.AsSpan(offset, nextSize).CopyTo(buffer.AsSpan());
            length = nextSize;
        }
        else
        {
            length = 0;
        }

        start = 0;
    }
}
