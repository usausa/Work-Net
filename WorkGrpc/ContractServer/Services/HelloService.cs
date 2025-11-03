namespace ContractServer.Services;

using ContractShared;

using Grpc.Core;

using ProtoBuf.Grpc;

using System.Threading;

public class HelloService : IHelloService
{
    private readonly ILogger<HelloService> log;

    private readonly Random random = new();

    public HelloService(ILogger<HelloService> log)
    {
        this.log = log;
    }

    public Task<HelloResponse> HelloAsync(HelloRequest request, CallContext context)
    {
        return Task.FromResult(new HelloResponse { Message = $"Hello {request.Name}" });
    }

    public Task<HelloResponse> RandomErrorAsync(HelloRequest request, CallContext context)
    {
        if (random.NextDouble() > 0.25)
        {
            throw new RpcException(new Status(StatusCode.Unavailable, $"- {request.Name}"));
        }

        return Task.FromResult(new HelloResponse { Message = $"Hello {request.Name}" });
    }

    public Task<HelloResponse> ErrorAsync(HelloRequest request, CallContext context)
    {
        throw new Exception("Error");
    }

    public async Task<HelloResponse> CancelAsync(HelloRequest request, CallContext context = default)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), context.CancellationToken);
            log.LogInformation("Canceled");
        }
        catch (Exception e)
        {
            log.LogError(e, "Cancel exception.");
            throw;
        }
        return new HelloResponse { Message = $"Hello {request.Name}" };
    }

    public async Task<HelloResponse> Cancel2Async(HelloRequest request, CancellationToken cancel)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancel);
            log.LogInformation("Canceled");
        }
        catch (Exception e)
        {
            log.LogError(e, "Cancel exception.");
            throw;
        }
        return new HelloResponse { Message = $"Hello {request.Name}" };
    }

    public async IAsyncEnumerable<HelloResponse> StreamAsync(IAsyncEnumerable<HelloRequest> messages, CallContext context = default)
    {
        await foreach (var message in messages.WithCancellation(context.CancellationToken))
        {
            log.LogInformation($"Received: {message.Name}");

            yield return new HelloResponse { Message = $"Hello {message.Name}" };
        }

        log.LogInformation("Client disconnected.");
    }

    public async IAsyncEnumerable<HelloResponse> TimeoutAsync(IAsyncEnumerable<HelloRequest> messages, CallContext context = default)
    {
        var en = messages.GetAsyncEnumerator();
        try
        {
            while (true)
            {
                var moveNextTask = en.MoveNextAsync().AsTask();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), context.CancellationToken);

                var completedTask = await Task.WhenAny(moveNextTask, timeoutTask);

                if (completedTask == moveNextTask)
                {
                    var hasNext = await moveNextTask;
                    if (!hasNext)
                    {
                        break;
                    }

                    var request = en.Current;

                    yield return new HelloResponse { Message = $"Hello {request.Name}" };
                }
                else if (context.CancellationToken.IsCancellationRequested)
                {
                    // Closed
                    log.LogInformation("Context token canceled.");
                    yield break;
                }
                else
                {
                    // Timeout
                    log.LogInformation("Timeout.");
                    yield break;
                }
            }
        }
        finally
        {
            try
            {
                await en.DisposeAsync();
            }
            catch (NotSupportedException)
            {
            }
        }

        log.LogInformation("Client disconnected.");
    }
}
