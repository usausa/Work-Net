namespace WorkFastFill;

using System.Buffers;

internal class Program
{
    public static void Main()
    {
        using var data = new Data(20, 10);
        data.Fill(1, 2, 3);
    }
}

public sealed class Data : IDisposable
{
    private byte[] buffer;

    public int Width { get; }

    public int Height { get; }

    public byte[] Buffer => buffer;

    public Data(int width, int height)
    {
        Width = width;
        Height = height;
        buffer = ArrayPool<byte>.Shared.Rent(width * height * 4);
    }


    public void Dispose()
    {
        if (buffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = [];
        }
    }

    public void Fill(byte r, byte g, byte b)
    {
        buffer[0] = r;
        buffer[1] = g;
        buffer[2] = b;

        var length = 3;
        var size = Width * Height * 3;
        while (length < size - length)
        {
            buffer.AsSpan(0, length).CopyTo(buffer.AsSpan(length));

            length += length;
        }

        if (length < size)
        {
            buffer.AsSpan(0, size - length).CopyTo(buffer.AsSpan(length));
        }
    }
}
