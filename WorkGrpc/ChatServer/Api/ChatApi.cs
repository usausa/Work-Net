namespace ChatServer.Api;
using ChatServer;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using System.Collections.Concurrent;

public class ChatApi : ChatServer.ChatApi.ChatApiBase
{
    // TODO Divide
    private static readonly ConcurrentDictionary<string, IServerStreamWriter<ChatMessage>> Subscribers = new();
    private static int connectionCounter;

    private readonly ILogger<ChatApi> log;

    public ChatApi(ILogger<ChatApi> log)
    {
        this.log = log;
    }

    public override async Task SendMessage(IAsyncStreamReader<ChatMessage> requestStream, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
    {
        var connectionId = $"{Interlocked.Increment(ref connectionCounter)}#{context.Peer}";
        Subscribers.TryAdd(connectionId, responseStream);

        try
        {
            // TODO add Timeout token
            await foreach (var message in requestStream.ReadAllAsync(context.CancellationToken))
            {
                var timestamp = DateTime.UtcNow;

                log.LogInformation("Message received. timestamp=[{timestamp:HH:mm:ss}], connectionId=[{connectionId}], name=[{name}], message=[{message}]", timestamp, connectionId, message.Name, message.Message);

                message.Timestamp = Timestamp.FromDateTime(timestamp);
                await BroadcastMessageAsync(message);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Unknown exception. id=[{connectionId}]", connectionId);
        }
        finally
        {
            if (Subscribers.TryRemove(connectionId, out _))
            {
                log.LogInformation("Subscriber disconnected. connectionId=[{connectionId}]", connectionId);
            }
        }
    }

    private async Task BroadcastMessageAsync(ChatMessage message)
    {
        var deadSubscribers = default(List<string>);

        foreach (var (connectionId, writer) in Subscribers)
        {
            try
            {
                await writer.WriteAsync(message);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Send message failed. connectionId=[{connectionId}]", connectionId);

                deadSubscribers ??= new List<string>();
                deadSubscribers.Add(connectionId);
            }
        }

        if (deadSubscribers is not null)
        {
            foreach (var connectionId in deadSubscribers)
            {
                if (Subscribers.TryRemove(connectionId, out _))
                {
                    log.LogInformation("Subscriber removed. connectionId=[{connectionId}]", connectionId);
                }
            }
        }
    }
}
