namespace WorkWifi;

using System;

public enum WifiSecurityType
{
    Open,
    Wep,
    WpaPsk,
    WpaEap,
    Sae,
    Wpa3Eap,
    Owe,
}

public enum WifiStandard
{
    Legacy,
    N,
    Ac,
    Ax,
    Be,
}

public enum WifiChannelWidth
{
    Width20Mhz,
    Width40Mhz,
    Width80Mhz,
    Width80Plus80Mhz,
    Width160Mhz,
    Width320Mhz,
}

public sealed class WifiAccessPoint
{
    public string Ssid { get; init; } = string.Empty;

    public string Bssid { get; init; } = string.Empty;

    public int Frequency { get; init; }

    public int CenterFreq0 { get; init; }

    public int CenterFreq1 { get; init; }

    public WifiChannelWidth ChannelWidth { get; init; }

    public int Level { get; init; }

    public WifiSecurityType SecurityType { get; init; }

    public WifiStandard Standard { get; init; }

    public string Capabilities { get; init; } = string.Empty;

    public string OperatorFriendlyName { get; init; } = string.Empty;

    public string VenueName { get; init; } = string.Empty;

    public bool IsPasspointNetwork { get; init; }

    public bool IsHidden { get; init; }

    public bool IsConnected { get; init; }

    public long TimestampMicros { get; init; }

    public string DisplaySsid => IsHidden || string.IsNullOrEmpty(Ssid) ? "(SSID 非公開)" : Ssid;

    public string SubTitle =>
        !string.IsNullOrEmpty(VenueName) ? VenueName :
        !string.IsNullOrEmpty(OperatorFriendlyName) ? OperatorFriendlyName :
        Capabilities;

    public int Channel => Frequency switch
    {
        2484 => 14,
        >= 2412 and <= 2472 => ((Frequency - 2412) / 5) + 1,
        >= 5000 and <= 5980 => (Frequency - 5000) / 5,
        >= 5955 and <= 7115 => (Frequency - 5950) / 5,
        _ => 0,
    };

    public string Band => Frequency switch
    {
        < 3000 => "2.4 GHz",
        < 5925 => "5 GHz",
        _ => "6 GHz",
    };

    public int SignalLevel => Level switch
    {
        >= -50 => 4,
        >= -60 => 3,
        >= -70 => 2,
        >= -80 => 1,
        _ => 0,
    };

    public string SignalLabel => SignalLevel switch
    {
        4 => "とても強い",
        3 => "強い",
        2 => "普通",
        1 => "弱い",
        _ => "非常に弱い",
    };

    public string SignalIcon => SignalLevel switch
    {
        >= 1 => "📶",
        _ => "📵",
    };

    public string SecurityLabel => SecurityType switch
    {
        WifiSecurityType.Open => "オープン",
        WifiSecurityType.Wep => "WEP",
        WifiSecurityType.WpaPsk => "WPA2-PSK",
        WifiSecurityType.WpaEap => "WPA2-EAP",
        WifiSecurityType.Sae => "WPA3-SAE",
        WifiSecurityType.Wpa3Eap => "WPA3-EAP",
        WifiSecurityType.Owe => "OWE",
        _ => "不明",
    };

    public string SecurityIcon => SecurityType == WifiSecurityType.Open ? "🔓" : "🔒";

    public string StandardLabel => Standard switch
    {
        WifiStandard.Legacy => "Wi-Fi Legacy",
        WifiStandard.N => "Wi-Fi 4",
        WifiStandard.Ac => "Wi-Fi 5",
        WifiStandard.Ax => "Wi-Fi 6",
        WifiStandard.Be => "Wi-Fi 7",
        _ => "?",
    };

    public string ChannelWidthLabel => ChannelWidth switch
    {
        WifiChannelWidth.Width20Mhz => "20 MHz",
        WifiChannelWidth.Width40Mhz => "40 MHz",
        WifiChannelWidth.Width80Mhz => "80 MHz",
        WifiChannelWidth.Width80Plus80Mhz => "80+80 MHz",
        WifiChannelWidth.Width160Mhz => "160 MHz",
        WifiChannelWidth.Width320Mhz => "320 MHz",
        _ => "?",
    };

    public Color SignalColor => SignalLevel switch
    {
        4 => Color.FromArgb("#43A047"),
        3 => Color.FromArgb("#1E88E5"),
        2 => Color.FromArgb("#FB8C00"),
        1 => Color.FromArgb("#E53935"),
        _ => Color.FromArgb("#9E9E9E"),
    };

    public Color BandColor => Frequency switch
    {
        < 3000 => Color.FromArgb("#1E88E5"),
        < 5925 => Color.FromArgb("#8E24AA"),
        _ => Color.FromArgb("#E91E63"),
    };

    public Color SecurityColor => SecurityType switch
    {
        WifiSecurityType.Open => Color.FromArgb("#E53935"),
        WifiSecurityType.Wep => Color.FromArgb("#FB8C00"),
        WifiSecurityType.WpaPsk or WifiSecurityType.WpaEap => Color.FromArgb("#1E88E5"),
        WifiSecurityType.Sae or WifiSecurityType.Wpa3Eap or WifiSecurityType.Owe => Color.FromArgb("#43A047"),
        _ => Color.FromArgb("#9E9E9E"),
    };

    public double Bar1Opacity => SignalLevel >= 1 ? 1.0 : 0.18;

    public double Bar2Opacity => SignalLevel >= 2 ? 1.0 : 0.18;

    public double Bar3Opacity => SignalLevel >= 3 ? 1.0 : 0.18;

    public double Bar4Opacity => SignalLevel >= 4 ? 1.0 : 0.18;
}
