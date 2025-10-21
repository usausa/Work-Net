using ContractShared;

using Grpc.Net.Client;

using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;

var channel = GrpcChannel.ForAddress("http://localhost:5000", new GrpcChannelOptions
{
    HttpHandler = new SocketsHttpHandler
    {
        PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
        KeepAlivePingDelay = TimeSpan.FromSeconds(60),
        KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
        EnableMultipleHttp2Connections = true,
    }
});

var client = channel.CreateGrpcService<IHelloService>();

var context = new CallContext(flags: CallContextFlags.CaptureMetadata);
var reply = await client.HelloAsync(new HelloRequest { Name = "うさうさ" }, context).ConfigureAwait(false);

Console.WriteLine(reply.Message);
var headers = await context.ResponseHeadersAsync();
foreach (var entry in headers)
{
    Console.WriteLine($"{entry.Key} : {entry.Value}");
}
