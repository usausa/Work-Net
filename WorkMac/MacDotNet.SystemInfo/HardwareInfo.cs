namespace MacDotNet.SystemInfo;

public sealed record PerformanceLevelEntry
{
    public required int Index { get; init; }

    public required string Name { get; init; }

    public required int PhysicalCpu { get; init; }

    public required int LogicalCpu { get; init; }

    public required int CpusPerL2 { get; init; }

    public required int L2CacheSize { get; init; }
}

public sealed class HardwareInfo
{
    public string Model { get; }

    public string Machine { get; }

    public string? TargetType { get; }

    public string? CpuBrandString { get; }

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

    private HardwareInfo()
    {
        Model = Helper.GetSysctlString("hw.model") ?? string.Empty;
        Machine = Helper.GetSysctlString("hw.machine") ?? string.Empty;
        TargetType = Helper.GetSysctlString("hw.targettype");
        CpuBrandString = Helper.GetSysctlString("machdep.cpu.brand_string");
        LogicalCpu = Helper.GetSysctlInt("hw.logicalcpu");
        LogicalCpuMax = Helper.GetSysctlInt("hw.logicalcpu_max");
        PhysicalCpu = Helper.GetSysctlInt("hw.physicalcpu");
        PhysicalCpuMax = Helper.GetSysctlInt("hw.physicalcpu_max");
        ActiveCpu = Helper.GetSysctlInt("hw.activecpu");
        Ncpu = Helper.GetSysctlInt("hw.ncpu");
        CpuCoreCount = Helper.GetSysctlInt("machdep.cpu.core_count");
        CpuThreadCount = Helper.GetSysctlInt("machdep.cpu.thread_count");
        CpuFrequency = Helper.GetSysctlLong("hw.cpufrequency");
        CpuFrequencyMax = Helper.GetSysctlLong("hw.cpufrequency_max");
        BusFrequency = Helper.GetSysctlLong("hw.busfrequency");
        TbFrequency = Helper.GetSysctlLong("hw.tbfrequency");
        MemSize = Helper.GetSysctlLong("hw.memsize");
        PageSize = Helper.GetSysctlLong("hw.pagesize");
        ByteOrder = Helper.GetSysctlInt("hw.byteorder");
        CacheLineSize = Helper.GetSysctlLong("hw.cachelinesize");
        L1ICacheSize = Helper.GetSysctlLong("hw.l1icachesize");
        L1DCacheSize = Helper.GetSysctlLong("hw.l1dcachesize");
        L2CacheSize = Helper.GetSysctlLong("hw.l2cachesize");
        L3CacheSize = Helper.GetSysctlLong("hw.l3cachesize");
        Packages = Helper.GetSysctlInt("hw.packages");
        Cpu64BitCapable = Helper.GetSysctlInt("hw.cpu64bit_capable") != 0;
    }

    public static HardwareInfo Create() => new();

    public static PerformanceLevelEntry[] GetPerformanceLevels()
    {
        var count = Helper.GetSysctlInt("hw.nperflevels");
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
                Name = Helper.GetSysctlString($"hw.perflevel{i}.name") ?? $"Level {i}",
                PhysicalCpu = Helper.GetSysctlInt($"hw.perflevel{i}.physicalcpu"),
                LogicalCpu = Helper.GetSysctlInt($"hw.perflevel{i}.logicalcpu"),
                CpusPerL2 = Helper.GetSysctlInt($"hw.perflevel{i}.cpusperl2"),
                L2CacheSize = Helper.GetSysctlInt($"hw.perflevel{i}.l2cachesize"),
            };
        }

        return levels;
    }
}
