namespace MacDotNet.SystemInfo;

using System.Runtime.InteropServices;

using static MacDotNet.SystemInfo.NativeMethods;

public sealed class CpuInfo
{
    public DateTime UpdateAt { get; private set; }

    public int LogicalCpu { get; private set; }

    public int PhysicalCpu { get; private set; }

    public int Ncpu { get; private set; }

    public int ActiveCpu { get; private set; }

    public string? BrandString { get; private set; }

    public long CpuFrequency { get; private set; }

    public int CacheLineSize { get; private set; }

    public long L2CacheSize { get; private set; }

    internal CpuInfo()
    {
        Update();
    }

    public bool Update()
    {
        LogicalCpu = Helper.GetSysctlInt("hw.logicalcpu");
        PhysicalCpu = Helper.GetSysctlInt("hw.physicalcpu");
        Ncpu = Helper.GetSysctlInt("hw.ncpu");
        ActiveCpu = Helper.GetSysctlInt("hw.activecpu");
        BrandString = Helper.GetSysctlString("machdep.cpu.brand_string");
        CpuFrequency = Helper.GetSysctlLong("hw.cpufrequency");
        CacheLineSize = Helper.GetSysctlInt("hw.cachelinesize");
        L2CacheSize = Helper.GetSysctlLong("hw.l2cachesize");

        UpdateAt = DateTime.Now;

        return true;
    }
}
