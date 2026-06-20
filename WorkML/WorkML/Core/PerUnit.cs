namespace WorkML.Core;

// p.u.（標幺値）正規化と系列ID生成。異なる基本電圧(100V/200V)を同一スケールに揃える。
public static class PerUnit
{
    // 電圧を基本電圧で正規化（100V系/200V系を 1.0 中心に統一）
    public static float Voltage(float measured, DeviceSpec spec) => measured / spec.BaseVoltage;

    public static float Current(float measured, DeviceSpec spec) => measured / spec.RatedCurrent;

    // TimeGEN の系列ID: Site-Device-Channel を一意キーに（可変チャンネル/マルチサイトを吸収）
    public static string UniqueId(DeviceSpec spec, int channelNo) => $"{spec.SiteId}-{spec.DeviceId}-ch{channelNo}";
}
