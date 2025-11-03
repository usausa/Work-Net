using ContractShared;

using Grpc.Net.Client;

using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Runtime.CompilerServices;
using Grpc.Core;

// ReSharper disable UseObjectOrCollectionInitializer

var rootCommand = new RootCommand("Client");

//--------------------------------------------------------------------------------
// hello
//--------------------------------------------------------------------------------
var helloCommand = new Command("hello");
helloCommand.Handler = CommandHandler.Create(static async () =>
{
    var channel = GrpcChannel.ForAddress("http://localhost:5000");

    var client = channel.CreateGrpcService<IHelloService>();

    var reply = await client.HelloAsync(new HelloRequest { Name = "うさうさ" }).ConfigureAwait(false);

    Console.WriteLine(reply.Message);
});
rootCommand.Add(helloCommand);

//--------------------------------------------------------------------------------
// detail
//--------------------------------------------------------------------------------
var detailCommand = new Command("detail");
detailCommand.Handler = CommandHandler.Create(static async () =>
{
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
});
rootCommand.Add(detailCommand);

//--------------------------------------------------------------------------------
// error
//--------------------------------------------------------------------------------
var errorCommand = new Command("error");
errorCommand.Handler = CommandHandler.Create(static async () =>
{
    var channel = GrpcChannel.ForAddress("http://localhost:5000");

    var client = channel.CreateGrpcService<IHelloService>();

    try
    {
        await client.ErrorAsync(new HelloRequest { Name = "うさうさ" }).ConfigureAwait(false);
    }
    catch (RpcException e)
    {
        Console.WriteLine(e);
    }
});
rootCommand.Add(errorCommand);

//--------------------------------------------------------------------------------
// deadline
//--------------------------------------------------------------------------------
var deadlineCommand = new Command("deadline");
deadlineCommand.Handler = CommandHandler.Create(static async () =>
{
    var channel = GrpcChannel.ForAddress("http://localhost:5000");

    var client = channel.CreateGrpcService<IHelloService>();

    try
    {
        var context = new CallContext(new CallOptions(deadline: DateTime.UtcNow.AddSeconds(3)));
        await client.CancelAsync(new HelloRequest { Name = "うさうさ" }, context).ConfigureAwait(false);
    }
    catch (RpcException e)
    {
        Console.WriteLine(e);
    }
});
rootCommand.Add(deadlineCommand);
//--------------------------------------------------------------------------------
// cancel
//--------------------------------------------------------------------------------
var cancelCommand = new Command("cancel");
cancelCommand.Handler = CommandHandler.Create(static async () =>
{
    var channel = GrpcChannel.ForAddress("http://localhost:5000");

    var client = channel.CreateGrpcService<IHelloService>();

    try
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(3));

        var context = new CallContext(new CallOptions(cancellationToken: cts.Token));
        await client.CancelAsync(new HelloRequest { Name = "うさうさ" }, context).ConfigureAwait(false);
    }
    catch (RpcException e)
    {
        Console.WriteLine(e);
    }
});
rootCommand.Add(cancelCommand);

//--------------------------------------------------------------------------------
// cancel2
//--------------------------------------------------------------------------------
var cancel2Command = new Command("cancel2");
cancel2Command.Handler = CommandHandler.Create(static async () =>
{
    var channel = GrpcChannel.ForAddress("http://localhost:5000");

    var client = channel.CreateGrpcService<IHelloService>();

    try
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(3));
        await client.Cancel2Async(new HelloRequest { Name = "うさうさ" }, cts.Token).ConfigureAwait(false);
    }
    catch (RpcException e)
    {
        Console.WriteLine(e);
    }
});
rootCommand.Add(cancel2Command);

//--------------------------------------------------------------------------------
// stream
//--------------------------------------------------------------------------------
var streamCommand = new Command("stream");
streamCommand.Handler = CommandHandler.Create(static async () =>
{
    var channel = GrpcChannel.ForAddress("http://localhost:5000");

    var client = channel.CreateGrpcService<IHelloService>();

    using var cts = new CancellationTokenSource();

    try
    {
        var call = client.StreamAsync(SendMessagesAsync(cts.Token), new CallContext(new CallOptions(cancellationToken: cts.Token)));

        await foreach (var response in call.WithCancellation(cts.Token))
        {
            Console.WriteLine($"Received response. message=[{response.Message}]");
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }

    static async IAsyncEnumerable<HelloRequest> SendMessagesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // ユーザー入力を待つ
            var input = await Task.Run(Console.ReadLine, cancellationToken);

            if (String.IsNullOrEmpty(input) || (input.ToLower() == "quit"))
            {
                Console.WriteLine("Quit.");
                break;
            }

            var request = new HelloRequest { Name = input };

            Console.WriteLine($"Send request. name=[{input}]");

            yield return request;
        }
    }
});
rootCommand.Add(streamCommand);

await rootCommand.InvokeAsync(args).ConfigureAwait(false);
#if DEBUG
Console.ReadLine();
#endif
