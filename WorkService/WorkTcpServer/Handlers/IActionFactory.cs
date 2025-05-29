namespace WorkTcpServer.Handlers;

public interface IActionFactory
{
    bool Match(ReadOnlySpan<byte> command);

    IAction Create();
}
