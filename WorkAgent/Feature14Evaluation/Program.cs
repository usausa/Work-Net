using AgentSampleCore;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI.Chat;

// ===========================================================================
// Feature14Evaluation : エージェントの評価 (LocalEvaluator)
// ---------------------------------------------------------------------------
// エージェントの応答が期待どおりかを自動でチェックする。LocalEvaluator は
// ローカルの検査関数だけで判定するため、評価のための追加 API 呼び出しが不要。
//  ・EvalChecks に組み込みの検査がある(キーワード・ツール呼び出し・非空など)。
//  ・agent.EvaluateAsync(質問列, evaluator) でエージェントを実行し、結果を採点する。
//  ・(より高度な「LLM as judge」評価は別パッケージ Microsoft.Extensions.AI.Evaluation)
// 追加環境: 不要 (検査はローカル。エージェント実行に既存の Foundry 接続を使う)。追加パッケージ: なし。
// 使う主な API: LocalEvaluator, EvalChecks, AIAgent.EvaluateAsync(...), AgentEvaluationResults
// ===========================================================================

var options = SampleHost.TryGetFoundryOptions();
if (options is null)
{
    return 1;
}

var chatClient = SampleHost.CreateChatClient(options);

AIAgent agent = chatClient.AsAIAgent(
    instructions:
        "あなたはこのPCの状態を調べるアシスタントです。" +
        "質問にはツールで実際の値を取得し、簡潔に日本語で答えてください。",
    name: "PcInfoAssistant",
    tools:
    [
        AIFunctionFactory.Create(PcTools.GetSystemInfo),
        AIFunctionFactory.Create(PcTools.GetDriveInfo),
    ]);

// ローカル検査だけで構成した評価器。
//  ・応答が空でない / "Windows" を含む / GetSystemInfo ツールを呼んだ、を確認する。
var evaluator = new LocalEvaluator(
[
    EvalChecks.NonEmpty(1),
    EvalChecks.KeywordCheck("Windows"),
    EvalChecks.ToolCalledCheck("GetSystemInfo"),
]);

// 複数の質問をまとめて評価できる。各質問が1つの評価項目(item)になる。
string[] queries =
[
    "このPCのOS名を教えて。",
    "このPCのOSは何?",
];
Console.WriteLine("評価対象の質問:");
foreach (var q in queries)
{
    Console.WriteLine($"  - {q}");
}

// EvaluateAsync はエージェントを各質問で実行し、その応答を上記の検査で採点する。
var results = await agent.EvaluateAsync(queries, evaluator);

// Passed / Total は「項目(質問)」単位。1項目でも検査に1つでも落ちると不合格になる。
Console.WriteLine();
Console.WriteLine("=== 評価結果 ===");
Console.WriteLine($"合格した項目数: {results.Passed} / {results.Total}");
Console.WriteLine($"全項目が合格  : {(results.AllPassed ? "はい" : "いいえ")}");

return 0;
