using System.Diagnostics;

using Grpc.Net.Client;

using WorkGrpcService;

namespace WorkGrpcClient;

public static class Program
{
    public static async Task Main()
    {
        const int loop = 10_0000;

        using var channel = GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true,
            }
        });
        var client = new Greeter.GreeterClient(channel);
        var reply = await client.SayHelloAsync(new HelloRequest { Name = "GreeterClient" });

        var watch = Stopwatch.StartNew();
        for (var i = 0; i < loop; i++)
        {
            _ = await client.SayHelloAsync(new HelloRequest { Name = "GreeterClient" });
        }

        Console.WriteLine((double)loop / watch.ElapsedMilliseconds * 1000);
    }
}
