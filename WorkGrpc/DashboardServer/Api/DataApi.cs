namespace DashboardServer.Api;

using System.Diagnostics;

using DashboardContract;

using ProtoBuf.Grpc;

public class DataApi : IDataApi
{
    public Task<EmptyResponse> SendDataAsync(DataRequest request, CallContext context = default)
    {
        Debug.WriteLine(request.Value);

        return Task.FromResult(new EmptyResponse());
    }
}
