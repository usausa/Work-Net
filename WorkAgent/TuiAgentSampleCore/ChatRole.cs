namespace TuiAgentSampleCore;

/// <summary>
/// 会話メッセージの発話者種別。
/// </summary>
public enum ChatRole
{
    /// <summary>利用者。</summary>
    User,

    /// <summary>アシスタント (エージェント本体)。</summary>
    Assistant,

    /// <summary>ツール (関数呼び出し) の実行結果。</summary>
    Tool
}
