using AgentSampleCore;

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Compaction;
using Microsoft.Extensions.AI;

using OpenAI.Chat;

// ===========================================================================
// Feature13Compaction : 会話履歴の圧縮 (Compaction)
// ---------------------------------------------------------------------------
// 会話が長くなると文脈ウィンドウを圧迫する。CompactionProvider に圧縮ストラテジを
// 与えると、毎回の実行前に古いメッセージを自動で間引き／要約して長さを抑える。
//  ・SlidingWindowCompactionStrategy: 古いユーザーターンから順に削る(LLM不要・決定的)。
//  ・トリガーは CompactionTriggers で指定(例: MessagesExceed(N))。
//  ・要約で残したい場合は SummarizationCompactionStrategy も選べる(LLM を使用)。
//
// ⚠ 実験的API: Compaction 系 (CompactionProvider / *CompactionStrategy / CompactionTriggers) は
//    GA パッケージ(1.10.0)で Experimental (診断 MAAI001) 指定。「評価目的のみ・将来変更/削除あり」
//    とされるため、該当箇所のみ #pragma で MAAI001 を局所的に抑制している。
//
// 追加環境: 不要 (本サンプルは決定的ストラテジで LLM 追加呼び出しなし)。追加パッケージ: なし。
// 使う主な API: CompactionProvider, SlidingWindowCompactionStrategy, CompactionTriggers
// ===========================================================================

var options = SampleHost.TryGetFoundryOptions();
if (options is null)
{
    return 1;
}

var chatClient = SampleHost.CreateChatClient(options);

// MAAI001(実験的API)の抑制は、実験的型を生成するこの箇所だけに限定する。
#pragma warning disable MAAI001
// 6メッセージを超えたら、直近の1ターンを残して古い履歴を間引くストラテジ。
var strategy = new SlidingWindowCompactionStrategy(CompactionTriggers.MessagesExceed(6), 1);
var compactionProvider = new CompactionProvider(strategy);
#pragma warning restore MAAI001

var agentOptions = new ChatClientAgentOptions
{
    Name = "PcChat",
    ChatOptions = new ChatOptions
    {
        Instructions = "あなたはこのPCについて答えるアシスタントです。簡潔に日本語で答えてください。",
        Tools =
        [
            AIFunctionFactory.Create(PcTools.GetSystemInfo),
            AIFunctionFactory.Create(PcTools.GetDriveInfo),
            AIFunctionFactory.Create(PcTools.GetMemoryInfo),
        ],
    },
    AIContextProviders = [compactionProvider],
};

AIAgent agent = chatClient.AsAIAgent(agentOptions);
var session = await agent.CreateSessionAsync();

// 何ターンも続けると、古いやり取りは圧縮で文脈から外れていく。
// 最後の「最初の質問は?」は、履歴が間引かれていると答えにくくなる(=圧縮の効果)。
string[] turns =
[
    "このPCのOS名は?",
    "メモリの総量は?",
    "Cドライブの空き容量は?",
    "論理プロセッサ数は?",
    "ここまでで一番最初に私が聞いた質問は何だった?",
];

foreach (var turn in turns)
{
    Console.WriteLine($"You   > {turn}");
    var response = await agent.RunAsync(turn, session);
    Console.WriteLine($"Agent > {response}");
    Console.WriteLine();
}

return 0;
