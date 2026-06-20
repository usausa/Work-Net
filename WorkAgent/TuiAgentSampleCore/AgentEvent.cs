namespace TuiAgentSampleCore;

/// <summary>
/// エージェント応答中に流れてくるストリーミングイベントの基底型。
/// 各 TUI はこれをパターンマッチして「最近のエージェント風」表示を構築する。
/// </summary>
public abstract record AgentEvent;

/// <summary>思考 (reasoning) フェーズの開始。</summary>
public sealed record ThinkingStarted : AgentEvent;

/// <summary>思考内容の断片。</summary>
/// <param name="Text">思考テキスト片。</param>
public sealed record ThinkingDelta(string Text) : AgentEvent;

/// <summary>思考フェーズの終了。</summary>
public sealed record ThinkingCompleted : AgentEvent;

/// <summary>ツール (関数) 呼び出しの開始。</summary>
/// <param name="Name">ツール名。</param>
/// <param name="Arguments">引数の表示用文字列。</param>
public sealed record ToolCallStarted(string Name, string Arguments) : AgentEvent;

/// <summary>ツール呼び出しの完了。</summary>
/// <param name="Name">ツール名。</param>
/// <param name="Result">実行結果。</param>
public sealed record ToolCallCompleted(string Name, string Result) : AgentEvent;

/// <summary>アシスタント本文のトークン断片 (ストリーミング)。</summary>
/// <param name="Text">本文テキスト片。</param>
public sealed record TextDelta(string Text) : AgentEvent;

/// <summary>応答全体の完了。</summary>
public sealed record ResponseCompleted : AgentEvent;
