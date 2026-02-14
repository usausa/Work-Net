namespace LinuxDotNet.SystemInfo;

public sealed class HardwareInfo
{
    public DateTime UpdateAt { get; private set; }

    public string Model { get; private set; } = string.Empty;

    public string Vendor { get; private set; } = string.Empty;

    public string ProductName { get; private set; } = string.Empty;

    public string? ProductVersion { get; private set; }

    public string? SerialNumber { get; private set; }

    public string Machine { get; private set; } = string.Empty;

    public string? CpuBrandString { get; private set; }

    public string? CpuVendor { get; private set; }

    public int CpuFamily { get; private set; }

    public int CpuModel { get; private set; }

    public int CpuStepping { get; private set; }

    public int LogicalCpu { get; private set; }

    public int PhysicalCpu { get; private set; }

    public int CoresPerSocket { get; private set; }

    public long CpuFrequency { get; private set; }

    public long CpuFrequencyMax { get; private set; }

    public long MemSize { get; private set; }

    public long PageSize { get; private set; }

    public long CacheLineSize { get; private set; }

    public long L1DCacheSize { get; private set; }

    public long L1ICacheSize { get; private set; }

    public long L2CacheSize { get; private set; }

    public long L3CacheSize { get; private set; }

    internal HardwareInfo()
    {
        Update();
    }

    public bool Update()
    {
        Model = ReadDmiFile("product_name");
        Vendor = ReadDmiFile("sys_vendor");
        ProductName = ReadDmiFile("product_name");
        ProductVersion = ReadDmiFileOrNull("product_version");
        SerialNumber = ReadDmiFileOrNull("product_serial");
        Machine = ReadFile("/proc/sys/kernel/arch") ?? Environment.GetEnvironmentVariable("HOSTTYPE") ?? "unknown";

        ParseCpuInfo();

        MemSize = ReadMemInfo("MemTotal") * 1024;
        PageSize = Environment.SystemPageSize;

        UpdateAt = DateTime.Now;

        return true;
    }

