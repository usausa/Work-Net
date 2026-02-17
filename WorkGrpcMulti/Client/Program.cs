using Grpc.Core;
using Grpc.Net.Client;
using ClientProxy;

Console.WriteLine("[Client] Starting...");

using var channel = GrpcChannel.ForAddress("http://localhost:5001");
var client = new ClientProxyService.ClientProxyServiceClient(channel);

using var call = client.Process();

var requestId = Guid.NewGuid().ToString();
var controlCount = 3; // 制御要求の回数
var processCompleted = false;
var cts = new CancellationTokenSource();

// Proxyからのメッセージを受信するタスク
var receiveTask = Task.Run(async () =>
{
    try
    {
        await foreach (var message in call.ResponseStream.ReadAllAsync(cts.Token))
        {
            switch (message.MessageCase)
            {
                case ProxyMessage.MessageOneofCase.ProcessResponse:
                    Console.WriteLine($"[Client] 処理応答受信: RequestId={message.ProcessResponse.RequestId}, " +
                                    $"Success={message.ProcessResponse.Success}, Message={message.ProcessResponse.Message}");
                    processCompleted = true;
                    cts.Cancel();
                    return;

                case ProxyMessage.MessageOneofCase.SettingNotification:
                    Console.WriteLine($"[Client] 設定通知受信: Key={message.SettingNotification.SettingKey}, " +
                                    $"Value={message.SettingNotification.SettingValue}");
                    break;

                case ProxyMessage.MessageOneofCase.ControlRequest:
                    Console.WriteLine($"[Client] 制御要求受信: ControlId={message.ControlRequest.ControlId}, " +
                                    $"Command={message.ControlRequest.Command}");

                    // 少しウエイトしてから制御応答を送信
                    await Task.Delay(500);

                    Console.WriteLine($"[Client] 制御応答送信: ControlId={message.ControlRequest.ControlId}");
                    await call.RequestStream.WriteAsync(new ClientMessage
                    {
                        ControlResponse = new ControlResponse
                        {
                            ControlId = message.ControlRequest.ControlId,
                            Result = $"result-{message.ControlRequest.ControlId}"
                        }
                    });
                    break;
            }
        }
    }
    catch (OperationCanceledException)
    {
        // 正常終了
    }
    catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
    {
        // キャンセルされた場合は正常終了
    }
});

// 処理要求を送信
Console.WriteLine($"[Client] 処理要求送信: RequestId={requestId}, ControlCount={controlCount}");
await call.RequestStream.WriteAsync(new ClientMessage
{
    ProcessRequest = new ProcessRequest
    {
        RequestId = requestId,
        ControlCount = controlCount
    }
});

// キャンセルテスト用（コメントアウトを外すとキャンセルをテストできます）
// _ = Task.Run(async () =>
// {
//     await Task.Delay(1000);
//     if (!processCompleted)
//     {
//         Console.WriteLine($"[Client] キャンセル要求送信: RequestId={requestId}");
//         await call.RequestStream.WriteAsync(new ClientMessage
//         {
//             CancelRequest = new CancelRequest
//             {
//                 RequestId = requestId
//             }
//         });
//     }
// });

// 受信タスクの完了を待つ
await receiveTask;

// ストリームを閉じる
await call.RequestStream.CompleteAsync();

Console.WriteLine("[Client] Completed.");

