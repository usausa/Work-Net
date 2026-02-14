namespace MacDotNet.SystemInfo;

using System.Runtime.InteropServices;

using static MacDotNet.SystemInfo.NativeMethods;

public enum InterfaceState
{
    Down,
    NoCarrier,
    Up,
}

public sealed record InterfaceAddress
{
    public required string Address { get; init; }

    public required int PrefixLength { get; init; }
}

public sealed record NetworkInterfaceEntry
{
    public required string Name { get; init; }

    public required InterfaceState State { get; init; }

    public required uint Flags { get; init; }

    public bool IsLoopback => (Flags & IFF_LOOPBACK) != 0;

    public bool SupportsBroadcast => (Flags & IFF_BROADCAST) != 0;

    public bool SupportsMulticast => (Flags & IFF_MULTICAST) != 0;

    public bool IsPointToPoint => (Flags & IFF_POINTOPOINT) != 0;

    public string? MacAddress { get; init; }

    public required IReadOnlyList<InterfaceAddress> IPv4Addresses { get; init; }

    public required IReadOnlyList<InterfaceAddress> IPv6Addresses { get; init; }

    public byte InterfaceType { get; init; }

    public string InterfaceTypeName => InterfaceType switch
    {
        IFT_ETHER => "Ethernet",
        IFT_LOOP => "Loopback",
        IFT_IEEE80211 => "Wi-Fi",
        IFT_GIF => "GIF Tunnel",
        IFT_STF => "6to4 Tunnel",
        IFT_CELLULAR => "Cellular",
        IFT_BRIDGE => "Bridge",
        _ => $"Other(0x{InterfaceType:X2})",
    };

    public uint Mtu { get; init; }

    public uint LinkSpeed { get; init; }

    public uint RxBytes { get; init; }

    public uint RxPackets { get; init; }

    public uint RxErrors { get; init; }

    public uint RxDrops { get; init; }

    public uint RxMulticast { get; init; }

    public uint TxBytes { get; init; }

    public uint TxPackets { get; init; }

    public uint TxErrors { get; init; }

    public uint TxMulticast { get; init; }

    public uint Collisions { get; init; }

    public uint NoProto { get; init; }
}

public static class NetworkInfo
{
    public static unsafe NetworkInterfaceEntry[] GetNetworkInterfaces()
    {
        if (getifaddrs(out var ifap) != 0)
        {
            return [];
        }

        try
        {
            var map = new Dictionary<string, InterfaceBuilder>(StringComparer.Ordinal);

            for (var ptr = ifap; ptr != nint.Zero;)
            {
                var ifa = Marshal.PtrToStructure<ifaddrs>(ptr);
                var name = Marshal.PtrToStringUTF8(ifa.ifa_name);

                if (name is not null)
                {
                    if (!map.TryGetValue(name, out var builder))
                    {
                        builder = new InterfaceBuilder { Name = name, Flags = ifa.ifa_flags };
                        map[name] = builder;
                    }

                    if (ifa.ifa_addr != nint.Zero)
                    {
                        var family = ((sockaddr*)ifa.ifa_addr)->sa_family;
                        switch (family)
                        {
                            case AF_LINK:
                                ProcessLinkAddress(ifa, builder);
                                break;
                            case AF_INET:
                                ProcessIPv4Address(ifa, builder);
                                break;
                            case AF_INET6:
                                ProcessIPv6Address(ifa, builder);
                                break;
                        }
                    }
                }

                ptr = ifa.ifa_next;
            }

            return [.. map.Values.Select(b => b.Build()).OrderBy(static i => i.Name, StringComparer.Ordinal)];
        }
        finally
        {
            freeifaddrs(ifap);
        }
    }

    private static unsafe void ProcessLinkAddress(ifaddrs ifa, InterfaceBuilder builder)
    {
        if (ifa.ifa_data != nint.Zero)
        {
            var data = (if_data*)ifa.ifa_data;
            builder.InterfaceType = data->ifi_type;
            builder.Mtu = data->ifi_mtu;
            builder.LinkSpeed = data->ifi_baudrate;
            builder.RxBytes = data->ifi_ibytes;
            builder.RxPackets = data->ifi_ipackets;
            builder.RxErrors = data->ifi_ierrors;
            builder.RxDrops = data->ifi_iqdrops;
            builder.RxMulticast = data->ifi_imcasts;
            builder.TxBytes = data->ifi_obytes;
            builder.TxPackets = data->ifi_opackets;
            builder.TxErrors = data->ifi_oerrors;
            builder.TxMulticast = data->ifi_omcasts;
            builder.Collisions = data->ifi_collisions;
            builder.NoProto = data->ifi_noproto;
        }

        var sdl = (sockaddr_dl*)ifa.ifa_addr;
        if (sdl->sdl_alen > 0 && sdl->sdl_alen <= 8)
        {
            var macPtr = (byte*)sdl + sockaddr_dl.DataOffset + sdl->sdl_nlen;
            Span<char> buf = stackalloc char[(sdl->sdl_alen * 3) - 1];
            var pos = 0;
            for (var i = 0; i < sdl->sdl_alen; i++)
            {
                if (i > 0)
                {
                    buf[pos++] = ':';
                }

                var b = macPtr[i];
                var hi = b >> 4;
                var lo = b & 0x0F;
                buf[pos++] = ToHexChar(hi);
                buf[pos++] = ToHexChar(lo);
            }

            builder.MacAddress = new string(buf[..pos]);
        }
    }

