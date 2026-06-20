using System.Globalization;

using AgentSampleCore;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI.Chat;

// ===========================================================================
// Feature06ContextProvider : コンテキストプロバイダー(動的な文脈注入)
// ---------------------------------------------------------------------------
// AIContextProvider を使うと、実行のたびに「追加の文脈」を動的に差し込める。
//  ・ProvideAIContextAsync(...) をオーバーライドし、AIContext を返す。
//  ・AIContext.Instructions / Messages / Tools がその回の入力にマージされる。
//  ・例: 現在時刻やマシン名など、毎回変化する/その時点の情報を自動で渡せる。
//  ・InvokedAsync 側をオーバーライドすれば、実行結果から記憶を蓄積することもできる。
// 接続方法: ChatClientAgentOptions.AIContextProviders に登録する。
// 使う主な API: AIContextProvider, AIContext, ChatClientAgentOptions.AIContextProviders
// ===========================================================================

var options = SampleHost.TryGetFoundryOptions();
if (options is null)
{
    return 1;
}

var chatClient = SampleHost.CreateChatClient(options);

// ChatClientAgentOptions 経由でコンテキストプロバイダーを登録する。
var agentOptions = new ChatClientAgentOptions
{
    Name = "PcInfoAssistant",
    ChatOptions = new ChatOptions
    {
        Instructions =
            "あなたはこのPCの状態を答えるアシスタントです。" +
            "提供された[コンテキスト]情報があればそれを最優先で使い、簡潔に日本語で答えてください。",
        Tools = [AIFunctionFactory.Create(PcTools.GetSystemInfo)],
    },
    AIContextProviders = [new PcContextProvider()],
};

AIAgent agent = chatClient.AsAIAgent(agentOptions);

// プロバイダーが時刻・マシン名を毎回注入するので、ツールを使わずとも答えられる。
await AskAsync(agent, "今の時刻と、あなたが把握しているこのPCのマシン名・ログインユーザーを教えて。");

return 0;

static async Task AskAsync(AIAgent agent, string question)
{
    Console.WriteLine($"You   > {question}");
    var response = await agent.RunAsync(question);
    Console.WriteLine($"Agent > {response}");
}

// ---------------------------------------------------------------------------
// カスタムのコンテキストプロバイダー。
// 実行のたびに「現在時刻・マシン名・ユーザー名」を追加指示として注入する。
// (外部/非信頼ソースの情報を注入する場合はプロンプトインジェクション対策が必要)
// ---------------------------------------------------------------------------
internal sealed class PcContextProvider : AIContextProvider
{
    protected override ValueTask<AIContext> ProvideAIContextAsync(
        AIContextProvider.InvokingContext context,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.Now;
        var note = string.Create(
            CultureInfo.InvariantCulture,
            $"[コンテキスト] 現在時刻: {now:yyyy-MM-dd HH:mm:ss zzz} / マシン名: {Environment.MachineName} / ユーザー: {Environment.UserName}");

        // Instructions に入れた内容はその回の入力にマージされてモデルへ渡る。
        var aiContext = new AIContext
        {
            Instructions = note,
        };

        return ValueTask.FromResult(aiContext);
    }
}
