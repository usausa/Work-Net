namespace MultiServer.Api;

using Grpc.Core;

public class MultiApi : MultiServer.MultiApi.MultiApiBase
{
    private readonly ILogger<MultiApi> log;

    public MultiApi(ILogger<MultiApi> log)
    {
        this.log = log;
    }

    public override async Task Execute(IAsyncStreamReader<Request> requestStream,
        IServerStreamWriter<Response> responseStream, ServerCallContext context)
    {
        await foreach (var request in requestStream.ReadAllAsync())
        {
            switch (request.PayloadCase)
            {
                case Request.PayloadOneofCase.TransactionRequest:
                    log.LogInformation("Received transaction request.");
                    await responseStream.WriteAsync(new Response
                    {
                        DeviceRequest = new DeviceRequest
                        {
                            Id = 1
                        }
                    });
                    break;
                case Request.PayloadOneofCase.CancelRequest:
                    log.LogInformation("Received cancel request.");
                    await responseStream.WriteAsync(new Response
                    {
                        TransactionResponse = new TransactionResponse
                        {
                            Result = "Complete"
                        }
                    });
                    break;
                case Request.PayloadOneofCase.DeviceResponse:
                    log.LogInformation("Received device response. result=[{Result}]", request.DeviceResponse.Result);
                    if (request.DeviceResponse.Result < 3)
                    {
                        await responseStream.WriteAsync(new Response
                        {
                            DeviceRequest = new DeviceRequest
                            {
                                Id = request.DeviceResponse.Result + 1
                            }
                        });
                    }
                    break;
                default:
                    log.LogInformation("Received unknown request. payload=[{PayloadCase}]", request.PayloadCase);
                    break;
            }
        }
    }
}
