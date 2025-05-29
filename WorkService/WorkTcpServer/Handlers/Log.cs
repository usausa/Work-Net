namespace WorkTcpServer.Handlers;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Handler connected. connectionId=[{connectionId}]")]
    public static partial void DebugHandlerConnected(this ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handler disconnected. connectionId=[{connectionId}]")]
    public static partial void DebugHandlerDisconnected(this ILogger logger, string connectionId);
}
