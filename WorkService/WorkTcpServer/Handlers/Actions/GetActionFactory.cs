namespace WorkTcpServer.Handlers.Actions;

using WorkTcpServer.Helpers;
using WorkTcpServer.Services;

public sealed class GetActionFactory(DataService service) : IActionFactory
{
    public bool Match(ReadOnlySpan<byte> command) => command.SequenceEqual("set"u8);

    public IAction Create() => new Action(service);

    private sealed class Response
    {
        public int Value { get; set; }
    }

    private sealed class Action(DataService service) : AbstractAction<Unit, Response>
    {
        protected override ValueTask<(Response Response, ActionResult ResultCode)> ProcessAsync(Unit request)
        {
            var response = new Response
            {
                Value = service.GetValue()
            };

            return ValueTask.FromResult((response, ActionResult.Success));
        }
    }
}
