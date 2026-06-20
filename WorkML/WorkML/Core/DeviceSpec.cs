namespace WorkML.Core;

// 装置マスタ（p.u. 正規化に必要なメタデータ）
public sealed class DeviceSpec
{
    public string DeviceId { get; init; } = default!;

    public string SiteId { get; init; } = default!;

    public float BaseVoltage { get; init; }    // 基本電圧 100 / 200 など

    public float RatedCurrent { get; init; }   // 定格電流
}
