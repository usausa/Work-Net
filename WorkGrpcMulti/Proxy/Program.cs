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
        // Client -> Proxy -> Server へのメッセージチャネル
        var toServerChannel = Channel.CreateUnbounded<ProxyServer.ProxyMessage>();

        // Server -> Proxy -> Client へのメッセージチャネル
        var toClientChannel = Channel.CreateUnbounded<ClientProxy.ProxyMessage>();

        var serverResponseReceived = false;
        var cts = new CancellationTokenSource();
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, cts.Token);

        // Serverとの双方向ストリームを開始
        using var serverCall = _serverClient.Process(cancellationToken: context.CancellationToken);

        // Clientからのメッセージを受信してServerに転送するタスク
        var clientReceiveTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var message in requestStream.ReadAllAsync(linkedCts.Token))
                {
                    switch (message.MessageCase)
                    {
                        case ClientProxy.ClientMessage.MessageOneofCase.ProcessRequest:
                            _logger.LogInformation("[Proxy] Client->Proxy 処理要求受信: RequestId={RequestId}",
                                message.ProcessRequest.RequestId);

                            await toServerChannel.Writer.WriteAsync(new ProxyServer.ProxyMessage
                            {
                                ProcessRequest = new ProxyServer.ProcessRequest
                                {
                                    RequestId = message.ProcessRequest.RequestId,
                                    ControlCount = message.ProcessRequest.ControlCount
                                }
                            }, linkedCts.Token);
                            break;

                        case ClientProxy.ClientMessage.MessageOneofCase.CancelRequest:
                            _logger.LogInformation("[Proxy] Client->Proxy キャンセル要求受信: RequestId={RequestId}",
                                message.CancelRequest.RequestId);

                            // 処理応答受信前であればServerに転送
                            if (!serverResponseReceived)
                            {
                                await toServerChannel.Writer.WriteAsync(new ProxyServer.ProxyMessage
                                {
                                    CancelRequest = new ProxyServer.CancelRequest
                                    {
                                        RequestId = message.CancelRequest.RequestId
                                    }
                                }, linkedCts.Token);
                            }
                            break;

                        case ClientProxy.ClientMessage.MessageOneofCase.ControlResponse:
                            _logger.LogInformation("[Proxy] Client->Proxy 制御応答受信: ControlId={ControlId}",
                                message.ControlResponse.ControlId);

                            await toServerChannel.Writer.WriteAsync(new ProxyServer.ProxyMessage
                            {
                                ControlResponse = new ProxyServer.ControlResponse
                                {
                                    ControlId = message.ControlResponse.ControlId,
                                    Result = message.ControlResponse.Result
                                }
                            }, linkedCts.Token);
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセルされた場合は正常終了
            }
            finally
            {
                toServerChannel.Writer.Complete();
            }
        });

        // Serverへのメッセージ送信タスク
        var serverSendTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var message in toServerChannel.Reader.ReadAllAsync(linkedCts.Token))
                {
                    _logger.LogInformation("[Proxy] Proxy->Server メッセージ送信: {MessageType}", message.MessageCase);
                    await serverCall.RequestStream.WriteAsync(message, linkedCts.Token);
                }
                await serverCall.RequestStream.CompleteAsync();
            }
            catch (OperationCanceledException)
            {
                // キャンセルされた場合は正常終了
            }
        });

        // Serverからのメッセージを受信してClientに転送するタスク
        var serverReceiveTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var message in serverCall.ResponseStream.ReadAllAsync(linkedCts.Token))
                {
                    switch (message.MessageCase)
                    {
                        case ServerMessage.MessageOneofCase.ProcessResponse:
                            _logger.LogInformation("[Proxy] Server->Proxy 処理応答受信: RequestId={RequestId}",
                                message.ProcessResponse.RequestId);
                            serverResponseReceived = true;

                            await toClientChannel.Writer.WriteAsync(new ClientProxy.ProxyMessage
                            {
                                ProcessResponse = new ClientProxy.ProcessResponse
                                {
                                    RequestId = message.ProcessResponse.RequestId,
                                    Success = message.ProcessResponse.Success,
                                    Message = message.ProcessResponse.Message
                                }
                            }, linkedCts.Token);

                            // 処理応答を受信したらチャネルを閉じる
                            toClientChannel.Writer.Complete();
                            cts.Cancel();
                            return;

                        case ServerMessage.MessageOneofCase.SettingNotification:
                            _logger.LogInformation("[Proxy] Server->Proxy 設定通知受信: Key={Key}",
                                message.SettingNotification.SettingKey);

                            await toClientChannel.Writer.WriteAsync(new ClientProxy.ProxyMessage
                            {
                                SettingNotification = new ClientProxy.SettingNotification
                                {
                                    SettingKey = message.SettingNotification.SettingKey,
                                    SettingValue = message.SettingNotification.SettingValue
                                }
                            }, linkedCts.Token);
                            break;

                        case ServerMessage.MessageOneofCase.ControlRequest:
                            _logger.LogInformation("[Proxy] Server->Proxy 制御要求受信: ControlId={ControlId}",
                                message.ControlRequest.ControlId);

                            await toClientChannel.Writer.WriteAsync(new ClientProxy.ProxyMessage
                            {
                                ControlRequest = new ClientProxy.ControlRequest
                                {
                                    ControlId = message.ControlRequest.ControlId,
                                    Command = message.ControlRequest.Command
                                }
                            }, linkedCts.Token);
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセルされた場合は正常終了
            }
            finally
            {
                toClientChannel.Writer.TryComplete();
            }
        });

        // Clientへのメッセージ送信タスク
        var clientSendTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var message in toClientChannel.Reader.ReadAllAsync(linkedCts.Token))
                {
                    _logger.LogInformation("[Proxy] Proxy->Client メッセージ送信: {MessageType}", message.MessageCase);
                    await responseStream.WriteAsync(message, linkedCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセルされた場合は正常終了
            }
        });

        // すべてのタスクが完了するのを待つ
        await Task.WhenAll(clientReceiveTask, serverSendTask, serverReceiveTask, clientSendTask);
    }
}
