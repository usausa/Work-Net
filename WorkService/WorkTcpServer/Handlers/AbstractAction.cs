namespace WorkTcpServer.Handlers;

using System.Buffers;

using WorkTcpServer.Helpers;

public abstract class AbstractAction<TRequest, TResponse> : IAction
{
    public ValueTask<ActionResult> ProcessAsync(ReadOnlySequence<byte> request, IBufferWriter<byte> response)
    {
        return typeof(TRequest) != typeof(Unit) ?
            ProcessAsync(ActionHelper.Deserialize<TRequest>(request), response) :
            ProcessAsync((TRequest)(object)Unit.Default, response);
    }

    private async ValueTask<ActionResult> ProcessAsync(TRequest request, IBufferWriter<byte> response)
    {
        var (obj, code) = await ProcessAsync(request);
        if (typeof(TResponse) != typeof(Unit) && obj is not null)
        {
            ActionHelper.Serialize(response, obj);
        }
        return code;
    }

    protected abstract ValueTask<(TResponse Response, ActionResult ResultCode)> ProcessAsync(TRequest request);
}
