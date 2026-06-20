using AgentSampleCore;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI.Chat;

// ===========================================================================
// Feature10Rag : 検索拡張生成 (RAG / TextSearchProvider)
// ---------------------------------------------------------------------------
// TextSearchProvider に「検索処理」を渡すと、毎回の実行前に関連情報を検索し、
// その結果を文脈へ自動注入してくれる。モデルは PC の実値(ツール)＋ナレッジ
// (検索結果)の両方を根拠に回答できる。
//  ・検索処理は Func<query, ct, Task<IEnumerable<TextSearchResult>>> で与える。
//  ・本サンプルは外部DB等を使わず、インメモリ配列を検索して RAG を完結させる。
// 追加環境: 不要 (既存の Foundry chat 接続のみ。検索はインメモリ)。追加パッケージ: なし。
// 使う主な API: TextSearchProvider, TextSearchProvider.TextSearchResult, AIContextProviders
// ===========================================================================

var options = SampleHost.TryGetFoundryOptions();
if (options is null)
{
    return 1;
}

var chatClient = SampleHost.CreateChatClient(options);

// 社内ナレッジ(本来はベクタDBや検索サービス。ここではインメモリ配列で代用)。
var knowledge = new (string Title, string Body)[]
{
    ("ディスク空き容量ポリシー", "社内基準では、ドライブの空き容量が全体の10%を下回ったら警告、5%未満は緊急対応とする。"),
    ("推奨メモリ", "開発機の推奨物理メモリは32GB以上。16GBは最小構成とする。"),
    ("OSサポート方針", "社内標準OSは Windows 11。Windows 10 は延長サポート期限に注意し、計画的に移行する。"),
};

// 検索処理: クエリ語に一致する文書を返す(本来は意味検索)。一致が無ければ全件返す。
var ragProvider = new TextSearchProvider(
    (query, cancellationToken) =>
    {
        var hits = knowledge
            .Where(doc => QueryMatches(query, doc.Title + " " + doc.Body))
            .Select(static doc => new TextSearchProvider.TextSearchResult { SourceName = doc.Title, Text = doc.Body })
            .ToList();

        var results = hits.Count > 0
            ? hits
            : knowledge.Select(static doc => new TextSearchProvider.TextSearchResult { SourceName = doc.Title, Text = doc.Body }).ToList();

        return Task.FromResult<IEnumerable<TextSearchProvider.TextSearchResult>>(results);
    },
    new TextSearchProviderOptions());

// RAG プロバイダーを登録したエージェント。
var agentOptions = new ChatClientAgentOptions
{
    Name = "PcAdvisor",
    ChatOptions = new ChatOptions
    {
        Instructions =
            "あなたはPC運用アドバイザーです。提供された社内ナレッジ(検索結果)を根拠に、" +
            "ツールで取得したPCの実値と照らし合わせて助言してください。" +
            "根拠が無いことは推測せず「不明」と答えてください。",
        Tools =
        [
            AIFunctionFactory.Create(PcTools.GetDriveInfo),
            AIFunctionFactory.Create(PcTools.GetMemoryInfo),
        ],
    },
    AIContextProviders = [ragProvider],
};

AIAgent agent = chatClient.AsAIAgent(agentOptions);

const string question = "このPCの空きディスクとメモリは、社内基準に照らして問題ない? 根拠も示して。";
Console.WriteLine($"You   > {question}");

var response = await agent.RunAsync(question);

Console.WriteLine($"Agent > {response}");

return 0;

// クエリ中の2文字以上の語が対象テキストに含まれるかの素朴な一致判定。
static bool QueryMatches(string query, string text)
    => query
        .Split([' ', '\t', '\n', '\r', '、', '。', '?', '?', '!', '!'], StringSplitOptions.RemoveEmptyEntries)
        .Where(static token => token.Length >= 2)
        .Any(token => text.Contains(token, StringComparison.OrdinalIgnoreCase));
