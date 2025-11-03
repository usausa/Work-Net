namespace ChatServer.Api;
using ChatServer;

using Grpc.Core;

using System.Collections.Concurrent;

public class ChatApi : ChatServer.ChatApi.ChatApiBase
{
    // TODO 分離
    private static readonly ConcurrentBag<IServerStreamWriter<ChatMessage>> clients = new();

    public override Task SendMessage(IAsyncStreamReader<ChatMessage> requestStream, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
    {
        clients.Add(responseStream);


        // TODO
        return Task.CompletedTask;
    }
}
