using Grpc.Core;
using ProxyServer;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

var app = builder.Build();
app.MapGrpcService<ProxyServerServiceImpl>();
app.Run();

public class ProxyServerServiceImpl : ProxyServerService.ProxyServerServiceBase
{
    private readonly ILogger<ProxyServerServiceImpl> _logger;

    public ProxyServerServiceImpl(ILogger<ProxyServerServiceImpl> logger)
    {
        _logger = logger;
    }

    public override async Task Process(
        IAsyncStreamReader<ProxyServer.ProxyMessage> requestStream,
        IServerStreamWriter<ServerMessage> responseStream,
        ServerCallContext context)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            context.CancellationToken, cancellationTokenSource.Token);
        var cancelled = false;
        string? currentRequestId = null;
        var controlCount = 0;

        // リクエストを処理するタスク
        var receiveTask = Task.Run(async () =>
        {
            await foreach (var message in requestStream.ReadAllAsync(context.CancellationToken))
            {
                switch (message.MessageCase)
                {
                    case ProxyServer.ProxyMessage.MessageOneofCase.ProcessRequest:
                        currentRequestId = message.ProcessRequest.RequestId;
                        controlCount = message.ProcessRequest.ControlCount;
                        _logger.LogInformation("[Server] 処理要求受信: RequestId={RequestId}, ControlCount={ControlCount}",
                            currentRequestId, controlCount);
                        break;

                    case ProxyServer.ProxyMessage.MessageOneofCase.CancelRequest:
                        _logger.LogInformation("[Server] キャンセル要求受信: RequestId={RequestId}",
                            message.CancelRequest.RequestId);
                        cancelled = true;
                        cancellationTokenSource.Cancel();
                        break;

                    case ProxyServer.ProxyMessage.MessageOneofCase.ControlResponse:
                        _logger.LogInformation("[Server] 制御応答受信: ControlId={ControlId}, Result={Result}",
                            message.ControlResponse.ControlId, message.ControlResponse.Result);
                        break;
                }
            }
        });

        // 処理要求を待つ
        while (currentRequestId == null && !context.CancellationToken.IsCancellationRequested)
        {
            await Task.Delay(10, context.CancellationToken);
        }

        if (currentRequestId == null)
        {
            return;
        }

        try
        {
            // 設定通知を送信
            _logger.LogInformation("[Server] 設定通知送信");
            await responseStream.WriteAsync(new ServerMessage
            {
                SettingNotification = new ProxyServer.SettingNotification
                {
                    SettingKey = "mode",
                    SettingValue = "normal"
                }
            }, linkedCts.Token);

            // 制御要求を複数回実行
            for (var i = 0; i < controlCount && !cancelled; i++)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                var controlId = $"control-{i + 1}";
                _logger.LogInformation("[Server] 制御要求送信: ControlId={ControlId}", controlId);

                await responseStream.WriteAsync(new ServerMessage
                {
                    ControlRequest = new ProxyServer.ControlRequest
                    {
                        ControlId = controlId,
                        Command = $"command-{i + 1}"
                    }
                }, linkedCts.Token);

                // 制御応答を待つ（タイムアウト付き）
                var timeout = Task.Delay(30000, linkedCts.Token);
                while (!context.CancellationToken.IsCancellationRequested && !cancelled)
                {
                    if (timeout.IsCompleted)
                    {
                        break;
                    }
                    await Task.Delay(100, linkedCts.Token);
                }
            }

            // 処理応答を送信
            var success = !cancelled;
            var resultMessage = cancelled ? "Cancelled" : "Completed";
            _logger.LogInformation("[Server] 処理応答送信: Success={Success}, Message={Message}", success, resultMessage);

            await responseStream.WriteAsync(new ServerMessage
            {
                ProcessResponse = new ProxyServer.ProcessResponse
                {
                    RequestId = currentRequestId,
                    Success = success,
                    Message = resultMessage
                }
            }, context.CancellationToken);
        }
        catch (OperationCanceledException) when (cancelled)
        {
            // キャンセルされた場合は処理応答を送信
            _logger.LogInformation("[Server] キャンセルにより処理応答送信");
            await responseStream.WriteAsync(new ServerMessage
            {
                ProcessResponse = new ProxyServer.ProcessResponse
                {
                    RequestId = currentRequestId,
                    Success = false,
                    Message = "Cancelled"
                }
            }, context.CancellationToken);
        }

        await receiveTask;
    }
}
