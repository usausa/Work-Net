using Grpc.Net.Client;

using MultiServer;

using Smart.Threading;

using var channel = GrpcChannel.ForAddress("http://localhost:5000");
var client = new MultiApi.MultiApiClient(channel);

using var call = client.Execute();

// Receiver
var readerTask = Task.Run<TransactionResponse?>(async () =>
{
    try
    {
        // TODO Timeout
        var tcs = new ReusableCancellationTokenSource();
        tcs.CancelAfter(5000);

        // ReSharper disable AccessToDisposedClosure
        while (await call.ResponseStream.MoveNext(tcs.Token))
        {
            var response = call.ResponseStream.Current;

            switch (response.PayloadCase)
            {
                case Response.PayloadOneofCase.TransactionResponse:
                    Console.WriteLine("Received transaction response.");
                    return response.TransactionResponse;
                case Response.PayloadOneofCase.DeviceRequest:
                    Console.WriteLine($"Received device request. id=[{response.DeviceRequest.Id}]");
                    await call.RequestStream.WriteAsync(new Request
                    {
                        DeviceResponse = new DeviceResponse
                        {
                            Result = response.DeviceRequest.Id
                        }
                    });
                    if (response.DeviceRequest.Id == 3)
                    {
                        await call.RequestStream.WriteAsync(new Request
                        {
                            CancelRequest = new CancelRequest()
                        });
                    }
                    break;
                default:
                    Console.WriteLine($"Received unknown response. payload=[{response.PayloadCase}]");
                    break;
            }

            tcs.Reset();
        }
        // ReSharper restore AccessToDisposedClosure
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unknown exception: {ex.Message}");
    }

    return null;
});

// Send
await call.RequestStream.WriteAsync(new Request
{
    TransactionRequest = new TransactionRequest
    {
        Content = "Start"
    }
});

var response = await readerTask;

await call.RequestStream.CompleteAsync();

Console.WriteLine(response is not null ? $"Execute success. result=[{response.Result}]" : "Execute failed.");
