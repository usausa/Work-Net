namespace WorkTcpServer.Handlers.Actions;

using WorkTcpServer.Helpers;
using WorkTcpServer.Services;

public sealed class SetActionFactory(DataService service) : IActionFactory
{
    public bool Match(ReadOnlySpan<byte> command) => command.SequenceEqual("set"u8);

    public IAction Create() => new Action(service);

    private sealed class Request
    {
        public int Value { get; set; }
    }

    private sealed class Action(DataService service) : AbstractAction<Request, Unit>
    {
        protected override ValueTask<(Unit Response, ActionResult ResultCode)> ProcessAsync(Request request)
        {
            if (request.Value < 0)
            {
                return ValueTask.FromResult((Unit.Default, ActionResult.BadRequest));
            }

            service.UpdateValue(request.Value);

            return ValueTask.FromResult((Unit.Default, ActionResult.Success));
        }
    }
}
