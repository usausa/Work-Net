using System.Diagnostics;
using System.Globalization;

using AgentSampleCore;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI.Chat;

// ===========================================================================
// Feature08Telemetry : 可観測性(OpenTelemetry によるトレース)
// ---------------------------------------------------------------------------
// UseOpenTelemetry() をパイプラインに足すと、エージェントの実行やツール呼び出しが
// OpenTelemetry の Activity(スパン)として記録される。
//  ・本番では OTLP エクスポーター等で収集するが、ここでは依存を増やさず
//    ActivityListener で購読し、スパンをコンソールに出して可視化する。
//  ・EnableSensitiveData = true にすると、プロンプトや応答内容もタグに含まれる
//    (機微情報を含むため、実運用では取り扱いに注意)。
// 使う主な API: AIAgentBuilder.UseOpenTelemetry(...), System.Diagnostics.ActivityListener
// ===========================================================================

var options = SampleHost.TryGetFoundryOptions();
if (options is null)
{
    return 1;
}

var chatClient = SampleHost.CreateChatClient(options);

// UseOpenTelemetry に渡すソース名。同じ名前を ActivityListener で購読する。
const string sourceName = "Feature08.PcAgent";

// トレース(スパン)を購読してコンソールへ出力するリスナー。
using var listener = new ActivityListener
{
    ShouldListenTo = static source => source.Name == sourceName,
    Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
    ActivityStopped = static activity =>
    {
        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"[trace] {activity.DisplayName} ({activity.Duration.TotalMilliseconds:F0} ms)"));
        foreach (var tag in activity.TagObjects)
        {
            Console.WriteLine($"          {tag.Key} = {tag.Value}");
        }
    },
};
ActivitySource.AddActivityListener(listener);

// ツールを持つ素のエージェント。
AIAgent innerAgent = chatClient.AsAIAgent(
    instructions:
        "あなたはこのPCの状態を調べるアシスタントです。" +
        "質問にはツールで実際の値を取得し、簡潔に日本語でまとめて答えてください。",
    name: "PcInfoAssistant",
    tools:
    [
        AIFunctionFactory.Create(PcTools.GetSystemInfo),
        AIFunctionFactory.Create(PcTools.GetDriveInfo),
    ]);

// パイプラインに OpenTelemetry 計測を追加する。
var agent = innerAgent.AsBuilder()
    .UseOpenTelemetry(sourceName, static telemetry => telemetry.EnableSensitiveData = true)
    .Build();

const string question = "このPCのOSと空きディスク容量を教えて。";
Console.WriteLine($"You   > {question}");
Console.WriteLine();

var response = await agent.RunAsync(question);

Console.WriteLine();
Console.WriteLine($"Agent > {response}");

return 0;
