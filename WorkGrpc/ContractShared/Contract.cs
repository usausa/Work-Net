namespace ContractShared;

using ProtoBuf.Grpc.Configuration;
using ProtoBuf;

[Service]
public interface IHelloService
{
    [Operation]
    Task<HelloResponse> HelloAsync(HelloRequest request);
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
