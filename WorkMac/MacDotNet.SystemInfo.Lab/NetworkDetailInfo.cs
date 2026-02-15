namespace MacDotNet.SystemInfo.Lab;

using System.Diagnostics;
using System.Runtime.InteropServices;

using static NativeMethods;

/// <summary>
/// 接続タイプ
/// </summary>
public enum NetworkConnectionType
{
    Unknown,
    Ethernet,
    WiFi,
    Bluetooth,
    Other,
}

/// <summary>
/// WiFi詳細情報
/// </summary>
public sealed record WiFiDetails
{
    public string? Ssid { get; init; }
    public string? Bssid { get; init; }
    public string? CountryCode { get; init; }
    public int? Rssi { get; init; }
    public int? Noise { get; init; }
    public string? Standard { get; init; }     // 802.11n, 802.11ac, 802.11ax
    public string? Security { get; init; }
    public string? Channel { get; init; }
    public string? ChannelBand { get; init; }  // 2GHz, 5GHz, 6GHz
    public string? ChannelWidth { get; init; } // 20MHz, 40MHz, 80MHz, 160MHz
}

/// <summary>
/// ネットワークインターフェース詳細情報
/// </summary>
public sealed record NetworkDetailEntry
{
    public required string BsdName { get; init; }
    public string? DisplayName { get; init; }
    public string? MacAddress { get; init; }
    public NetworkConnectionType ConnectionType { get; init; }
    public bool IsPrimary { get; init; }
    public uint BaudRate { get; init; }
    public string? LocalIpV4 { get; init; }
    public string? LocalIpV6 { get; init; }
    public WiFiDetails? WiFi { get; init; }
}

/// <summary>
/// VPN接続情報
/// </summary>
public sealed record VpnConnectionInfo
{
    public bool IsVpnConnected { get; init; }
    public IReadOnlyList<string> VpnInterfaces { get; init; } = [];
}

/// <summary>
/// ネットワーク詳細情報取得
/// </summary>
public static class NetworkDetailInfo
{
    /// <summary>
    /// プライマリインターフェースのBSD名を取得
    /// </summary>
    public static string? GetPrimaryInterface()
    {
        var key = CFStringCreateWithCString(nint.Zero, "State:/Network/Global/IPv4", kCFStringEncodingUTF8);
        var value = SCDynamicStoreCopyValue(nint.Zero, key);
        CFRelease(key);

        if (value == nint.Zero)
        {
            return null;
        }

        try
        {
            var primaryKey = CFStringCreateWithCString(nint.Zero, "PrimaryInterface", kCFStringEncodingUTF8);
            var primaryValue = CFDictionaryGetValue(value, primaryKey);
            CFRelease(primaryKey);

            if (primaryValue != nint.Zero && CFGetTypeID(primaryValue) == CFStringGetTypeID())
            {
                return CfStringToManaged(primaryValue);
            }
        }
        finally
        {
            CFRelease(value);
        }

        return null;
    }

    /// <summary>
    /// VPN接続情報を取得
    /// </summary>
    public static VpnConnectionInfo GetVpnInfo()
    {
        var proxySettings = CFNetworkCopySystemProxySettings();
        if (proxySettings == nint.Zero)
        {
            return new VpnConnectionInfo();
        }

        try
        {
            var scopedKey = CFStringCreateWithCString(nint.Zero, "__SCOPED__", kCFStringEncodingUTF8);
            var scopedValue = CFDictionaryGetValue(proxySettings, scopedKey);
            CFRelease(scopedKey);

            if (scopedValue == nint.Zero || CFGetTypeID(scopedValue) != CFDictionaryGetTypeID())
            {
                return new VpnConnectionInfo();
            }

            // CFDictionaryからキーを列挙するには追加のP/Invokeが必要
            // ここでは簡易的にnetstat等で判定する代替実装
            var vpnInterfaces = GetVpnInterfacesFromNetstat();
            return new VpnConnectionInfo
            {
                IsVpnConnected = vpnInterfaces.Count > 0,
                VpnInterfaces = vpnInterfaces,
            };
        }
        finally
        {
            CFRelease(proxySettings);
        }
    }

