namespace WorkTcpServer.Handlers;

using System;
using System.Buffers;
using System.Buffers.Text;

using Microsoft.AspNetCore.Connections;

using Smart.IO;
using Smart.Threading;

using WorkTcpServer.Helpers;

public sealed class SampleConnectionHandler : ConnectionHandler
{
    private const int RequestVersionSize = 8;
    private const int RequestCommandSize = 16;
    private const int RequestLengthSize = 8;

    private const int RequestVersionOffset = 0;
    private const int RequestCommandOffset = RequestVersionOffset + RequestVersionSize;
    private const int RequestLengthOffset = RequestCommandOffset + RequestCommandSize;

    private const int RequestHeaderSize = RequestLengthOffset + RequestLengthSize;

    private const int ResponseVersionSize = 8;
    private const int ResponseCodeSize = 8;
    private const int ResponseReservedSize = 8;
    private const int ResponseLengthSize = 8;

    private const int ResponseVersionOffset = 0;
    private const int ResponseCodeOffset = ResponseVersionOffset + ResponseVersionSize;
    private const int ResponseReservedOffset = ResponseCodeOffset + ResponseCodeSize;
    private const int ResponseLengthOffset = ResponseReservedOffset + ResponseReservedSize;

    private const int ResponseHeaderSize = ResponseLengthOffset + ResponseLengthSize;

    private static readonly StandardFormat LengthFormat = new('D', 8);

    private readonly ILogger<SampleConnectionHandler> log;

    private readonly IActionFactory[] factories;

    public SampleConnectionHandler(
        ILogger<SampleConnectionHandler> log,
        IEnumerable<IActionFactory> factories)
    {
        this.log = log;
        this.factories = factories.ToArray();
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        log.DebugHandlerConnected(connection.ConnectionId);

        try
        {
            var state = new State();
            using var timeout = new ReusableCancellationTokenSource();

            while (true)
            {
                timeout.CancelAfter(30_000);
                var result = await connection.Transport.Input.ReadAsync(timeout.Token);
                var buffer = result.Buffer;

                while (!buffer.IsEmpty)
                {
                    var read = Read(state, ref buffer, out var request);
                    if (read == ReadResult.Progress)
                    {
                        break;
                    }

                    if (read == ReadResult.Success)
                    {
                        await ProcessRequestAsync(request, connection.Transport.Output);
                    }
                    else
                    {
                        WriteResponse(connection.Transport.Output, request, ActionResult.BadRequest, ReadOnlySpan<byte>.Empty);
                    }

                    await connection.Transport.Output.FlushAsync(CancellationToken.None);

                    state.Reset();
                }

                if (result.IsCompleted)
                {
                    break;
                }

                connection.Transport.Input.AdvanceTo(buffer.Start, buffer.End);

                timeout.Reset();
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        finally
        {
            log.DebugHandlerDisconnected(connection.ConnectionId);
        }
    }

    private static unsafe ReadResult Read(State state, ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> request)
    {
        if (!state.HeaderProcessed)
        {
            if (buffer.Length < RequestHeaderSize)
            {
                request = default!;
                return ReadResult.Progress;
            }

            Span<byte> header = stackalloc byte[RequestHeaderSize];
            buffer.Slice(0, RequestHeaderSize).CopyTo(header);

            if (!Utf8Parser.TryParse(header.Slice(RequestLengthOffset, RequestLengthSize), out int size, out _))
            {
                request = default!;
                return ReadResult.ProtocolError;
            }

            state.HeaderProcessed = true;
            state.BodySize = size;
        }

        var totalLength = RequestHeaderSize + state.BodySize;
        if (buffer.Length < totalLength)
        {
            request = default!;
            return ReadResult.Progress;
        }

        request = buffer.Slice(0, totalLength);
        buffer = buffer.Slice(totalLength);

        return ReadResult.Success;
    }

    private async ValueTask ProcessRequestAsync(ReadOnlySequence<byte> request, IBufferWriter<byte> response)
    {
        Span<byte> command = stackalloc byte[RequestCommandSize];
        request.Slice(RequestCommandOffset, RequestCommandSize).CopyTo(command);
        command = command.TrimEnd();

        foreach (var factory in factories)
        {
            if (factory.Match(command))
            {
                var action = factory.Create();
                using var body = new PooledBufferWriter<byte>(4096);
                var result = await action.ProcessAsync(request, body);
                WriteResponse(response, request, result, body.WrittenSpan);
                return;
            }
        }

        WriteResponse(response, request, ActionResult.NotFound, Span<byte>.Empty);
    }

    private static void WriteResponse(IBufferWriter<byte> response, ReadOnlySequence<byte> request, ActionResult result, ReadOnlySpan<byte> body)
    {
        var span = response.GetSpan(ResponseHeaderSize);

        request.Slice(RequestVersionOffset, RequestVersionSize).CopyTo(span);

        var codeSpan = span.Slice(ResponseCodeOffset, ResponseCodeSize);
        switch (result)
        {
            case ActionResult.Success:
                "OK:200"u8.CopyTo(codeSpan);
                break;
            case ActionResult.BadRequest:
                "NG:400"u8.CopyTo(codeSpan);
                break;
            case ActionResult.NotFound:
                "NG:404"u8.CopyTo(codeSpan);
                break;
        }

        Utf8Formatter.TryFormat(body.Length, span.Slice(ResponseLengthOffset, ResponseLengthSize), out _, LengthFormat);

        response.Advance(ResponseHeaderSize);

        if (!body.IsEmpty)
        {
            span = response.GetSpan(body.Length);
            body.CopyTo(span);
            response.Advance(body.Length);
        }
    }

    private enum ReadResult
    {
        Success,
        Progress,
        ProtocolError
    }

    private sealed class State
    {
        public bool HeaderProcessed { get; set; }

        public int BodySize { get; set; }

        public void Reset()
        {
            HeaderProcessed = false;
            BodySize = 0;
        }
    }
}
