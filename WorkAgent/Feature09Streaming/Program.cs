using AgentSampleCore;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI.Chat;

// ===========================================================================
// Feature09Streaming : ストリーミング応答 (RunStreamingAsync)
// ---------------------------------------------------------------------------
// 回答を一括で受け取る RunAsync に対し、RunStreamingAsync は生成されたそばから
// 断片 (AgentResponseUpdate) を逐次受け取れる。長い回答でも待たずに表示できる。
//  ・各 update の ToString() がテキスト断片。
//  ・update.Contents にはツール呼び出し等の内容も流れてくる。
// 追加環境: 不要 (既存の Foundry chat 接続のみで動作)。追加パッケージ: なし。
// 使う主な API: AIAgent.RunStreamingAsync(...), AgentResponseUpdate
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
        "質問にはツールで実際の値を取得し、順序立てて日本語で説明してください。",
    name: "PcInfoAssistant",
    tools:
    [
        AIFunctionFactory.Create(PcTools.GetSystemInfo),
        AIFunctionFactory.Create(PcTools.GetDriveInfo),
    ]);

const string question = "このPCのOSと空きディスク容量を、順を追って説明して。";
Console.WriteLine($"You   > {question}");
Console.Write("Agent > ");

// RunStreamingAsync は断片を逐次返す。届いたそばから書き出す。
await foreach (var update in agent.RunStreamingAsync(question))
{
    // update が保持するツール呼び出しを行頭マーカーで可視化する。
    foreach (var content in update.Contents)
    {
        if (content is FunctionCallContent call)
        {
            Console.Write($"\n  [tool] {call.Name}\n  ");
        }
    }

    Console.Write(update); // テキスト断片
}

Console.WriteLine();

return 0;
