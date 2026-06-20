using AgentSampleCore;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI.Chat;

// ===========================================================================
// Feature12FileAccess : ファイルアクセス (FileAccessProvider)
// ---------------------------------------------------------------------------
// FileAccessProvider は AIContextProvider の一種で、エージェントに
// 「保存・読み取り・一覧・検索・削除」のファイルツールを与える。保存先は
// AgentFileStore で差し替えられ、ここでは InMemoryAgentFileStore を使うため
// 実ディスクへの書き込みは発生しない。
//
// ⚠ 実験的API: FileAccessProvider / InMemoryAgentFileStore は GA パッケージ(1.10.0)で
//    Experimental (診断 MAAI001) 指定。「評価目的のみ・将来変更/削除あり」とされるため、
//    該当箇所のみ #pragma で MAAI001 を局所的に抑制している。
//
// 追加環境: 不要 (保存先はインメモリ。実ファイルは作られない)。追加パッケージ: なし。
// 使う主な API: FileAccessProvider, InMemoryAgentFileStore, AIContextProviders
// ===========================================================================

var options = SampleHost.TryGetFoundryOptions();
if (options is null)
{
    return 1;
}

var chatClient = SampleHost.CreateChatClient(options);

// MAAI001(実験的API)の抑制は、実験的型を生成するこの箇所だけに限定する。
#pragma warning disable MAAI001
// 保存先(インメモリ)。FileSystemAgentFileStore に変えれば実ディスクにも保存できる。
var fileStore = new InMemoryAgentFileStore();

var fileProvider = new FileAccessProvider(fileStore, new FileAccessProviderOptions
{
    Instructions = "ファイルの保存・読み取りが必要なときは、提供されたファイルツールを使ってください。",
});
#pragma warning restore MAAI001

var agentOptions = new ChatClientAgentOptions
{
    Name = "PcReportFiler",
    ChatOptions = new ChatOptions
    {
        Instructions =
            "あなたはPC情報を記録するアシスタントです。指示に従って PC 情報を取得し、" +
            "ファイルに保存したり読み返したりしてください。",
        Tools =
        [
            AIFunctionFactory.Create(PcTools.GetSystemInfo),
            AIFunctionFactory.Create(PcTools.GetDriveInfo),
        ],
    },
    AIContextProviders = [fileProvider],
};

AIAgent agent = chatClient.AsAIAgent(agentOptions);

// 保存→読み返しを同じセッションで行い、ファイルツールの往復を確認する。
var session = await agent.CreateSessionAsync();

await AskAsync(agent, session, "このPCのシステム情報を取得して、pc-info.txt というファイルに保存して。");
await AskAsync(agent, session, "保存した pc-info.txt の内容をそのまま読み上げて。");

return 0;

static async Task AskAsync(AIAgent agent, AgentSession session, string message)
{
    Console.WriteLine($"You   > {message}");
    var response = await agent.RunAsync(message, session);
    Console.WriteLine($"Agent > {response}");
    Console.WriteLine();
}