    private static char ToHexChar(int value) => (char)(value < 10 ? '0' + value : 'a' + value - 10);

    private static unsafe void ProcessIPv4Address(ifaddrs ifa, InterfaceBuilder builder)
    {
        var sinAddr = (byte*)ifa.ifa_addr + sockaddr_in.AddrOffset;
        var buf = stackalloc byte[(int)INET_ADDRSTRLEN];

        if (inet_ntop(AF_INET, sinAddr, buf, INET_ADDRSTRLEN) == nint.Zero)
        {
            return;
        }

        var address = Marshal.PtrToStringUTF8((nint)buf);
        if (address is null)
        {
            return;
        }

        var prefix = 0;
        if (ifa.ifa_netmask != nint.Zero)
        {
            var maskAddr = (byte*)ifa.ifa_netmask + sockaddr_in.AddrOffset;
            prefix = CountPrefixBits(maskAddr, 4);
        }

        builder.IPv4Addresses.Add(new InterfaceAddress { Address = address, PrefixLength = prefix });
    }

    private static unsafe void ProcessIPv6Address(ifaddrs ifa, InterfaceBuilder builder)
    {
        var sin6Addr = (byte*)ifa.ifa_addr + sockaddr_in6.AddrOffset;
        var buf = stackalloc byte[(int)INET6_ADDRSTRLEN];

        if (inet_ntop(AF_INET6, sin6Addr, buf, INET6_ADDRSTRLEN) == nint.Zero)
        {
            return;
        }

        var address = Marshal.PtrToStringUTF8((nint)buf);
        if (address is null)
        {
            return;
        }

        var scopeId = *(uint*)((byte*)ifa.ifa_addr + sockaddr_in6.ScopeIdOffset);
        if (scopeId != 0)
        {
            address = $"{address}%{scopeId}";
        }

        var prefix = 0;
        if (ifa.ifa_netmask != nint.Zero)
        {
            var maskAddr = (byte*)ifa.ifa_netmask + sockaddr_in6.AddrOffset;
            prefix = CountPrefixBits(maskAddr, 16);
        }

        builder.IPv6Addresses.Add(new InterfaceAddress { Address = address, PrefixLength = prefix });
    }

    private static unsafe int CountPrefixBits(byte* mask, int length)
    {
        var bits = 0;
        for (var i = 0; i < length; i++)
        {
            if (mask[i] == 0xFF)
            {
                bits += 8;
                continue;
            }

            var b = mask[i];
            while ((b & 0x80) != 0)
            {
                bits++;
                b <<= 1;
            }

            break;
        }

        return bits;
    }

    private sealed class InterfaceBuilder
    {
        public required string Name { get; init; }

        public uint Flags { get; init; }

        public byte InterfaceType { get; set; }

        public string? MacAddress { get; set; }

        public uint Mtu { get; set; }

        public uint LinkSpeed { get; set; }

        public uint RxBytes { get; set; }

        public uint RxPackets { get; set; }

        public uint RxErrors { get; set; }

        public uint RxDrops { get; set; }

        public uint RxMulticast { get; set; }

        public uint TxBytes { get; set; }

        public uint TxPackets { get; set; }

        public uint TxErrors { get; set; }

        public uint TxMulticast { get; set; }

        public uint Collisions { get; set; }

        public uint NoProto { get; set; }

        public List<InterfaceAddress> IPv4Addresses { get; } = [];

        public List<InterfaceAddress> IPv6Addresses { get; } = [];

        public NetworkInterfaceEntry Build() => new()
        {
            Name = Name,
            State = (Flags & IFF_UP) == 0
                ? InterfaceState.Down
                : (Flags & IFF_RUNNING) != 0
                    ? InterfaceState.Up
                    : InterfaceState.NoCarrier,
            Flags = Flags,
            MacAddress = MacAddress,
            InterfaceType = InterfaceType,
            Mtu = Mtu,
            LinkSpeed = LinkSpeed,
            IPv4Addresses = IPv4Addresses,
            IPv6Addresses = IPv6Addresses,
            RxBytes = RxBytes,
            RxPackets = RxPackets,
            RxErrors = RxErrors,
            RxDrops = RxDrops,
            RxMulticast = RxMulticast,
            TxBytes = TxBytes,
            TxPackets = TxPackets,
            TxErrors = TxErrors,
            TxMulticast = TxMulticast,
            Collisions = Collisions,
            NoProto = NoProto,
        };
    }
}
