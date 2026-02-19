namespace MacDotNet.SystemInfo;

using static MacDotNet.SystemInfo.NativeMethods;

public sealed record PerformanceLevelEntry
{
    public required int Index { get; init; }

    public required string Name { get; init; }

    public required int PhysicalCpu { get; init; }

    public required int LogicalCpu { get; init; }

    public required int CpusPerL2 { get; init; }

    public required int L2CacheSize { get; init; }

    public int CpuFrequencyMax { get; init; }
}

public sealed class HardwareInfo
{
    public string Model { get; }

    public string Machine { get; }

    public string? TargetType { get; }

    public string? CpuBrandString { get; }

    public string? SerialNumber { get; }

    public int LogicalCpu { get; }

    public int LogicalCpuMax { get; }

    public int PhysicalCpu { get; }

    public int PhysicalCpuMax { get; }

    public int ActiveCpu { get; }

    public int Ncpu { get; }

    public int CpuCoreCount { get; }

    public int CpuThreadCount { get; }

    public long CpuFrequency { get; }

    public long CpuFrequencyMax { get; }

    public long BusFrequency { get; }

    public long TbFrequency { get; }

    public long MemSize { get; }

    public long PageSize { get; }

    public int ByteOrder { get; }

    public long CacheLineSize { get; }

    public long L1ICacheSize { get; }

    public long L1DCacheSize { get; }

    public long L2CacheSize { get; }

    public long L3CacheSize { get; }

    public int Packages { get; }

    public bool Cpu64BitCapable { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private HardwareInfo()
    {
        Model = GetSystemControlString("hw.model") ?? string.Empty;
        Machine = GetSystemControlString("hw.machine") ?? string.Empty;
        TargetType = GetSystemControlString("hw.targettype");
        CpuBrandString = GetSystemControlString("machdep.cpu.brand_string");
        SerialNumber = GetSerialNumber();
        LogicalCpu = GetSystemControlInt32("hw.logicalcpu");
        LogicalCpuMax = GetSystemControlInt32("hw.logicalcpu_max");
        PhysicalCpu = GetSystemControlInt32("hw.physicalcpu");
        PhysicalCpuMax = GetSystemControlInt32("hw.physicalcpu_max");
        ActiveCpu = GetSystemControlInt32("hw.activecpu");
        Ncpu = GetSystemControlInt32("hw.ncpu");
        CpuCoreCount = GetSystemControlInt32("machdep.cpu.core_count");
        CpuThreadCount = GetSystemControlInt32("machdep.cpu.thread_count");
        CpuFrequency = GetSystemControlInt64("hw.cpufrequency");
        CpuFrequencyMax = GetSystemControlInt64("hw.cpufrequency_max");
        BusFrequency = GetSystemControlInt64("hw.busfrequency");
        TbFrequency = GetSystemControlInt64("hw.tbfrequency");
        MemSize = GetSystemControlInt64("hw.memsize");
        PageSize = GetSystemControlInt64("hw.pagesize");
        ByteOrder = GetSystemControlInt32("hw.byteorder");
        CacheLineSize = GetSystemControlInt64("hw.cachelinesize");
        L1ICacheSize = GetSystemControlInt64("hw.l1icachesize");
        L1DCacheSize = GetSystemControlInt64("hw.l1dcachesize");
        L2CacheSize = GetSystemControlInt64("hw.l2cachesize");
        L3CacheSize = GetSystemControlInt64("hw.l3cachesize");
        Packages = GetSystemControlInt32("hw.packages");
        Cpu64BitCapable = GetSystemControlInt32("hw.cpu64bit_capable") != 0;
    }

    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    public static HardwareInfo Create() => new();

    public static PerformanceLevelEntry[] GetPerformanceLevels()
    {
        var count = GetSystemControlInt32("hw.nperflevels");
        if (count <= 0)
        {
            return [];
        }

        var levels = new PerformanceLevelEntry[count];
        for (var i = 0; i < count; i++)
        {
            levels[i] = new PerformanceLevelEntry
            {
                Index = i,
                Name = GetSystemControlString($"hw.perflevel{i}.name") ?? $"Level {i}",
                PhysicalCpu = GetSystemControlInt32($"hw.perflevel{i}.physicalcpu"),
                LogicalCpu = GetSystemControlInt32($"hw.perflevel{i}.logicalcpu"),
                CpusPerL2 = GetSystemControlInt32($"hw.perflevel{i}.cpusperl2"),
                L2CacheSize = GetSystemControlInt32($"hw.perflevel{i}.l2cachesize"),
                CpuFrequencyMax = GetSystemControlInt32($"hw.perflevel{i}.cpufreq_max"),
            };
        }

        return levels;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static string? GetSerialNumber()
    {
        var matching = IOServiceMatching("IOPlatformExpertDevice");
        if (matching == IntPtr.Zero)
        {
            return null;
        }

        var service = IOServiceGetMatchingService(0, matching);
        if (service == 0)
        {
            return null;
        }

        try
        {
            var key = CFStringCreateWithCString(IntPtr.Zero, "IOPlatformSerialNumber", kCFStringEncodingUTF8);
            var value = IORegistryEntryCreateCFProperty(service, key, IntPtr.Zero, 0);
            CFRelease(key);

            if (value == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return CfStringToManaged(value);
            }
            finally
            {
                CFRelease(value);
            }
        }
        finally
        {
            IOObjectRelease(service);
        }
    }
}