    private static List<string> GetVpnInterfacesFromNetstat()
    {
        var vpnInterfaces = new List<string>();

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/sbin/ifconfig",
                Arguments = "-l",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return vpnInterfaces;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var interfaces = output.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var iface in interfaces)
            {
                if (iface.StartsWith("utun", StringComparison.Ordinal) ||
                    iface.StartsWith("tun", StringComparison.Ordinal) ||
                    iface.StartsWith("tap", StringComparison.Ordinal) ||
                    iface.StartsWith("ppp", StringComparison.Ordinal) ||
                    iface.StartsWith("ipsec", StringComparison.Ordinal))
                {
                    vpnInterfaces.Add(iface);
                }
            }
        }
        catch
        {
            // Ignore
        }

        return vpnInterfaces;
    }

    /// <summary>
    /// ネットワークインターフェース詳細一覧を取得
    /// </summary>
    public static unsafe NetworkDetailEntry[] GetNetworkInterfaces()
    {
        var results = new List<NetworkDetailEntry>();
        var primaryInterface = GetPrimaryInterface();

        var allInterfaces = SCNetworkInterfaceCopyAll();
        if (allInterfaces == nint.Zero)
        {
            return [];
        }

        try
        {
            var count = CFArrayGetCount(allInterfaces);
            for (var i = (nint)0; i < count; i++)
            {
                var iface = CFArrayGetValueAtIndex(allInterfaces, i);
                if (iface == nint.Zero)
                {
                    continue;
                }

                var bsdNamePtr = SCNetworkInterfaceGetBSDName(iface);
                var bsdName = bsdNamePtr != nint.Zero ? CfStringToManaged(bsdNamePtr) : null;
                if (string.IsNullOrEmpty(bsdName))
                {
                    continue;
                }

                var displayNamePtr = SCNetworkInterfaceGetLocalizedDisplayName(iface);
                var displayName = displayNamePtr != nint.Zero ? CfStringToManaged(displayNamePtr) : null;

                var macAddressPtr = SCNetworkInterfaceGetHardwareAddressString(iface);
                var macAddress = macAddressPtr != nint.Zero ? CfStringToManaged(macAddressPtr) : null;

                var typePtr = SCNetworkInterfaceGetInterfaceType(iface);
                var type = typePtr != nint.Zero ? CfStringToManaged(typePtr) : null;

                var connectionType = type switch
                {
                    kSCNetworkInterfaceTypeEthernet => NetworkConnectionType.Ethernet,
                    kSCNetworkInterfaceTypeIEEE80211 => NetworkConnectionType.WiFi,
                    kSCNetworkInterfaceTypeBluetooth => NetworkConnectionType.Bluetooth,
                    _ => NetworkConnectionType.Other,
                };

                // getifaddrsからIPアドレスとbaudrate取得
                var (ipv4, ipv6, baudRate) = GetInterfaceAddresses(bsdName);

                // WiFi詳細
                WiFiDetails? wifiDetails = null;
                if (connectionType == NetworkConnectionType.WiFi)
                {
                    wifiDetails = GetWiFiDetails(bsdName);
                }

                results.Add(new NetworkDetailEntry
                {
                    BsdName = bsdName,
                    DisplayName = displayName,
                    MacAddress = macAddress,
                    ConnectionType = connectionType,
                    IsPrimary = bsdName == primaryInterface,
                    BaudRate = baudRate,
                    LocalIpV4 = ipv4,
                    LocalIpV6 = ipv6,
                    WiFi = wifiDetails,
                });
            }
        }
        finally
        {
            CFRelease(allInterfaces);
        }

        return [.. results];
    }

    private static unsafe (string? ipv4, string? ipv6, uint baudRate) GetInterfaceAddresses(string bsdName)
    {
        string? ipv4 = null;
        string? ipv6 = null;
        uint baudRate = 0;

        if (getifaddrs(out var ifap) != 0)
        {
            return (ipv4, ipv6, baudRate);
        }

        try
        {
            var current = ifap;
            while (current != nint.Zero)
            {
                var ifa = Marshal.PtrToStructure<ifaddrs>(current);
                var name = ifa.ifa_name != nint.Zero ? Marshal.PtrToStringUTF8(ifa.ifa_name) : null;

                if (name == bsdName && ifa.ifa_addr != nint.Zero)
                {
                    var sa = Marshal.PtrToStructure<sockaddr>(ifa.ifa_addr);

                    if (sa.sa_family == AF_INET)
                    {
                        var addrBuf = stackalloc byte[(int)INET_ADDRSTRLEN];
                        var sockaddrIn = (byte*)ifa.ifa_addr;
                        var addrPtr = sockaddrIn + 4; // sin_addr offset
                        if (inet_ntop(AF_INET, addrPtr, addrBuf, INET_ADDRSTRLEN) != nint.Zero)
                        {
                            ipv4 = Marshal.PtrToStringUTF8((nint)addrBuf);
                        }
                    }
                    else if (sa.sa_family == AF_INET6)
                    {
                        var addrBuf = stackalloc byte[(int)INET6_ADDRSTRLEN];
                        var sockaddrIn6 = (byte*)ifa.ifa_addr;
                        var addrPtr = sockaddrIn6 + 8; // sin6_addr offset
                        if (inet_ntop(AF_INET6, addrPtr, addrBuf, INET6_ADDRSTRLEN) != nint.Zero)
                        {
                            ipv6 = Marshal.PtrToStringUTF8((nint)addrBuf);
                        }
                    }
                    else if (sa.sa_family == AF_LINK && ifa.ifa_data != nint.Zero)
                    {
                        var ifData = Marshal.PtrToStructure<if_data>(ifa.ifa_data);
                        baudRate = ifData.ifi_baudrate;
                    }
                }

                current = ifa.ifa_next;
            }
        }
        finally
        {
            freeifaddrs(ifap);
        }

        return (ipv4, ipv6, baudRate);
    }

    /// <summary>
    /// WiFi詳細情報を取得 (system_profiler経由)
    /// </summary>
    private static WiFiDetails? GetWiFiDetails(string interfaceName)
    {
        // CoreWLANはObjective-C frameworkなので、system_profilerで代替
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/sbin/system_profiler",
                Arguments = "SPAirPortDataType -json",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // JSONパースは簡易実装
            return ParseWiFiDetailsFromJson(output, interfaceName);
        }
        catch
        {
            return null;
        }
    }

    private static WiFiDetails? ParseWiFiDetailsFromJson(string json, string interfaceName)
    {
        // 簡易的なJSONパース
        // 本来はSystem.Text.Jsonを使用すべき
        if (!json.Contains(interfaceName, StringComparison.Ordinal))
        {
            return null;
        }

        var ssid = ExtractJsonValue(json, "\"_name\"");
        var phyMode = ExtractJsonValue(json, "\"spairport_network_phymode\"");
        var countryCode = ExtractJsonValue(json, "\"spairport_network_country_code\"");

        if (ssid is null)
        {
            // networksetup経由でSSID取得
            ssid = GetSsidFromNetworkSetup(interfaceName);
        }

        return new WiFiDetails
        {
            Ssid = ssid,
            Standard = phyMode,
            CountryCode = countryCode,
        };
    }

    private static string? GetSsidFromNetworkSetup(string interfaceName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/sbin/networksetup",
                Arguments = $"-getairportnetwork {interfaceName}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            // "Current Wi-Fi Network: SSID_NAME"
            var colonIndex = output.IndexOf(':');
            if (colonIndex >= 0)
            {
                return output[(colonIndex + 1)..].Trim();
            }
        }
        catch
        {
            // Ignore
        }

        return null;
    }

    private static string? ExtractJsonValue(string json, string key)
    {
        var keyIndex = json.IndexOf(key, StringComparison.Ordinal);
        if (keyIndex < 0)
        {
            return null;
        }

        var colonIndex = json.IndexOf(':', keyIndex);
        if (colonIndex < 0)
        {
            return null;
        }

        var valueStart = json.IndexOf('"', colonIndex);
        if (valueStart < 0)
        {
            return null;
        }

        var valueEnd = json.IndexOf('"', valueStart + 1);
        if (valueEnd < 0)
        {
            return null;
        }

        return json.Substring(valueStart + 1, valueEnd - valueStart - 1);
    }
}
