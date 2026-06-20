using AgentSampleCore;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI.Chat;

// ===========================================================================
// Feature07MultiAgent : マルチエージェント連携(エージェントのツール化)
// ---------------------------------------------------------------------------
// 専門エージェントを AsAIFunction() でツール化すると、別の「調整役」エージェントが
// それらをツールとして呼び出せる。これにより役割分担した多段の構成を作れる。
//  ・各専門エージェントは自分のツールだけを持つ(関心の分離)。
//  ・調整役は専門エージェント(=ツール)を必要に応じて呼び、回答を統合する。
// (専用パッケージ Microsoft.Agents.AI.Workflows を使うと、より高度な
//   グラフ型ワークフロー〔逐次/並列/分岐〕も組めるが、ここでは追加依存なしで実現する)
// 使う主な API: AIAgent.AsAIFunction(...), AIFunctionFactoryOptions
// ===========================================================================

var options = SampleHost.TryGetFoundryOptions();
if (options is null)
{
    return 1;
}

var chatClient = SampleHost.CreateChatClient(options);

// --- 専門エージェント1: ハードウェア担当(OS・CPU・メモリ) ---
AIAgent hardwareAgent = chatClient.AsAIAgent(
    instructions:
        "あなたはハードウェア専門のアシスタントです。" +
        "OS・CPU・メモリに関する質問にツールで実際の値を取得して簡潔に答えます。",
    name: "HardwareSpecialist",
    tools:
    [
        AIFunctionFactory.Create(PcTools.GetSystemInfo),
        AIFunctionFactory.Create(PcTools.GetMemoryInfo),
    ]);

// --- 専門エージェント2: ストレージ/プロセス担当 ---
AIAgent storageAgent = chatClient.AsAIAgent(
    instructions:
        "あなたはストレージとプロセス専門のアシスタントです。" +
        "ディスク容量やプロセスに関する質問にツールで実際の値を取得して簡潔に答えます。",
    name: "StorageSpecialist",
    tools:
    [
        AIFunctionFactory.Create(PcTools.GetDriveInfo),
        AIFunctionFactory.Create(PcTools.GetTopProcesses),
    ]);

// 専門エージェントを「ツール」に変換する。名前と説明が調整役の振り分け判断に使われる。
var hardwareTool = hardwareAgent.AsAIFunction(new AIFunctionFactoryOptions
{
    Name = "ask_hardware_specialist",
    Description = "OS・CPU・メモリなどハードウェア面の調査を、ハードウェア専門エージェントに依頼します。",
});

var storageTool = storageAgent.AsAIFunction(new AIFunctionFactoryOptions
{
    Name = "ask_storage_specialist",
    Description = "ディスクの空き容量やメモリ消費プロセスの調査を、ストレージ専門エージェントに依頼します。",
});

// --- 調整役エージェント: 2つの専門エージェント(ツール)を束ねて統合回答する ---
AIAgent coordinator = chatClient.AsAIAgent(
    instructions:
        "あなたは調整役です。ハードウェアに関することは ask_hardware_specialist、" +
        "ディスクやプロセスに関することは ask_storage_specialist に委譲してください。" +
        "両方から得た回答を統合し、日本語で分かりやすくまとめてください。",
    name: "Coordinator",
    tools: [hardwareTool, storageTool]);

const string question = "このPCのハードウェア概要(OS・CPU・メモリ)と、ディスクの空き状況・メモリ上位プロセスをまとめて。";
Console.WriteLine($"You   > {question}");
Console.WriteLine();

var response = await coordinator.RunAsync(question);

Console.WriteLine($"Coordinator > {response}");

return 0;
