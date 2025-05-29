namespace WorkTcpServer.Handlers;

using System.Buffers;
using System.Text.Json;

using WorkTcpServer.Helpers;

public static class ActionHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void Serialize(IBufferWriter<byte> writer, object obj)
    {
        JsonSerializer.Serialize(new BufferWriterStream(writer), obj, Options);
    }

    public static T Deserialize<T>(ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsSingleSegment)
        {
            return JsonSerializer.Deserialize<T>(buffer.FirstSpan, Options)!;
        }

        var reader = new Utf8JsonReader(buffer, true, new JsonReaderState());
        return JsonSerializer.Deserialize<T>(ref reader, Options)!;
    }
}
