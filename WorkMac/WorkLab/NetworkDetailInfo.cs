namespace MacDotNet.SystemInfo.Lab;

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
        var key = CFStringCreateWithCString(IntPtr.Zero, "State:/Network/Global/IPv4", kCFStringEncodingUTF8);
        var value = SCDynamicStoreCopyValue(IntPtr.Zero, key);
        CFRelease(key);

        if (value == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var primaryKey = CFStringCreateWithCString(IntPtr.Zero, "PrimaryInterface", kCFStringEncodingUTF8);
            var primaryValue = CFDictionaryGetValue(value, primaryKey);
            CFRelease(primaryKey);

            if (primaryValue != IntPtr.Zero && CFGetTypeID(primaryValue) == CFStringGetTypeID())
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
    /// ネットワークインターフェース詳細一覧を取得
    /// </summary>
    public static unsafe NetworkDetailEntry[] GetNetworkInterfaces()
    {
        var results = new List<NetworkDetailEntry>();
        var primaryInterface = GetPrimaryInterface();

        var allInterfaces = SCNetworkInterfaceCopyAll();
        if (allInterfaces == IntPtr.Zero)
        {
            return [];
        }

        try
        {
            var count = CFArrayGetCount(allInterfaces);
            for (var i = (IntPtr)0; i < count; i++)
            {
                var iface = CFArrayGetValueAtIndex(allInterfaces, i);
                if (iface == IntPtr.Zero)
                {
                    continue;
                }

                var bsdNamePtr = SCNetworkInterfaceGetBSDName(iface);
                var bsdName = bsdNamePtr != IntPtr.Zero ? CfStringToManaged(bsdNamePtr) : null;
                if (string.IsNullOrEmpty(bsdName))
                {
                    continue;
                }

                var displayNamePtr = SCNetworkInterfaceGetLocalizedDisplayName(iface);
                var displayName = displayNamePtr != IntPtr.Zero ? CfStringToManaged(displayNamePtr) : null;

                var macAddressPtr = SCNetworkInterfaceGetHardwareAddressString(iface);
                var macAddress = macAddressPtr != IntPtr.Zero ? CfStringToManaged(macAddressPtr) : null;

                var typePtr = SCNetworkInterfaceGetInterfaceType(iface);
                var type = typePtr != IntPtr.Zero ? CfStringToManaged(typePtr) : null;

                var connectionType = type switch
                {
                    kSCNetworkInterfaceTypeEthernet => NetworkConnectionType.Ethernet,
                    kSCNetworkInterfaceTypeIEEE80211 => NetworkConnectionType.WiFi,
                    kSCNetworkInterfaceTypeBluetooth => NetworkConnectionType.Bluetooth,
                    _ => NetworkConnectionType.Other,
                };

                // getifaddrsからIPアドレスとbaudrate取得
                var (ipv4, ipv6, baudRate) = GetInterfaceAddresses(bsdName);

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
            while (current != IntPtr.Zero)
            {
                var ifa = Marshal.PtrToStructure<ifaddrs>(current);
                var name = ifa.ifa_name != IntPtr.Zero ? Marshal.PtrToStringUTF8(ifa.ifa_name) : null;

                if (name == bsdName && ifa.ifa_addr != IntPtr.Zero)
                {
                    var sa = Marshal.PtrToStructure<sockaddr>(ifa.ifa_addr);

                    if (sa.sa_family == AF_INET)
                    {
                        var addrBuf = stackalloc byte[(int)INET_ADDRSTRLEN];
                        var sockaddrIn = (byte*)ifa.ifa_addr;
                        var addrPtr = sockaddrIn + 4; // sin_addr offset
                        if (inet_ntop(AF_INET, addrPtr, addrBuf, INET_ADDRSTRLEN) != IntPtr.Zero)
                        {
                            ipv4 = Marshal.PtrToStringUTF8((IntPtr)addrBuf);
                        }
                    }
                    else if (sa.sa_family == AF_INET6)
                    {
                        var addrBuf = stackalloc byte[(int)INET6_ADDRSTRLEN];
                        var sockaddrIn6 = (byte*)ifa.ifa_addr;
                        var addrPtr = sockaddrIn6 + 8; // sin6_addr offset
                        if (inet_ntop(AF_INET6, addrPtr, addrBuf, INET6_ADDRSTRLEN) != IntPtr.Zero)
                        {
                            ipv6 = Marshal.PtrToStringUTF8((IntPtr)addrBuf);
                        }
                    }
                    else if (sa.sa_family == AF_LINK && ifa.ifa_data != IntPtr.Zero)
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
}
