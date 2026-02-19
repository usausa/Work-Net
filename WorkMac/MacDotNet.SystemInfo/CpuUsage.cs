namespace MacDotNet.SystemInfo;

using static MacDotNet.SystemInfo.NativeMethods;

public readonly record struct CpuLoadTicks(int CpuNumber, uint User, uint System, uint Idle, uint Nice);

public sealed class CpuUsage
{
    private int[]? previousCpuTicks;
    private uint previousUserTicks;
    private uint previousSystemTicks;
    private uint previousIdleTicks;
    private uint previousNiceTicks;

    public DateTime UpdateAt { get; private set; }

    public int LogicalCpu { get; }

    public int PhysicalCpu { get; }

    public bool HasHyperthreading { get; }

    public double UserLoad { get; private set; }

    public double SystemLoad { get; private set; }

    public double IdleLoad { get; private set; }

    public double TotalLoad => UserLoad + SystemLoad;

    public CpuLoadTicks[] Ticks { get; private set; } = [];

    public double[] UsagePerCore { get; private set; } = [];

    public double? ECoreUsage { get; private set; }

    public double? PCoreUsage { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private CpuUsage()
    {
        LogicalCpu = Helper.GetSysctlInt("hw.logicalcpu");
        PhysicalCpu = Helper.GetSysctlInt("hw.physicalcpu");
        HasHyperthreading = LogicalCpu != PhysicalCpu;
    }

    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    public static CpuUsage Create() => new();

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public unsafe bool Update()
    {
        var host = mach_host_self();
        var result = host_processor_info(host, PROCESSOR_CPU_LOAD_INFO, out var processorCount, out var info, out var infoCnt);
        if (result != KERN_SUCCESS)
        {
            return false;
        }

        try
        {
            var ptr = (uint*)info;

            uint totalUser = 0, totalSystem = 0, totalIdle = 0, totalNice = 0;
            var ticks = new CpuLoadTicks[processorCount];
            var usageList = new double[processorCount];
            var currentTicks = new int[processorCount * CPU_STATE_MAX];

            for (var i = 0; i < processorCount; i++)
            {
                var offset = i * CPU_STATE_MAX;
                var user = ptr[offset + CPU_STATE_USER];
                var system = ptr[offset + CPU_STATE_SYSTEM];
                var idle = ptr[offset + CPU_STATE_IDLE];
                var nice = ptr[offset + CPU_STATE_NICE];

                ticks[i] = new CpuLoadTicks(i, user, system, idle, nice);

                currentTicks[offset + CPU_STATE_USER] = (int)user;
                currentTicks[offset + CPU_STATE_SYSTEM] = (int)system;
                currentTicks[offset + CPU_STATE_IDLE] = (int)idle;
                currentTicks[offset + CPU_STATE_NICE] = (int)nice;

                totalUser += user;
                totalSystem += system;
                totalIdle += idle;
                totalNice += nice;

                if (previousCpuTicks is not null)
                {
                    var userDiff = user - (uint)previousCpuTicks[offset + CPU_STATE_USER];
                    var systemDiff = system - (uint)previousCpuTicks[offset + CPU_STATE_SYSTEM];
                    var idleDiff = idle - (uint)previousCpuTicks[offset + CPU_STATE_IDLE];
                    var niceDiff = nice - (uint)previousCpuTicks[offset + CPU_STATE_NICE];
                    var total = userDiff + systemDiff + idleDiff + niceDiff;

                    usageList[i] = total > 0 ? (double)(userDiff + systemDiff + niceDiff) / total : 0;
                }
            }

            if (previousCpuTicks is not null)
            {
                var userDiff = totalUser - previousUserTicks;
                var sysDiff = totalSystem - previousSystemTicks;
                var idleDiff = totalIdle - previousIdleTicks;
                var niceDiff = totalNice - previousNiceTicks;
                var totalDiff = userDiff + sysDiff + idleDiff + niceDiff;

                if (totalDiff > 0)
                {
                    UserLoad = (double)userDiff / totalDiff;
                    SystemLoad = (double)sysDiff / totalDiff;
                    IdleLoad = (double)idleDiff / totalDiff;
                }
            }

            previousCpuTicks = currentTicks;
            previousUserTicks = totalUser;
            previousSystemTicks = totalSystem;
            previousIdleTicks = totalIdle;
            previousNiceTicks = totalNice;
            Ticks = ticks;
            UsagePerCore = usageList;

            CalculateAppleSiliconCoreUsage();

            UpdateAt = DateTime.Now;

            return true;
        }
        finally
        {
            _ = vm_deallocate(task_self_trap(), info, (IntPtr)(infoCnt * sizeof(int)));
        }
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private void CalculateAppleSiliconCoreUsage()
    {
        var nperflevels = Helper.GetSysctlInt("hw.nperflevels");
        if (nperflevels <= 0)
        {
            ECoreUsage = null;
            PCoreUsage = null;
            return;
        }

        var eCoreCount = Helper.GetSysctlInt("hw.perflevel0.logicalcpu");
        var pCoreCount = Helper.GetSysctlInt("hw.perflevel1.logicalcpu");

        if (eCoreCount > 0 && UsagePerCore.Length >= eCoreCount)
        {
            ECoreUsage = UsagePerCore.Take(eCoreCount).Average();
        }

        if (pCoreCount > 0 && UsagePerCore.Length >= eCoreCount + pCoreCount)
        {
            PCoreUsage = UsagePerCore.Skip(eCoreCount).Take(pCoreCount).Average();
        }
    }
}
