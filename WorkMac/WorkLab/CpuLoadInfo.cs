namespace MacDotNet.SystemInfo.Lab;

using static NativeMethods;

/// <summary>
/// CPU使用率情報 (User/System/Idle分離、コア毎の使用率)
/// </summary>
public sealed class CpuLoadInfo
{
    private int[]? previousCpuTicks;
    private host_cpu_load_info previousTotalTicks;

    /// <summary>
    /// 論理CPU数
    /// </summary>
    public int LogicalCpu { get; }

    /// <summary>
    /// 物理CPU数
    /// </summary>
    public int PhysicalCpu { get; }

    /// <summary>
    /// ハイパースレッディング有効
    /// </summary>
    public bool HasHyperthreading { get; }

    /// <summary>
    /// 全体のユーザー使用率 (0.0-1.0)
    /// </summary>
    public double UserLoad { get; private set; }

    /// <summary>
    /// 全体のシステム使用率 (0.0-1.0)
    /// </summary>
    public double SystemLoad { get; private set; }

    /// <summary>
    /// 全体のアイドル率 (0.0-1.0)
    /// </summary>
    public double IdleLoad { get; private set; }

    /// <summary>
    /// 全体の合計使用率 (User + System)
    /// </summary>
    public double TotalLoad => UserLoad + SystemLoad;

    /// <summary>
    /// コア毎の使用率 (0.0-1.0)
    /// </summary>
    public double[] UsagePerCore { get; private set; } = [];

    /// <summary>
    /// E-Core (Efficiency) 平均使用率 (Apple Silicon)
    /// </summary>
    public double? ECoreUsage { get; private set; }

    /// <summary>
    /// P-Core (Performance) 平均使用率 (Apple Silicon)
    /// </summary>
    public double? PCoreUsage { get; private set; }

    private CpuLoadInfo()
    {
        LogicalCpu = GetSysctlInt("hw.logicalcpu");
        PhysicalCpu = GetSysctlInt("hw.physicalcpu");
        HasHyperthreading = LogicalCpu != PhysicalCpu;
    }

    public static CpuLoadInfo Create() => new();

    public unsafe bool Update()
    {
        // 全体のCPU使用率
        var totalTicks = GetHostCpuLoadInfo();
        if (totalTicks is { } current)
        {
            var userDiff = current.cpu_ticks_user - previousTotalTicks.cpu_ticks_user;
            var sysDiff = current.cpu_ticks_system - previousTotalTicks.cpu_ticks_system;
            var idleDiff = current.cpu_ticks_idle - previousTotalTicks.cpu_ticks_idle;
            var niceDiff = current.cpu_ticks_nice - previousTotalTicks.cpu_ticks_nice;
            var totalDiff = userDiff + sysDiff + idleDiff + niceDiff;

            if (totalDiff > 0)
            {
                UserLoad = (double)userDiff / totalDiff;
                SystemLoad = (double)sysDiff / totalDiff;
                IdleLoad = (double)idleDiff / totalDiff;
            }

            previousTotalTicks = current;
        }

        // コア毎のCPU使用率
        var host = mach_host_self();
        int* cpuInfo;
        var result = host_processor_info(host, PROCESSOR_CPU_LOAD_INFO, out var numCpus, out cpuInfo, out var cpuInfoCount);
        if (result != KERN_SUCCESS)
        {
            return false;
        }

        try
        {
            var usageList = new double[numCpus];
            var currentTicks = new int[numCpus * CPU_STATE_MAX];

            for (var i = 0; i < numCpus; i++)
            {
                var offset = i * CPU_STATE_MAX;
                currentTicks[offset + CPU_STATE_USER] = cpuInfo[offset + CPU_STATE_USER];
                currentTicks[offset + CPU_STATE_SYSTEM] = cpuInfo[offset + CPU_STATE_SYSTEM];
                currentTicks[offset + CPU_STATE_IDLE] = cpuInfo[offset + CPU_STATE_IDLE];
                currentTicks[offset + CPU_STATE_NICE] = cpuInfo[offset + CPU_STATE_NICE];

                if (previousCpuTicks is not null)
                {
                    var inUse = (currentTicks[offset + CPU_STATE_USER] - previousCpuTicks[offset + CPU_STATE_USER])
                                + (currentTicks[offset + CPU_STATE_SYSTEM] - previousCpuTicks[offset + CPU_STATE_SYSTEM])
                                + (currentTicks[offset + CPU_STATE_NICE] - previousCpuTicks[offset + CPU_STATE_NICE]);
                    var total = inUse + (currentTicks[offset + CPU_STATE_IDLE] - previousCpuTicks[offset + CPU_STATE_IDLE]);

                    usageList[i] = total > 0 ? (double)inUse / total : 0;
                }
            }

            previousCpuTicks = currentTicks;
            UsagePerCore = usageList;

            // Apple Silicon E-Core/P-Core計算
            CalculateAppleSiliconCoreUsage();
        }
        finally
        {
            vm_deallocate(mach_task_self(), (IntPtr)cpuInfo, (nuint)(cpuInfoCount * sizeof(int)));
        }

        return true;
    }

    private void CalculateAppleSiliconCoreUsage()
    {
        var nperflevels = GetSysctlInt("hw.nperflevels");
        if (nperflevels <= 0)
        {
            ECoreUsage = null;
            PCoreUsage = null;
            return;
        }

        var eCoreCount = GetSysctlInt("hw.perflevel0.logicalcpu");
        var pCoreCount = GetSysctlInt("hw.perflevel1.logicalcpu");

        if (eCoreCount > 0 && UsagePerCore.Length >= eCoreCount)
        {
            ECoreUsage = UsagePerCore.Take(eCoreCount).Average();
        }

        if (pCoreCount > 0 && UsagePerCore.Length >= eCoreCount + pCoreCount)
        {
            PCoreUsage = UsagePerCore.Skip(eCoreCount).Take(pCoreCount).Average();
        }
    }

    private static unsafe host_cpu_load_info? GetHostCpuLoadInfo()
    {
        var count = 4;
        host_cpu_load_info info;
        var result = host_statistics64(mach_host_self(), HOST_CPU_LOAD_INFO, &info, ref count);
        return result == KERN_SUCCESS ? info : null;
    }
}
