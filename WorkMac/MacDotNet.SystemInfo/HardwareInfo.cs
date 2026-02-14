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
    public DateTime UpdateAt { get; private set; }

    public string Model { get; private set; } = string.Empty;

    public string Machine { get; private set; } = string.Empty;

    public string? TargetType { get; private set; }

    public string? CpuBrandString { get; private set; }

    public int LogicalCpu { get; private set; }

    public int LogicalCpuMax { get; private set; }

    public int PhysicalCpu { get; private set; }

    public int PhysicalCpuMax { get; private set; }

    public int ActiveCpu { get; private set; }

    public int Ncpu { get; private set; }

    public int CpuCoreCount { get; private set; }

    public int CpuThreadCount { get; private set; }

    public long CpuFrequency { get; private set; }

    public long CpuFrequencyMax { get; private set; }

    public long BusFrequency { get; private set; }

    public long TbFrequency { get; private set; }

    public long MemSize { get; private set; }

    public long PageSize { get; private set; }

    public int ByteOrder { get; private set; }

    public long CacheLineSize { get; private set; }

    public long L1ICacheSize { get; private set; }

    public long L1DCacheSize { get; private set; }

    public long L2CacheSize { get; private set; }

    public long L3CacheSize { get; private set; }

    public int Packages { get; private set; }

    public bool Cpu64BitCapable { get; private set; }

    internal HardwareInfo()
    {
        Update();
    }

    public bool Update()
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

        UpdateAt = DateTime.Now;

        return true;
    }

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
