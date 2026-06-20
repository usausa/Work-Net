namespace TuiAgentSampleCore;

/// <summary>
/// 1 つの会話セッションを表す抽象。履歴を保持し、応答をイベントストリームとして返す。
/// </summary>
/// <remarks>
/// 実エージェントへの差し替えは、この interface を実装した別クラス
/// (例: <c>Microsoft.Extensions.AI.IChatClient</c> をラップした実装) を用意し、
/// 各 TUI の生成箇所で <see cref="SimulatedAgentConversation"/> と入れ替えるだけでよい。
/// </remarks>
public interface IAgentConversation
{
    /// <summary>エージェントの表示名。</summary>
    string AgentName { get; }

    /// <summary>モデルの表示名。</summary>
    string ModelName { get; }

    /// <summary>これまでの確定済み会話履歴。</summary>
    IReadOnlyList<ChatMessage> History { get; }

    /// <summary>
    /// 利用者メッセージを送り、応答イベントを非同期ストリームで受け取る。
    /// </summary>
    /// <param name="userMessage">利用者の入力。</param>
    /// <param name="cancellationToken">キャンセル用トークン。</param>
    /// <returns>応答イベントの非同期シーケンス。</returns>
    IAsyncEnumerable<AgentEvent> SendAsync(string userMessage, CancellationToken cancellationToken = default);
}
