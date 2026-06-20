using AgentSampleCore;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI.Chat;

// ===========================================================================
// Feature01Tools : 関数ツール(Function Tools)
// ---------------------------------------------------------------------------
// Agent Framework の最も基本的な機能。
//  ・通常の C# メソッドに [Description] を付けるだけでツールになる(PcTools 参照)。
//  ・複数のツールを登録すると、モデルが質問に応じて必要なものだけを自動で選んで呼ぶ。
//  ・引数つきツール GetTopProcesses(int) では、引数もモデルが文脈から決めて渡す。
//  ・ツールの呼び出しと結果は応答メッセージ列に残るため、後から観測できる。
// 使う主な API: ChatClient.AsAIAgent(...), AIFunctionFactory.Create(...), AIAgent.RunAsync(...)
// ===========================================================================

var options = SampleHost.TryGetFoundryOptions();
if (options is null)
{
    return 1;
}

var chatClient = SampleHost.CreateChatClient(options);

// 4つの PC情報ツールを登録したエージェントを生成する。
AIAgent agent = chatClient.AsAIAgent(
    instructions:
        "あなたはこのPCの状態を調べるアシスタントです。" +
        "PC・OS・メモリ・ディスク・プロセスに関する質問には必ず適切なツールを使って実際の値を取得し、" +
        "結果を簡潔に日本語でまとめて答えてください。",
    name: "PcInfoAssistant",
    tools:
    [
        AIFunctionFactory.Create(PcTools.GetSystemInfo),
        AIFunctionFactory.Create(PcTools.GetMemoryInfo),
        AIFunctionFactory.Create(PcTools.GetDriveInfo),
        AIFunctionFactory.Create(PcTools.GetTopProcesses),
    ]);

// (1) 複数ツール + 引数つきツールが必要になる質問。
//     モデルは GetTopProcesses(topCount: 3) と GetDriveInfo() を選ぶことが期待される。
await AskAsync(agent, "メモリ使用量が多い上位3つのプロセスと、空きディスク容量を教えて。");

// (2) 1つのツールだけで足りる質問。モデルが必要なツールだけを選ぶ様子を確認する。
await AskAsync(agent, "このPCのOSと .NET のバージョンは?");

return 0;

// 質問を投げ、呼ばれたツールと最終回答を表示するヘルパー。
static async Task AskAsync(AIAgent agent, string question)
{
    Console.WriteLine($"You   > {question}");

    var response = await agent.RunAsync(question);

    // どのツールがどんな引数で呼ばれたかを、応答メッセージ列から観測する。
    foreach (var message in response.Messages)
    {
        foreach (var content in message.Contents)
        {
            if (content is FunctionCallContent call)
            {
                var args = call.Arguments is { Count: > 0 } a
                    ? string.Join(", ", a.Select(static x => $"{x.Key}={x.Value}"))
                    : "(引数なし)";
                Console.WriteLine($"  [tool] {call.Name}({args})");
            }
        }
    }

    Console.WriteLine($"Agent > {response}");
    Console.WriteLine();
}
