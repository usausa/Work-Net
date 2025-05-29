namespace WorkTcpServer.Helpers;

public static class MemoryExtensions
{
    public static Span<byte> TrimEnd(this Span<byte> span)
    {
        var end = span.Length - 1;
        for (; end > 0; end--)
        {
            if (span[end] > 0x20)
            {
                break;
            }
        }

        return span[..(end + 1)];
    }
}
