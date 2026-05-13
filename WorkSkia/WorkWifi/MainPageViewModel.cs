namespace WorkWifi;

using System;
using System.Collections.ObjectModel;

public sealed class MainPageViewModel
{
    public ObservableCollection<WifiAccessPoint> AccessPoints { get; }

    public MainPageViewModel()
    {
        var bootMicros = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000L;
        AccessPoints =
        [
            new WifiAccessPoint
            {
                Ssid = "HomeNet_5G",
                Bssid = "ac:84:c6:11:22:33",
                Frequency = 5180,
                CenterFreq0 = 5210,
                ChannelWidth = WifiChannelWidth.Width80Mhz,
                Level = -42,
                SecurityType = WifiSecurityType.Sae,
                Standard = WifiStandard.Ax,
                Capabilities = "[WPA3-SAE-CCMP][RSN-SAE-CCMP][ESS]",
                IsConnected = true,
                TimestampMicros = bootMicros,
            },
            new WifiAccessPoint
            {
                Ssid = "HomeNet_2G",
                Bssid = "ac:84:c6:11:22:34",
                Frequency = 2437,
                CenterFreq0 = 2437,
                ChannelWidth = WifiChannelWidth.Width20Mhz,
                Level = -55,
                SecurityType = WifiSecurityType.WpaPsk,
                Standard = WifiStandard.N,
                Capabilities = "[WPA2-PSK-CCMP][ESS]",
                TimestampMicros = bootMicros,
            },
            new WifiAccessPoint
            {
                Ssid = "Office-Internal",
                Bssid = "b8:27:eb:f1:23:45",
                Frequency = 5745,
                CenterFreq0 = 5775,
                ChannelWidth = WifiChannelWidth.Width160Mhz,
                Level = -50,
                SecurityType = WifiSecurityType.Wpa3Eap,
                Standard = WifiStandard.Ax,
                Capabilities = "[WPA3-EAP-CCMP][ESS]",
                OperatorFriendlyName = "Acme Corp.",
                IsPasspointNetwork = true,
                TimestampMicros = bootMicros,
            },
            new WifiAccessPoint
            {
                Ssid = "WiFi7-FastNet",
                Bssid = "70:8b:cd:11:22:33",
                Frequency = 6135,
                CenterFreq0 = 6175,
                ChannelWidth = WifiChannelWidth.Width320Mhz,
                Level = -58,
                SecurityType = WifiSecurityType.Sae,
                Standard = WifiStandard.Be,
                Capabilities = "[WPA3-SAE-GCMP-256][ESS][MLO]",
                TimestampMicros = bootMicros,
            },
            new WifiAccessPoint
            {
                Ssid = "Starbucks_Free",
                Bssid = "00:0c:e7:33:44:55",
                Frequency = 2437,
                CenterFreq0 = 2437,
                ChannelWidth = WifiChannelWidth.Width20Mhz,
                Level = -68,
                SecurityType = WifiSecurityType.Owe,
                Standard = WifiStandard.Ac,
                Capabilities = "[OWE][ESS]",
                VenueName = "Starbucks 渋谷店",
                IsPasspointNetwork = true,
                TimestampMicros = bootMicros,
            },
            new WifiAccessPoint
            {
                Ssid = "aterm-a8b9c2-g",
                Bssid = "f0:79:59:aa:bb:cc",
                Frequency = 2462,
                CenterFreq0 = 2462,
                ChannelWidth = WifiChannelWidth.Width40Mhz,
                Level = -72,
                SecurityType = WifiSecurityType.WpaPsk,
                Standard = WifiStandard.N,
                Capabilities = "[WPA2-PSK-CCMP][WPS][ESS]",
                TimestampMicros = bootMicros,
            },
            new WifiAccessPoint
            {
                Ssid = string.Empty,
                IsHidden = true,
                Bssid = "de:ad:be:ef:00:01",
                Frequency = 5500,
                CenterFreq0 = 5530,
                ChannelWidth = WifiChannelWidth.Width80Mhz,
                Level = -65,
                SecurityType = WifiSecurityType.WpaPsk,
                Standard = WifiStandard.Ac,
                Capabilities = "[WPA2-PSK-CCMP][ESS]",
                TimestampMicros = bootMicros,
            },
            new WifiAccessPoint
            {
                Ssid = "elecom-2g",
                Bssid = "44:21:91:12:34:56",
                Frequency = 2412,
                CenterFreq0 = 2412,
                ChannelWidth = WifiChannelWidth.Width20Mhz,
                Level = -78,
                SecurityType = WifiSecurityType.WpaPsk,
                Standard = WifiStandard.Legacy,
                Capabilities = "[WPA-PSK-CCMP+TKIP][WPA2-PSK-CCMP+TKIP][ESS]",
                TimestampMicros = bootMicros,
            },
            new WifiAccessPoint
            {
                Ssid = "RetroNet",
                Bssid = "00:11:22:99:88:77",
                Frequency = 2467,
                CenterFreq0 = 2467,
                ChannelWidth = WifiChannelWidth.Width20Mhz,
                Level = -85,
                SecurityType = WifiSecurityType.Wep,
                Standard = WifiStandard.Legacy,
                Capabilities = "[WEP][ESS]",
                TimestampMicros = bootMicros,
            },
            new WifiAccessPoint
            {
                Ssid = "GuestWiFi",
                Bssid = "ac:84:c6:11:22:35",
                Frequency = 2437,
                CenterFreq0 = 2437,
                ChannelWidth = WifiChannelWidth.Width40Mhz,
                Level = -90,
                SecurityType = WifiSecurityType.Open,
                Standard = WifiStandard.N,
                Capabilities = "[ESS]",
                TimestampMicros = bootMicros,
            },
        ];
    }
}
