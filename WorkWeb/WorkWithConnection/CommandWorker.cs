namespace WorkWithConnection;

using System.Net.Sockets;
using System.Net;

public sealed class CommandWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // TODO
        var listener = new TcpListener(IPAddress.Any, 8888);
        listener.Start();
        while (!stoppingToken.IsCancellationRequested)
        {
            var client = await listener.AcceptTcpClientAsync(stoppingToken);
            _ = Task.Run(async () =>
            {
                var stream = client.GetStream();

                while (!stoppingToken.IsCancellationRequested)
                {
                    var data = new byte[1024];
                    var read = await stream.ReadAsync(data, 0, 1024, stoppingToken);
                    if (read == 0)
                    {
                        break;
                    }

                    await stream.WriteAsync(data, 0, read, stoppingToken);
                }
            }, stoppingToken);
        }
    }
}
