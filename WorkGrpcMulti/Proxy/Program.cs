using System.Threading.Channels;
using Grpc.Core;
using Grpc.Net.Client;
using ClientProxy;
using ProxyServer;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddSingleton(_ =>
{
    var channel = GrpcChannel.ForAddress("http://localhost:5002");
    return new ProxyServerService.ProxyServerServiceClient(channel);
});

var app = builder.Build();
app.MapGrpcService<ClientProxyServiceImpl>();
app.Run();

// イベント種別
public enum ProxyEventType
{
    ClientMessage,
    ServerMessage,
    ClientDisconnected,
    ServerDisconnected
}

// 統一イベント
public record ProxyEvent(ProxyEventType Type, object? Message = null);

public class ClientProxyServiceImpl : ClientProxyService.ClientProxyServiceBase
{
    private readonly ILogger<ClientProxyServiceImpl> _logger;
    private readonly ProxyServerService.ProxyServerServiceClient _serverClient;

    public ClientProxyServiceImpl(
        ILogger<ClientProxyServiceImpl> logger,
        ProxyServerService.ProxyServerServiceClient serverClient)
    {
        _logger = logger;
        _serverClient = serverClient;
    }

    public override async Task Process(
        IAsyncStreamReader<ClientProxy.ClientMessage> requestStream,
        IServerStreamWriter<ClientProxy.ProxyMessage> responseStream,
        ServerCallContext context)
    {
        // 統一イベントチャネル
        var eventChannel = Channel.CreateUnbounded<ProxyEvent>();
        var serverResponseReceived = false;

        // Serverとの双方向ストリームを開始
        using var serverCall = _serverClient.Process(cancellationToken: context.CancellationToken);

        // Clientからのメッセージ受信タスク（イベントチャネルへ投入）
        var clientReceiveTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var message in requestStream.ReadAllAsync(context.CancellationToken))
                {
                    await eventChannel.Writer.WriteAsync(
                        new ProxyEvent(ProxyEventType.ClientMessage, message),
                        context.CancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ChannelClosedException)
            {
            }
            finally
            {
                eventChannel.Writer.TryWrite(new ProxyEvent(ProxyEventType.ClientDisconnected));
            }
        });

        // Serverからのメッセージ受信タスク（イベントチャネルへ投入）
        var serverReceiveTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var message in serverCall.ResponseStream.ReadAllAsync(context.CancellationToken))
                {
                    await eventChannel.Writer.WriteAsync(
                        new ProxyEvent(ProxyEventType.ServerMessage, message),
                        context.CancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ChannelClosedException)
            {
            }
            finally
            {
                eventChannel.Writer.TryWrite(new ProxyEvent(ProxyEventType.ServerDisconnected));
            }
        });

        // メインループ（シングルスレッドでイベントを処理）
        var clientDisconnected = false;
        var serverDisconnected = false;

        try
        {
            await foreach (var evt in eventChannel.Reader.ReadAllAsync(context.CancellationToken))
            {
                switch (evt.Type)
                {
                    case ProxyEventType.ClientMessage:
                        await HandleClientMessage(
                            (ClientProxy.ClientMessage)evt.Message!,
                            serverCall.RequestStream,
                            serverResponseReceived,
                            context.CancellationToken);
                        break;

                    case ProxyEventType.ServerMessage:
                        var (handled, isProcessResponse) = await HandleServerMessage(
                            (ServerMessage)evt.Message!,
                            responseStream,
                            context.CancellationToken);
                        if (isProcessResponse)
                        {
                            serverResponseReceived = true;
                            // 処理応答を受信したら終了
                            eventChannel.Writer.Complete();
                        }
                        break;

                    case ProxyEventType.ClientDisconnected:
                        _logger.LogInformation("[Proxy] Client disconnected");
                        clientDisconnected = true;
                        if (clientDisconnected && serverDisconnected)
                        {
                            eventChannel.Writer.Complete();
                        }
                        break;

                    case ProxyEventType.ServerDisconnected:
                        _logger.LogInformation("[Proxy] Server disconnected");
                        serverDisconnected = true;
                        if (clientDisconnected && serverDisconnected)
                        {
                            eventChannel.Writer.Complete();
                        }
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ChannelClosedException)
        {
        }

        // Serverへのストリームを閉じる
        try
        {
            await serverCall.RequestStream.CompleteAsync();
        }
        catch
        {
        }

        await Task.WhenAll(clientReceiveTask, serverReceiveTask);
    }

    private async Task HandleClientMessage(
        ClientProxy.ClientMessage message,
        IClientStreamWriter<ProxyServer.ProxyMessage> serverStream,
        bool serverResponseReceived,
        CancellationToken ct)
    {
        switch (message.MessageCase)
        {
            case ClientProxy.ClientMessage.MessageOneofCase.ProcessRequest:
                _logger.LogInformation("[Proxy] Client->Proxy 処理要求受信: RequestId={RequestId}",
                    message.ProcessRequest.RequestId);

                _logger.LogInformation("[Proxy] Proxy->Server 処理要求送信");
                await serverStream.WriteAsync(new ProxyServer.ProxyMessage
                {
                    ProcessRequest = new ProxyServer.ProcessRequest
                    {
                        RequestId = message.ProcessRequest.RequestId,
                        ControlCount = message.ProcessRequest.ControlCount
                    }
                }, ct);
                break;

            case ClientProxy.ClientMessage.MessageOneofCase.CancelRequest:
                _logger.LogInformation("[Proxy] Client->Proxy キャンセル要求受信: RequestId={RequestId}",
                    message.CancelRequest.RequestId);

                // 処理応答受信前であればServerに転送
                if (!serverResponseReceived)
                {
                    _logger.LogInformation("[Proxy] Proxy->Server キャンセル要求送信");
                    await serverStream.WriteAsync(new ProxyServer.ProxyMessage
                    {
                        CancelRequest = new ProxyServer.CancelRequest
                        {
                            RequestId = message.CancelRequest.RequestId
                        }
                    }, ct);
                }
                break;

            case ClientProxy.ClientMessage.MessageOneofCase.ControlResponse:
                _logger.LogInformation("[Proxy] Client->Proxy 制御応答受信: ControlId={ControlId}",
                    message.ControlResponse.ControlId);

                _logger.LogInformation("[Proxy] Proxy->Server 制御応答送信");
                await serverStream.WriteAsync(new ProxyServer.ProxyMessage
                {
                    ControlResponse = new ProxyServer.ControlResponse
                    {
                        ControlId = message.ControlResponse.ControlId,
                        Result = message.ControlResponse.Result
                    }
                }, ct);
                break;
        }
    }

    private async Task<(bool handled, bool isProcessResponse)> HandleServerMessage(
        ServerMessage message,
        IServerStreamWriter<ClientProxy.ProxyMessage> clientStream,
        CancellationToken ct)
    {
        switch (message.MessageCase)
        {
            case ServerMessage.MessageOneofCase.ProcessResponse:
                _logger.LogInformation("[Proxy] Server->Proxy 処理応答受信: RequestId={RequestId}",
                    message.ProcessResponse.RequestId);

                _logger.LogInformation("[Proxy] Proxy->Client 処理応答送信");
                await clientStream.WriteAsync(new ClientProxy.ProxyMessage
                {
                    ProcessResponse = new ClientProxy.ProcessResponse
                    {
                        RequestId = message.ProcessResponse.RequestId,
                        Success = message.ProcessResponse.Success,
                        Message = message.ProcessResponse.Message
                    }
                }, ct);
                return (true, true);

            case ServerMessage.MessageOneofCase.SettingNotification:
                _logger.LogInformation("[Proxy] Server->Proxy 設定通知受信: Key={Key}",
                    message.SettingNotification.SettingKey);

                _logger.LogInformation("[Proxy] Proxy->Client 設定通知送信");
                await clientStream.WriteAsync(new ClientProxy.ProxyMessage
                {
                    SettingNotification = new ClientProxy.SettingNotification
                    {
                        SettingKey = message.SettingNotification.SettingKey,
                        SettingValue = message.SettingNotification.SettingValue
                    }
                }, ct);
                return (true, false);

            case ServerMessage.MessageOneofCase.ControlRequest:
                _logger.LogInformation("[Proxy] Server->Proxy 制御要求受信: ControlId={ControlId}",
                    message.ControlRequest.ControlId);

                _logger.LogInformation("[Proxy] Proxy->Client 制御要求送信");
                await clientStream.WriteAsync(new ClientProxy.ProxyMessage
                {
                    ControlRequest = new ClientProxy.ControlRequest
                    {
                        ControlId = message.ControlRequest.ControlId,
                        Command = message.ControlRequest.Command
                    }
                }, ct);
                return (true, false);

            default:
                return (false, false);
        }
    }
}
