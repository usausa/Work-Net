namespace ChatServer.Api;

using ChatServer;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using System.Collections.Concurrent;

public class ChatSubscription
{
    private readonly ILogger<ChatSubscription> log;

    private readonly ConcurrentDictionary<string, IServerStreamWriter<ChatMessage>> subscribers = new();

    private int connectionCounter;

    public ChatSubscription(ILogger<ChatSubscription> log)
    {
        this.log = log;
    }

    public string AddSubscriber(ServerCallContext context, IServerStreamWriter<ChatMessage> responseStream)
    {
        var connectionId = $"{Interlocked.Increment(ref connectionCounter)}#{context.Peer}";
        subscribers.TryAdd(connectionId, responseStream);
        return connectionId;
    }

    public void RemoveSubscriber(string connectionId)
    {
        if (subscribers.TryRemove(connectionId, out _))
        {
            log.LogInformation("Subscriber disconnected. connectionId=[{connectionId}]", connectionId);
        }
    }

    public async Task BroadcastMessageAsync(ChatMessage message)
    {
        var deadSubscribers = default(List<string>);

        foreach (var (connectionId, writer) in subscribers)
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
                if (subscribers.TryRemove(connectionId, out _))
                {
                    log.LogInformation("Subscriber removed. connectionId=[{connectionId}]", connectionId);
                }
            }
        }
    }
}

public class ChatApi : ChatServer.ChatApi.ChatApiBase
{
    private readonly ILogger<ChatApi> log;

    private readonly ChatSubscription subscription;

    public ChatApi(
        ILogger<ChatApi> log,
        ChatSubscription subscription)
    {
        this.log = log;
        this.subscription = subscription;
    }

    public override async Task SendMessage(IAsyncStreamReader<ChatMessage> requestStream, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
    {
        var connectionId = subscription.AddSubscriber(context, responseStream);

        try
        {
            await foreach (var message in requestStream.ReadAllAsync(context.CancellationToken))
            {
                var timestamp = DateTime.UtcNow;

                log.LogInformation("Message received. timestamp=[{timestamp:HH:mm:ss}], connectionId=[{connectionId}], name=[{name}], message=[{message}]", timestamp, connectionId, message.Name, message.Message);

                message.Timestamp = Timestamp.FromDateTime(timestamp);
                await subscription.BroadcastMessageAsync(message);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Unknown exception. id=[{connectionId}]", connectionId);
        }
        finally
        {
            subscription.RemoveSubscriber(connectionId);
        }
    }
}
