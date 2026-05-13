namespace WorkWifi;

using System;
using System.Collections.ObjectModel;

public sealed class MainWindowViewModel
{
    public ObservableCollection<WifiAccessPoint> AccessPoints { get; }

    public MainWindowViewModel()
    {
        var now = DateTime.Now;
        AccessPoints =
        [
            new WifiAccessPoint
            {
                Ssid = "HomeNet_5G",
                Bssid = "AC:84:C6:11:22:33",
                Frequency = 5180,
                Channel = 36,
                ChannelWidth = 80,
                Level = -42,
                Security = WifiSecurity.Wpa3,
                Standard = WifiStandard.Ax,
                Vendor = "Buffalo Inc.",
                IsConnected = true,
                LastSeen = now,
            },
            new WifiAccessPoint
            {
                Ssid = "HomeNet_2G",
                Bssid = "AC:84:C6:11:22:34",
                Frequency = 2437,
                Channel = 6,
                ChannelWidth = 20,
                Level = -55,
                Security = WifiSecurity.Wpa2,
                Standard = WifiStandard.N,
                Vendor = "Buffalo Inc.",
                LastSeen = now,
            },
            new WifiAccessPoint
            {
                Ssid = "Office-Internal",
                Bssid = "B8:27:EB:F1:23:45",
                Frequency = 5745,
                Channel = 149,
                ChannelWidth = 160,
                Level = -50,
                Security = WifiSecurity.Wpa3Enterprise,
                Standard = WifiStandard.Ax,
                Vendor = "Cisco Systems",
                LastSeen = now,
            },
            new WifiAccessPoint
            {
                Ssid = "WiFi7-FastNet",
                Bssid = "70:8B:CD:11:22:33",
                Frequency = 6135,
                Channel = 37,
                ChannelWidth = 160,
                Level = -58,
                Security = WifiSecurity.Wpa3,
                Standard = WifiStandard.Be,
                Vendor = "TP-Link",
                LastSeen = now,
            },
            new WifiAccessPoint
            {
                Ssid = "aterm-a8b9c2-g",
                Bssid = "F0:79:59:AA:BB:CC",
                Frequency = 2462,
                Channel = 11,
                ChannelWidth = 40,
                Level = -68,
                Security = WifiSecurity.Wpa2,
                Standard = WifiStandard.Ac,
                Vendor = "NEC Platforms",
                LastSeen = now,
            },
            new WifiAccessPoint
            {
                Ssid = "elecom-2g",
                Bssid = "44:21:91:12:34:56",
                Frequency = 2412,
                Channel = 1,
                ChannelWidth = 20,
                Level = -72,
                Security = WifiSecurity.Wpa,
                Standard = WifiStandard.LegacyG,
                Vendor = "ELECOM",
                LastSeen = now,
            },
            new WifiAccessPoint
            {
                Ssid = "FreeWifi-Spot",
                Bssid = "00:0C:E7:33:44:55",
                Frequency = 2437,
                Channel = 6,
                ChannelWidth = 20,
                Level = -78,
                Security = WifiSecurity.Open,
                Standard = WifiStandard.N,
                Vendor = "Cisco Meraki",
                LastSeen = now,
            },
            new WifiAccessPoint
            {
                Ssid = string.Empty,
                IsHidden = true,
                Bssid = "DE:AD:BE:EF:00:01",
                Frequency = 5500,
                Channel = 100,
                ChannelWidth = 80,
                Level = -65,
                Security = WifiSecurity.Wpa2,
                Standard = WifiStandard.Ac,
                Vendor = "ASUSTeK",
                LastSeen = now,
            },
            new WifiAccessPoint
            {
                Ssid = "RetroNet",
                Bssid = "00:11:22:99:88:77",
                Frequency = 2467,
                Channel = 12,
                ChannelWidth = 20,
                Level = -85,
                Security = WifiSecurity.Wep,
                Standard = WifiStandard.LegacyB,
                Vendor = "Linksys",
                LastSeen = now,
            },
            new WifiAccessPoint
            {
                Ssid = "GuestWiFi",
                Bssid = "AC:84:C6:11:22:35",
                Frequency = 2437,
                Channel = 6,
                ChannelWidth = 40,
                Level = -90,
                Security = WifiSecurity.Open,
                Standard = WifiStandard.N,
                Vendor = "Buffalo Inc.",
                LastSeen = now,
            },
        ];
    }
}
