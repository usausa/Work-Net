namespace WorkML.Core;

// 1サンプリング点（チャンネル単位の生ログ）
public sealed class ChannelReading
{
    public string DeviceId { get; init; } = default!;

    public int ChannelNo { get; init; }

    public DateTime Timestamp { get; init; }

    public float Value { get; init; }          // 実測値（電圧など）
}
