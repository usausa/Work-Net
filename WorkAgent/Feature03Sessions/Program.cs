using System.Text.Json;

using AgentSampleCore;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using OpenAI.Chat;

// ===========================================================================
// Feature03Sessions : セッションの永続化(会話状態の保存と復元)
// ---------------------------------------------------------------------------
// AgentSession は会話履歴などの状態を保持する。これを JSON にして保存しておけば、
// プロセスを終了しても、後で復元して同じ会話の続きから再開できる。
//  ・SerializeSessionAsync(session)  : セッション状態を JsonElement にする
//  ・DeserializeSessionAsync(json)   : JsonElement からセッションを復元する
// このサンプルは2回実行する想定:
//  1回目 … 新しいセッションで会話し、状態をファイルへ保存
//  2回目 … 保存した状態を復元し、前回の会話を覚えているか確認
// 使う主な API: AIAgent.CreateSessionAsync / SerializeSessionAsync / DeserializeSessionAsync
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
        "PC・OS・メモリ・ディスクに関する質問にはツールで実際の値を取得し、" +
        "直前までの会話の文脈も踏まえて簡潔に日本語で答えてください。",
    name: "PcInfoAssistant",
    tools:
    [
        AIFunctionFactory.Create(PcTools.GetSystemInfo),
        AIFunctionFactory.Create(PcTools.GetDriveInfo),
    ]);

var sessionFile = Path.Combine(AppContext.BaseDirectory, "pc-agent-session.json");

if (!File.Exists(sessionFile))
{
    // --- 1回目: 新しいセッションで会話し、最後に状態をファイルへ保存 ---
    Console.WriteLine("=== 1回目: 新しいセッションで会話します ===");
    var session = await agent.CreateSessionAsync();

    await ChatAsync(agent, session, "このPCのOS名だけ教えて。");
    await ChatAsync(agent, session, "それは何ビットのアーキテクチャ?"); // 直前の文脈を参照

    // セッション状態を JSON にしてファイルへ保存する。
    var state = await agent.SerializeSessionAsync(session);
    await File.WriteAllTextAsync(sessionFile, state.GetRawText());

    Console.WriteLine($"セッションを保存しました: {sessionFile}");
    Console.WriteLine("もう一度実行すると、保存した会話の続きから再開します。");
}
else
{
    // --- 2回目以降: 保存済みセッションを復元して会話を継続 ---
    Console.WriteLine("=== 2回目: 保存済みセッションを復元して継続します ===");
    var json = await File.ReadAllTextAsync(sessionFile);
    using var document = JsonDocument.Parse(json);
    var session = await agent.DeserializeSessionAsync(document.RootElement);

    // 前回の会話を覚えているか確認する(ツールを使わずに答えられるはず)。
    await ChatAsync(agent, session, "さっき教えてくれたOS名をもう一度言って。");

    // サンプルを繰り返し試せるよう、保存ファイルを削除して最初の状態に戻す。
    File.Delete(sessionFile);
    Console.WriteLine("セッションファイルを削除しました(次回はまた1回目から始まります)。");
}

return 0;

// 1ターン分の会話を行い、結果を表示するヘルパー。session を渡すと履歴が引き継がれる。
static async Task ChatAsync(AIAgent agent, AgentSession session, string message)
{
    Console.WriteLine($"You   > {message}");
    var response = await agent.RunAsync(message, session);
    Console.WriteLine($"Agent > {response}");
    Console.WriteLine();
}
