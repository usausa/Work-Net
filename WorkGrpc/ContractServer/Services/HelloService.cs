namespace ContractServer.Services;

using ContractShared;

using Grpc.Core;

using ProtoBuf.Grpc;

public class HelloService : IHelloService
{
    private readonly Random random = new();

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
}
