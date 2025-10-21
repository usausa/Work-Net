namespace ContractShared;

using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

[Service]
public interface IHelloService
{
    [Operation]
    Task<HelloResponse> HelloAsync(HelloRequest request, CallContext context = default!);

    [Operation]
    Task<HelloResponse> RandomErrorAsync(HelloRequest request, CallContext context = default!);

    [Operation]
    Task<HelloResponse> ErrorAsync(HelloRequest request, CallContext context = default!);
}

[ProtoContract]
public class HelloRequest
{
    [ProtoMember(1)]
    public string Name { get; set; } = default!;
}

[ProtoContract]
public class HelloResponse
{
    [ProtoMember(1)]
    public string Message { get; set; } = default!;
}
