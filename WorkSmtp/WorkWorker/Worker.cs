using System.Buffers;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace WorkWorker;

using SmtpServer;

public class Worker : IHostedService
{
    private readonly SmtpServer server;

    public Worker(IServiceProvider provider)
    {
        var options = new SmtpServerOptionsBuilder()
            .ServerName("localhost")
            .Port(25, 587)
            .Build();
        server = new SmtpServer(options, provider);
    }

    public Task StartAsync(CancellationToken cancellationToken) =>
        server.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) =>
        server.ShutdownTask;
}

public class CustomMessageStore : MessageStore
{
    public override async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();

        var position = buffer.GetPosition(0);
        while (buffer.TryGet(ref position, out var memory))
        {
            await stream.WriteAsync(memory, cancellationToken);
        }

        stream.Position = 0;

        Console.WriteLine(await new StreamReader(stream).ReadToEndAsync(cancellationToken));
        return SmtpResponse.Ok;
    }
}
