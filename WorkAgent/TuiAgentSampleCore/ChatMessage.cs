namespace TuiAgentSampleCore;

/// <summary>
/// 会話履歴に積まれる確定済みメッセージ。
/// </summary>
/// <param name="Role">発話者種別。</param>
/// <param name="Content">本文 (アシスタントは Markdown 風テキスト)。</param>
public sealed record ChatMessage(ChatRole Role, string Content)
{
    /// <summary><see cref="ChatRole.Tool"/> の場合の呼び出しツール名。</summary>
    public string? ToolName { get; init; }
}
