using System.Diagnostics;
using System.Globalization;

using AgentSampleCore;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI.Chat;

// ===========================================================================
// Feature04Middleware : ミドルウェア(エージェントのパイプライン)
// ---------------------------------------------------------------------------
// AsBuilder() でエージェントをパイプライン化し、Use(...) で処理を差し込める。
// MEAI(Microsoft.Extensions.AI)と同じビルダー方式。ここでは2種類を示す:
//  (A) 実行ミドルウェア      : RunAsync 全体をはさんで前後で処理(所要時間の計測など)
//  (B) 関数呼び出しミドルウェア: ツール1回ごとの呼び出しをはさんで観測/加工
// このほか組み込みの .UseLogging() / .UseOpenTelemetry()(Feature08)も同様に差し込める。
// 使う主な API: AIAgent.AsBuilder(), AIAgentBuilder.Use(...), AIAgentBuilder.Build()
// ===========================================================================

var options = SampleHost.TryGetFoundryOptions();
if (options is null)
{
    return 1;
}

var chatClient = SampleHost.CreateChatClient(options);

// まずはツールを持つ素のエージェントを用意する。
AIAgent innerAgent = chatClient.AsAIAgent(
    instructions:
        "あなたはこのPCの状態を調べるアシスタントです。" +
        "質問にはツールで実際の値を取得し、簡潔に日本語でまとめて答えてください。",
    name: "PcInfoAssistant",
    tools:
    [
        AIFunctionFactory.Create(PcTools.GetSystemInfo),
        AIFunctionFactory.Create(PcTools.GetDriveInfo),
        AIFunctionFactory.Create(PcTools.GetTopProcesses),
    ]);

// AsBuilder() でパイプライン化し、2つのミドルウェアを差し込んでから Build() する。
var agent = innerAgent.AsBuilder()
    // (A) 実行ミドルウェア(引数5個のオーバーロード)。RunAsync 全体をはさむ。
    .Use(async (messages, session, runOptions, next, cancellationToken) =>
    {
        var stopwatch = Stopwatch.StartNew();
        Console.WriteLine("[run] エージェント実行を開始します");
        await next(messages, session, runOptions, cancellationToken);
        stopwatch.Stop();
        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"[run] 実行完了 ({stopwatch.ElapsedMilliseconds} ms)"));
    })
    // (B) 関数呼び出しミドルウェア(引数4個のオーバーロード)。ツール1回ごとをはさむ。
    .Use(async (agentInstance, context, next, cancellationToken) =>
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await next(context, cancellationToken); // 実際のツール呼び出し
        stopwatch.Stop();
        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  [tool] {context.Function.Name} -> {stopwatch.ElapsedMilliseconds} ms"));
        return result;
    })
    .Build();

// 複数ツールを使う質問。実行ミドルウェアが1回、関数ミドルウェアがツールの回数だけ発火する。
const string question = "このPCのOSと空きディスク、メモリ上位2プロセスをまとめて教えて。";
Console.WriteLine($"You   > {question}");
Console.WriteLine();

var response = await agent.RunAsync(question);

Console.WriteLine();
Console.WriteLine($"Agent > {response}");

return 0;
