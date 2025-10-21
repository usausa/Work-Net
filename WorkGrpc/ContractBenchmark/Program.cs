using ContractShared;

using Grpc.Net.Client;

using ProtoBuf.Grpc.Client;

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;

#pragma warning disable CA1812

var rootCommand = new RootCommand("gRPC benchmark");
rootCommand.AddOption(new Option<int>(["--thread", "-t"], () => 100, "thread"));
rootCommand.AddOption(new Option<int>(["--loop", "-l"], () => 1000, "loop"));
rootCommand.Handler = CommandHandler.Create((IConsole console, int thread, int loop) =>
{
    var tasks = new List<Task>();
    for (var i = 0; i < thread; i++)
    {
        tasks.Add(Task.Run(async () =>
        {
            var channel = GrpcChannel.ForAddress("http://localhost:5000");
            var client = channel.CreateGrpcService<IHelloService>();
            for (var j = 0; j < loop; j++)
            {
                _ = await client.HelloAsync(new HelloRequest { Name = "うさうさ" }).ConfigureAwait(false);
            }
        }));
    }

    var watch = Stopwatch.StartNew();

    // 完了待ち
    Task.WaitAll(tasks.ToArray());

    var total = thread * loop;
    console.WriteLine($"TotalCount : {total}");
    console.WriteLine($"TotalTime : {watch.ElapsedMilliseconds}");
    console.WriteLine($"TPS : {(double)total / watch.ElapsedMilliseconds * 1000}");
});

return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
