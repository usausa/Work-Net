using AgentSampleCore;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI.Chat;

// ===========================================================================
// Feature11Todo : TODO 管理 (TodoProvider)
// ---------------------------------------------------------------------------
// TodoProvider は AIContextProvider の一種で、エージェントに「TODO 管理ツール」と
// 関連指示を自動で与える。複数ステップの長めの作業を、計画→実行→完了で
// 進めさせたいときに使う。
//  ・登録するだけで、エージェントが TODO の作成・更新を自分で行う。
//  ・実行後、GetAllTodosAsync(session) で TODO の最終状態を取得できる。
//
// ⚠ 実験的API: TodoProvider は GA パッケージ(1.10.0)で Experimental (診断 MAAI001) 指定。
//    「評価目的のみ・将来変更/削除あり」とされるため、該当箇所のみ #pragma で MAAI001 を
//    局所的に抑制している。本番採用時は最新の提供状況を確認すること。
//
// 追加環境: 不要 (既存の Foundry chat 接続のみ)。追加パッケージ: なし。
// 使う主な API: TodoProvider, TodoProviderOptions, TodoProvider.GetAllTodosAsync(...)
// ===========================================================================

var options = SampleHost.TryGetFoundryOptions();
if (options is null)
{
    return 1;
}

var chatClient = SampleHost.CreateChatClient(options);

// MAAI001(実験的API)の抑制は、実験的型を生成するこの箇所だけに限定する。
#pragma warning disable MAAI001
using var todoProvider = new TodoProvider(new TodoProviderOptions
{
    Instructions = "複数の点検項目がある作業では、まず TODO を作成し、各項目が終わるたびに完了に更新してください。",
});
#pragma warning restore MAAI001

var agentOptions = new ChatClientAgentOptions
{
    Name = "PcDiagnosticAssistant",
    ChatOptions = new ChatOptions
    {
        Instructions =
            "あなたはPC診断アシスタントです。依頼された点検を TODO で管理しながら順に実施し、" +
            "最後に結果の総評を日本語でまとめてください。",
        Tools =
        [
            AIFunctionFactory.Create(PcTools.GetSystemInfo),
            AIFunctionFactory.Create(PcTools.GetMemoryInfo),
            AIFunctionFactory.Create(PcTools.GetDriveInfo),
            AIFunctionFactory.Create(PcTools.GetTopProcesses),
        ],
    },
    AIContextProviders = [todoProvider],
};

AIAgent agent = chatClient.AsAIAgent(agentOptions);

// TODO の状態を後から取得するため、セッションを使う。
var session = await agent.CreateSessionAsync();

const string question =
    "このPCの健康診断をして。OS情報・メモリ・ディスク空き・メモリ上位プロセスの4点を順に点検し、最後に総評を。";
Console.WriteLine($"You   > {question}");

var response = await agent.RunAsync(question, session);
Console.WriteLine($"Agent > {response}");

// エージェントが管理した TODO の最終状態を表示する。
var todos = await todoProvider.GetAllTodosAsync(session);
Console.WriteLine();
Console.WriteLine("=== エージェントが管理した TODO ===");
foreach (var todo in todos)
{
    Console.WriteLine($"  {(todo.IsComplete ? "[x]" : "[ ]")} {todo.Title}");
}

return 0;
