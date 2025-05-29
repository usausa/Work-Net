namespace WorkTcpServer.Helpers;

using System.Buffers;

public sealed class BufferWriterStream : Stream
{
    private readonly IBufferWriter<byte> writer;

    private long position;

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => position;

    public override long Position
    {
        get => position;
        set => throw new NotSupportedException();
    }

    public BufferWriterStream(IBufferWriter<byte> writer)
    {
        this.writer = writer;
    }

    public override void Flush() => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        var span = writer.GetSpan(count);
        buffer.AsSpan(offset, count).CopyTo(span);
        writer.Advance(count);
        position += count;
    }
}
