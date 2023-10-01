namespace WorkSocket;

using Microsoft.AspNetCore.Connections;

public class EchoConnectionHandler : ConnectionHandler
{
    private readonly ILogger<EchoConnectionHandler> logger;

    public EchoConnectionHandler(ILogger<EchoConnectionHandler> logger)
    {
        this.logger = logger;
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        logger.LogInformation(connection.ConnectionId + " connected");

        while (true)
        {
            var result = await connection.Transport.Input.ReadAsync();
            var buffer = result.Buffer;

            foreach (var segment in buffer)
            {
                await connection.Transport.Output.WriteAsync(segment);
            }

            if (result.IsCompleted)
            {
                break;
            }

            connection.Transport.Input.AdvanceTo(buffer.End);
        }

        logger.LogInformation(connection.ConnectionId + " disconnected");
    }
}
