namespace WorkTcpServer.Handlers;

using System.Buffers;

public interface IAction
{
    ValueTask<ActionResult> ProcessAsync(ReadOnlySequence<byte> request, IBufferWriter<byte> response);
}
