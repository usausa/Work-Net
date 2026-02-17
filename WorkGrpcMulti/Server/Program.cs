using System.Threading.Channels;
using Grpc.Core;
using ProxyServer;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

var app = builder.Build();
app.MapGrpcService<ProxyServerServiceImpl>();
app.Run();

// Serverイベント種別
public enum ServerEventType
{
    ProcessRequest,
    ControlResponse,
    CancelRequest,
    ClientDisconnected
}

// Serverイベント
public record ServerEvent(ServerEventType Type, object? Message = null);

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
        // 統一イベントチャネル
        var eventChannel = Channel.CreateUnbounded<ServerEvent>();

        // メッセージ受信タスク（イベントチャネルへ投入）
        var receiveTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var message in requestStream.ReadAllAsync(context.CancellationToken))
                {
                    var evt = message.MessageCase switch
                    {
                        ProxyServer.ProxyMessage.MessageOneofCase.ProcessRequest =>
                            new ServerEvent(ServerEventType.ProcessRequest, message.ProcessRequest),
                        ProxyServer.ProxyMessage.MessageOneofCase.ControlResponse =>
                            new ServerEvent(ServerEventType.ControlResponse, message.ControlResponse),
                        ProxyServer.ProxyMessage.MessageOneofCase.CancelRequest =>
                            new ServerEvent(ServerEventType.CancelRequest, message.CancelRequest),
                        _ => null
                    };

                    if (evt != null)
                    {
                        await eventChannel.Writer.WriteAsync(evt, context.CancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await eventChannel.Writer.WriteAsync(new ServerEvent(ServerEventType.ClientDisconnected));
                eventChannel.Writer.Complete();
            }
        });

        string? currentRequestId = null;
        var controlCount = 0;
        var cancelled = false;

        try
        {
            // 処理要求を待つ
            await foreach (var evt in eventChannel.Reader.ReadAllAsync(context.CancellationToken))
            {
                if (evt.Type == ServerEventType.ProcessRequest)
                {
                    var req = (ProxyServer.ProcessRequest)evt.Message!;
                    currentRequestId = req.RequestId;
                    controlCount = req.ControlCount;
                    _logger.LogInformation("[Server] 処理要求受信: RequestId={RequestId}, ControlCount={ControlCount}",
                        currentRequestId, controlCount);
                    break;
                }

                if (evt.Type == ServerEventType.ClientDisconnected)
                {
                    _logger.LogInformation("[Server] Client disconnected before process request");
                    return;
                }
            }

            if (currentRequestId == null)
            {
                return;
            }

            // 設定通知を送信
            _logger.LogInformation("[Server] 設定通知送信");
            await responseStream.WriteAsync(new ServerMessage
            {
                SettingNotification = new ProxyServer.SettingNotification
                {
                    SettingKey = "mode",
                    SettingValue = "normal"
                }
            }, context.CancellationToken);

            // 制御要求を複数回実行
            for (var i = 0; i < controlCount && !cancelled; i++)
            {
                var controlId = $"control-{i + 1}";
                _logger.LogInformation("[Server] 制御要求送信: ControlId={ControlId}", controlId);

                await responseStream.WriteAsync(new ServerMessage
                {
                    ControlRequest = new ProxyServer.ControlRequest
                    {
                        ControlId = controlId,
                        Command = $"command-{i + 1}"
                    }
                }, context.CancellationToken);

                // 制御応答を待つ（タイムアウト・キャンセル対応）
                var waitResult = await WaitForControlResponseAsync(
                    eventChannel.Reader,
                    controlId,
                    TimeSpan.FromSeconds(5),
                    context.CancellationToken);

                switch (waitResult)
                {
                    case WaitResult.Received:
                        _logger.LogInformation("[Server] 制御応答受信: ControlId={ControlId}", controlId);
                        break;
                    case WaitResult.Cancelled:
                        _logger.LogInformation("[Server] キャンセル要求受信");
                        cancelled = true;
                        break;
                    case WaitResult.Timeout:
                        _logger.LogWarning("[Server] 制御応答タイムアウト: ControlId={ControlId}", controlId);
                        cancelled = true;
                        break;
                    case WaitResult.Disconnected:
                        _logger.LogInformation("[Server] Client disconnected");
                        return;
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
        catch (OperationCanceledException)
        {
            if (currentRequestId != null)
            {
                _logger.LogInformation("[Server] キャンセルにより処理応答送信");
                try
                {
                    await responseStream.WriteAsync(new ServerMessage
                    {
                        ProcessResponse = new ProxyServer.ProcessResponse
                        {
                            RequestId = currentRequestId,
                            Success = false,
                            Message = "Cancelled"
                        }
                    }, CancellationToken.None);
                }
                catch
                {
                }
            }
        }

        await receiveTask;

        _logger.LogInformation("[Server] Connection closed");
    }

    private enum WaitResult { Received, Cancelled, Timeout, Disconnected }

    private async Task<WaitResult> WaitForControlResponseAsync(
        ChannelReader<ServerEvent> reader,
        string expectedControlId,
        TimeSpan timeout,
        CancellationToken ct)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            while (true)
            {
                var evt = await reader.ReadAsync(linkedCts.Token);

                switch (evt.Type)
                {
                    case ServerEventType.ControlResponse:
                        var response = (ProxyServer.ControlResponse)evt.Message!;
                        if (response.ControlId == expectedControlId)
                        {
                            return WaitResult.Received;
                        }
                        // 想定外のControlIdの場合は次のイベントを待つ
                        break;

                    case ServerEventType.CancelRequest:
                        return WaitResult.Cancelled;

                    case ServerEventType.ClientDisconnected:
                        return WaitResult.Disconnected;

                    case ServerEventType.ProcessRequest:
                        // 制御応答待ち中の処理要求は無視
                        break;
                }
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return WaitResult.Timeout;
        }
    }
}
