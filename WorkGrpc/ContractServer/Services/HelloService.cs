namespace ContractServer.Services;

using System.Diagnostics;

using ContractShared;

using Grpc.Core;

using ProtoBuf.Grpc;

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
}
