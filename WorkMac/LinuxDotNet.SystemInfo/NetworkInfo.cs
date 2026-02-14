namespace LinuxDotNet.SystemInfo;

public enum InterfaceState
{
    Unknown,
    Down,
    Up,
    Dormant,
    LowerLayerDown,
    NotPresent,
    Testing,
}

public sealed record InterfaceAddress
{
    public required string Address { get; init; }

    public required int PrefixLength { get; init; }
}

public sealed class NetworkInterface
{
    private readonly string basePath;

    public string Name { get; }

    public InterfaceState State
    {
        get
        {
            var state = ReadFile("operstate");
            return state switch
            {
                "up" => InterfaceState.Up,
                "down" => InterfaceState.Down,
                "dormant" => InterfaceState.Dormant,
                "lowerlayerdown" => InterfaceState.LowerLayerDown,
                "notpresent" => InterfaceState.NotPresent,
                "testing" => InterfaceState.Testing,
                _ => InterfaceState.Unknown,
            };
        }
    }

    public bool IsUp => State == InterfaceState.Up;

    public string MacAddress => ReadFile("address");

    public int Mtu => ReadFileAsInt32("mtu");

    public int Speed => ReadFileAsInt32("speed");

    public uint Flags => ReadFileAsUInt32Hex("flags");

    public bool IsLoopback => (Flags & 0x8) != 0;

    public bool SupportsBroadcast => (Flags & 0x2) != 0;

    public bool SupportsMulticast => (Flags & 0x1000) != 0;

    public bool IsPointToPoint => (Flags & 0x10) != 0;

    public int InterfaceType => ReadFileAsInt32("type");

    public string InterfaceTypeName => InterfaceType switch
    {
        1 => "Ethernet",
        772 => "Loopback",
        801 => "Wi-Fi",
        776 => "IPv6 in IPv4",
        _ => $"Other({InterfaceType})",
    };

    public int TxQueueLength => ReadFileAsInt32("tx_queue_len");

    public string Carrier => ReadFile("carrier");

    public string Duplex => ReadFile("duplex");

    public long RxBytes => ReadStatAsInt64("rx_bytes");

    public long RxPackets => ReadStatAsInt64("rx_packets");

    public long RxErrors => ReadStatAsInt64("rx_errors");

    public long RxDropped => ReadStatAsInt64("rx_dropped");

    public long TxBytes => ReadStatAsInt64("tx_bytes");

    public long TxPackets => ReadStatAsInt64("tx_packets");

    public long TxErrors => ReadStatAsInt64("tx_errors");

    public long TxDropped => ReadStatAsInt64("tx_dropped");

    public long Collisions => ReadStatAsInt64("collisions");

    internal NetworkInterface(string name)
    {
        Name = name;
        basePath = $"/sys/class/net/{name}";
    }

    public IReadOnlyList<InterfaceAddress> GetIPv4Addresses()
    {
        var addresses = new List<InterfaceAddress>();

        try
        {
            using var reader = new StreamReader("/proc/net/fib_trie");
            string? currentInterface = null;
            var nextLineIsAddress = false;

            while (reader.ReadLine() is { } line)
            {
                var trimmed = line.AsSpan().Trim();

                if (trimmed.Contains("via", StringComparison.Ordinal))
                {
                    continue;
                }

                if (line.Contains($"  {Name} ") || line.EndsWith($"  {Name}", StringComparison.Ordinal))
                {
                    currentInterface = Name;
                }

                if (currentInterface == Name && trimmed.StartsWith("/32 host LOCAL", StringComparison.Ordinal))
                {
                    nextLineIsAddress = true;
                    continue;
                }

                if (nextLineIsAddress && currentInterface == Name)
                {
                    var parts = trimmed.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 1)
                    {
                        addresses.Add(new InterfaceAddress { Address = parts[0], PrefixLength = 32 });
                    }
                    nextLineIsAddress = false;
                }
            }
        }
        catch
        {
            // Fallback: read from /proc/net/route is complex, return empty
        }

        if (addresses.Count == 0)
        {
            // Alternative: parse /proc/net/fib_trie or use simple heuristic
            var addrFile = Path.Combine(basePath, "address");
            // For IPv4, we need to use /proc or ip command typically
        }

        return addresses;
    }

    public IReadOnlyList<InterfaceAddress> GetIPv6Addresses()
    {
        var addresses = new List<InterfaceAddress>();

        try
        {
            using var reader = new StreamReader("/proc/net/if_inet6");
            while (reader.ReadLine() is { } line)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 6 && parts[5] == Name)
                {
                    var hexAddr = parts[0];
                    var prefixLen = Convert.ToInt32(parts[2], 16);

                    var formatted = FormatIPv6(hexAddr);
                    addresses.Add(new InterfaceAddress { Address = formatted, PrefixLength = prefixLen });
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return addresses;
    }

    private static string FormatIPv6(string hex)
    {
        if (hex.Length != 32)
        {
            return hex;
        }

        var parts = new string[8];
        for (var i = 0; i < 8; i++)
        {
            parts[i] = hex.Substring(i * 4, 4).TrimStart('0');
            if (string.IsNullOrEmpty(parts[i]))
            {
                parts[i] = "0";
            }
        }

        return string.Join(":", parts).ToLowerInvariant();
    }

    private string ReadFile(string name)
    {
        var path = Path.Combine(basePath, name);
        if (File.Exists(path))
        {
            try
            {
                return File.ReadAllText(path).Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        return string.Empty;
    }

    private int ReadFileAsInt32(string name)
    {
        var value = ReadFile(name);
        return Int32.TryParse(value, out var result) ? result : 0;
    }

    private uint ReadFileAsUInt32Hex(string name)
    {
        var value = ReadFile(name);
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            value = value[2..];
        }

        return UInt32.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out var result) ? result : 0;
    }

    private long ReadStatAsInt64(string name)
    {
        var path = Path.Combine(basePath, "statistics", name);
        if (File.Exists(path))
        {
            try
            {
                var value = File.ReadAllText(path).Trim();
                return Int64.TryParse(value, out var result) ? result : 0;
            }
            catch
            {
                return 0;
            }
        }

        return 0;
    }
}

public static class NetworkInfo
{
    public static IReadOnlyList<NetworkInterface> GetInterfaces()
    {
        var interfaces = new List<NetworkInterface>();

        const string netPath = "/sys/class/net";
        if (Directory.Exists(netPath))
        {
            foreach (var dir in Directory.GetDirectories(netPath))
            {
                var name = Path.GetFileName(dir);
                interfaces.Add(new NetworkInterface(name));
            }
        }

        return interfaces.OrderBy(static x => x.Name, StringComparer.Ordinal).ToArray();
    }

    public static NetworkInterface? GetInterface(string name)
    {
        var path = $"/sys/class/net/{name}";
        return Directory.Exists(path) ? new NetworkInterface(name) : null;
    }
}
