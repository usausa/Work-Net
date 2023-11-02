namespace WorkWithConnection;

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

using System.Buffers;
using System.Reflection.PortableExecutable;
using System.Text;

public sealed class CommandHandler : ConnectionHandler
{
    private readonly ILogger<CommandHandler> log;

    public CommandHandler(ILogger<CommandHandler> log)
    {
        this.log = log;
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        log.LogInformation(connection.ConnectionId + " connected");

        while (true)
        {
            var result = await connection.Transport.Input.ReadAsync();
            var buffer = result.Buffer;

            while (!buffer.IsEmpty && ReadLine(ref buffer, out var line))
            {
                log.LogDebug("Execute. command=[{Command}]", Encoding.UTF8.GetString(line));
            }

            if (result.IsCompleted || result.IsCanceled)
            {
                break;
            }

            connection.Transport.Input.AdvanceTo(buffer.Start, buffer.End);
        }

        log.LogInformation(connection.ConnectionId + " disconnected");
    }

    private static bool ReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        var reader = new SequenceReader<byte>(buffer);
        if (reader.TryReadTo(out line, "\r\n"u8))
        {
            buffer = buffer.Slice(reader.Position);
            return true;
        }

        return false;
    }
}
