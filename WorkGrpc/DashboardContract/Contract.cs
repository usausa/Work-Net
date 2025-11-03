namespace DashboardContract;

using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

[Service]
public interface IDataApi
{
    [Operation]
    Task<EmptyResponse> SendDataAsync(DataRequest request, CallContext context = default!);
}

[ProtoContract]
public class DataRequest
{
    [ProtoMember(1)]
    public int Value { get; set; }
}

[ProtoContract]
public class EmptyResponse
{
}
