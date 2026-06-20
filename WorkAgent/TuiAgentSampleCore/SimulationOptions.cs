namespace TuiAgentSampleCore;

/// <summary>
/// 擬似エージェントの挙動 (表示名・各種ディレイ) を調整するための設定。
/// </summary>
public sealed class SimulationOptions
{
    /// <summary>エージェントの表示名。</summary>
    public string AgentName { get; set; } = "Aria";

    /// <summary>モデルの表示名。</summary>
    public string ModelName { get; set; } = "sim-opus (simulated)";

    /// <summary>思考トークン 1 片あたりの待ち時間 (ミリ秒)。</summary>
    public int ThinkingDelayMilliseconds { get; set; } = 180;

    /// <summary>本文トークン 1 片あたりの待ち時間 (ミリ秒)。</summary>
    public int TokenDelayMilliseconds { get; set; } = 24;

    /// <summary>ツール実行 1 回あたりの待ち時間 (ミリ秒)。</summary>
    public int ToolCallDelayMilliseconds { get; set; } = 450;
}
