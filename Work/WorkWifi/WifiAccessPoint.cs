namespace WorkWifi;

using System;
using System.Windows.Media;

public enum WifiSecurity
{
    Open,
    Wep,
    Wpa,
    Wpa2,
    Wpa2Enterprise,
    Wpa3,
    Wpa3Enterprise,
}

public enum WifiStandard
{
    LegacyA,
    LegacyB,
    LegacyG,
    N,
    Ac,
    Ax,
    Be,
}

public sealed class WifiAccessPoint
{
    public string Ssid { get; init; } = string.Empty;

    public string Bssid { get; init; } = string.Empty;

    public int Frequency { get; init; }

    public int Channel { get; init; }

    public int ChannelWidth { get; init; }

    public int Level { get; init; }

    public WifiSecurity Security { get; init; }

    public WifiStandard Standard { get; init; }

    public string Vendor { get; init; } = string.Empty;

    public bool IsHidden { get; init; }

    public bool IsConnected { get; init; }

    public DateTime LastSeen { get; init; }

    public string DisplaySsid => IsHidden || string.IsNullOrEmpty(Ssid) ? "(SSID 非表示)" : Ssid;

    public string Band => Frequency switch
    {
        < 3000 => "2.4 GHz",
        < 5925 => "5 GHz",
        _ => "6 GHz",
    };

    public int SignalBars => Level switch
    {
        >= -50 => 4,
        >= -60 => 3,
        >= -70 => 2,
        >= -80 => 1,
        _ => 0,
    };

    public string SignalIcon => SignalBars > 0 ? "📶" : "❌";

    public string SignalLabel => SignalBars switch
    {
        4 => "優",
        3 => "良",
        2 => "可",
        1 => "弱",
        _ => "圏外",
    };

    public string SecurityIcon => Security == WifiSecurity.Open ? "🔓" : "🔒";

    public string SecurityLabel => Security switch
    {
        WifiSecurity.Open => "オープン",
        WifiSecurity.Wep => "WEP",
        WifiSecurity.Wpa => "WPA",
        WifiSecurity.Wpa2 => "WPA2-PSK",
        WifiSecurity.Wpa2Enterprise => "WPA2-EAP",
        WifiSecurity.Wpa3 => "WPA3-SAE",
        WifiSecurity.Wpa3Enterprise => "WPA3-EAP",
        _ => "不明",
    };

    public string StandardLabel => Standard switch
    {
        WifiStandard.LegacyA => "Wi-Fi 1 (a)",
        WifiStandard.LegacyB => "Wi-Fi 1 (b)",
        WifiStandard.LegacyG => "Wi-Fi 3 (g)",
        WifiStandard.N => "Wi-Fi 4 (n)",
        WifiStandard.Ac => "Wi-Fi 5 (ac)",
        WifiStandard.Ax => "Wi-Fi 6 (ax)",
        WifiStandard.Be => "Wi-Fi 7 (be)",
        _ => "?",
    };

    public Brush SignalBrush => SignalBars switch
    {
        4 => Palette.SignalExcellent,
        3 => Palette.SignalGood,
        2 => Palette.SignalFair,
        1 => Palette.SignalWeak,
        _ => Palette.SignalNone,
    };

    public Brush BandBrush => Frequency switch
    {
        < 3000 => Palette.Band24,
        < 5925 => Palette.Band5,
        _ => Palette.Band6,
    };

    public Brush SecurityBrush => Security switch
    {
        WifiSecurity.Open => Palette.SecurityOpen,
        WifiSecurity.Wep => Palette.SecurityWeak,
        WifiSecurity.Wpa => Palette.SecurityWeak,
        WifiSecurity.Wpa2 or WifiSecurity.Wpa2Enterprise => Palette.SecurityOk,
        WifiSecurity.Wpa3 or WifiSecurity.Wpa3Enterprise => Palette.SecurityStrong,
        _ => Palette.SecurityOk,
    };

    public double Bar1Opacity => SignalBars >= 1 ? 1.0 : 0.18;

    public double Bar2Opacity => SignalBars >= 2 ? 1.0 : 0.18;

    public double Bar3Opacity => SignalBars >= 3 ? 1.0 : 0.18;

    public double Bar4Opacity => SignalBars >= 4 ? 1.0 : 0.18;
}

internal static class Palette
{
    public static readonly Brush SignalExcellent = Freeze(0x9E, 0xCE, 0x6A);
    public static readonly Brush SignalGood = Freeze(0x7D, 0xCF, 0xFF);
    public static readonly Brush SignalFair = Freeze(0xE0, 0xAF, 0x68);
    public static readonly Brush SignalWeak = Freeze(0xFF, 0x9E, 0x64);
    public static readonly Brush SignalNone = Freeze(0xF7, 0x76, 0x8E);

    public static readonly Brush Band24 = Freeze(0x7A, 0xA2, 0xF7);
    public static readonly Brush Band5 = Freeze(0xBB, 0x9A, 0xF7);
    public static readonly Brush Band6 = Freeze(0xF7, 0x76, 0x8E);

    public static readonly Brush SecurityOpen = Freeze(0xF7, 0x76, 0x8E);
    public static readonly Brush SecurityWeak = Freeze(0xFF, 0x9E, 0x64);
    public static readonly Brush SecurityOk = Freeze(0x7D, 0xCF, 0xFF);
    public static readonly Brush SecurityStrong = Freeze(0x9E, 0xCE, 0x6A);

    private static SolidColorBrush Freeze(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
