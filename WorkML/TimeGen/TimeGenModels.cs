namespace WorkML.TimeGen;

using System.Text.Json.Serialization;

// long 形式の1点（unique_id, ds, y）。Nixtla/TimeGEN の標準入力形式。
public sealed class SeriesPoint
{
    [JsonPropertyName("unique_id")]
    public string UniqueId { get; init; } = default!;

    [JsonPropertyName("ds")]
    public DateTime Ds { get; init; }

    [JsonPropertyName("y")]
    public double Y { get; init; }              // p.u. 正規化済みの値
}

// 予測リクエスト。JSON フィールド名・ルートはデプロイ時の公式 API リファレンスで最終確認すること（本型は雛形）。
public sealed class ForecastRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = "timegpt-1";

    [JsonPropertyName("freq")]
    public string Freq { get; init; } = "5min";     // 5分間隔

    [JsonPropertyName("h")]
    public int H { get; init; } = 12;               // 予測ホライズン（5分×12 = 1時間先）

    [JsonPropertyName("level")]
    public int[]? Level { get; init; }              // 予測区間 例: [80, 95]

    [JsonPropertyName("finetune_steps")]
    public int FinetuneSteps { get; init; }         // 0 = 純 zero-shot

    [JsonPropertyName("y")]
    public IReadOnlyList<SeriesPoint> Y { get; init; } = default!;
}

public sealed class ForecastResponse
{
    [JsonPropertyName("forecast")]
    public IReadOnlyList<ForecastPoint> Forecast { get; init; } = [];
}

public sealed class ForecastPoint
{
    [JsonPropertyName("unique_id")]
    public string UniqueId { get; init; } = default!;

    [JsonPropertyName("ds")]
    public DateTime Ds { get; init; }

    [JsonPropertyName("value")]
    public double Value { get; init; }
}
