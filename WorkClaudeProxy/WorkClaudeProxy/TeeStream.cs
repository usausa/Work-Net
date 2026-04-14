namespace WorkClaudeProxy;

/// <summary>
/// レスポンスボディを元のストリームに転送しながら、同時にバッファにも書き込むストリーム。
/// </summary>
internal sealed class TeeStream : Stream
{
    private readonly Stream primary;
    private readonly MemoryStream secondary;

    public TeeStream(Stream primary, MemoryStream secondary)
    {
        this.primary = primary;
        this.secondary = secondary;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => primary.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken)
        => primary.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException();

    public override void SetLength(long value)
        => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        primary.Write(buffer, offset, count);
        secondary.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        primary.Write(buffer);
        secondary.Write(buffer);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await primary.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
        secondary.Write(buffer, offset, count);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await primary.WriteAsync(buffer, cancellationToken);
        secondary.Write(buffer.Span);
    }

    protected override void Dispose(bool disposing)
    {
        // primaryとsecondaryは呼び出し元が所有するため、ここでは破棄しない
        base.Dispose(disposing);
    }
}