    private void ParseCpuInfo()
    {
        var physicalIds = new HashSet<int>();
        var processors = 0;
        var coresPerSocket = 0;

        try
        {
            using var reader = new StreamReader("/proc/cpuinfo");
            while (reader.ReadLine() is { } line)
            {
                var span = line.AsSpan();
                var colonIndex = span.IndexOf(':');
                if (colonIndex < 0)
                {
                    continue;
                }

                var key = span[..colonIndex].Trim().ToString();
                var value = span[(colonIndex + 1)..].Trim().ToString();

                switch (key)
                {
                    case "model name":
                        CpuBrandString ??= value;
                        break;
                    case "vendor_id":
                        CpuVendor ??= value;
                        break;
                    case "cpu family":
                        if (CpuFamily == 0 && Int32.TryParse(value, out var family))
                        {
                            CpuFamily = family;
                        }

                        break;
                    case "model":
                        if (CpuModel == 0 && Int32.TryParse(value, out var model))
                        {
                            CpuModel = model;
                        }

                        break;
                    case "stepping":
                        if (CpuStepping == 0 && Int32.TryParse(value, out var stepping))
                        {
                            CpuStepping = stepping;
                        }

                        break;
                    case "processor":
                        processors++;
                        break;
                    case "physical id":
                        if (Int32.TryParse(value, out var physId))
                        {
                            physicalIds.Add(physId);
                        }

                        break;
                    case "cpu cores":
                        if (coresPerSocket == 0 && Int32.TryParse(value, out var cores))
                        {
                            coresPerSocket = cores;
                        }

                        break;
                    case "cpu MHz":
                        if (CpuFrequency == 0 && Double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var mhz))
                        {
                            CpuFrequency = (long)(mhz * 1_000_000);
                        }

                        break;
                    case "cache size":
                        if (L3CacheSize == 0)
                        {
                            var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 0 && Int64.TryParse(parts[0], out var cacheKb))
                            {
                                L3CacheSize = cacheKb * 1024;
                            }
                        }

                        break;
                    case "clflush size":
                        if (CacheLineSize == 0 && Int64.TryParse(value, out var clflush))
                        {
                            CacheLineSize = clflush;
                        }

                        break;
                }
            }
        }
        catch
        {
            // Ignore
        }

        LogicalCpu = processors;
        PhysicalCpu = physicalIds.Count > 0 ? physicalIds.Count : (processors > 0 ? 1 : 0);
        CoresPerSocket = coresPerSocket;

        CpuFrequencyMax = ReadCpuFreqMax();

        ParseCacheInfo();
    }

    private void ParseCacheInfo()
    {
        var cacheBasePath = "/sys/devices/system/cpu/cpu0/cache";
        if (!Directory.Exists(cacheBasePath))
        {
            return;
        }

        try
        {
            foreach (var indexDir in Directory.GetDirectories(cacheBasePath, "index*"))
            {
                var levelStr = ReadFile(Path.Combine(indexDir, "level"));
                var typeStr = ReadFile(Path.Combine(indexDir, "type"));
                var sizeStr = ReadFile(Path.Combine(indexDir, "size"));

                if (!Int32.TryParse(levelStr, out var level))
                {
                    continue;
                }

                var sizeKb = ParseCacheSize(sizeStr);

                switch (level)
                {
                    case 1 when typeStr?.Contains("Data", StringComparison.OrdinalIgnoreCase) == true:
                        L1DCacheSize = sizeKb * 1024;
                        break;
                    case 1 when typeStr?.Contains("Instruction", StringComparison.OrdinalIgnoreCase) == true:
                        L1ICacheSize = sizeKb * 1024;
                        break;
                    case 2:
                        L2CacheSize = sizeKb * 1024;
                        break;
                    case 3:
                        L3CacheSize = sizeKb * 1024;
                        break;
                }
            }
        }
        catch
        {
            // Ignore
        }
    }

    private static long ParseCacheSize(string? sizeStr)
    {
        if (string.IsNullOrEmpty(sizeStr))
        {
            return 0;
        }

        sizeStr = sizeStr.Trim().ToUpperInvariant();
        if (sizeStr.EndsWith('K'))
        {
            return Int64.TryParse(sizeStr[..^1], out var kb) ? kb : 0;
        }

        if (sizeStr.EndsWith('M'))
        {
            return Int64.TryParse(sizeStr[..^1], out var mb) ? mb * 1024 : 0;
        }

        return Int64.TryParse(sizeStr, out var bytes) ? bytes / 1024 : 0;
    }

    private static long ReadCpuFreqMax()
    {
        var path = "/sys/devices/system/cpu/cpu0/cpufreq/cpuinfo_max_freq";
        if (File.Exists(path))
        {
            try
            {
                var value = File.ReadAllText(path).Trim();
                if (Int64.TryParse(value, out var khz))
                {
                    return khz * 1000;
                }
            }
            catch
            {
                // Ignore
            }
        }

        return 0;
    }

    private static long ReadMemInfo(string key)
    {
        try
        {
            using var reader = new StreamReader("/proc/meminfo");
            while (reader.ReadLine() is { } line)
            {
                if (line.StartsWith(key + ":", StringComparison.Ordinal))
                {
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && Int64.TryParse(parts[1], out var value))
                    {
                        return value;
                    }
                }
            }
        }
        catch
        {
            // Ignore
        }

        return 0;
    }

    private static string ReadDmiFile(string name)
    {
        var path = $"/sys/class/dmi/id/{name}";
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

    private static string? ReadDmiFileOrNull(string name)
    {
        var value = ReadDmiFile(name);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static string? ReadFile(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                return File.ReadAllText(path).Trim();
            }
            catch
            {
                return null;
            }
        }

        return null;
    }
}
