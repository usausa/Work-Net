namespace MacDotNet.SystemInfo;

using static MacDotNet.SystemInfo.NativeMethods;

public readonly record struct CpuLoadTicks(int CpuNumber, uint User, uint System, uint Idle, uint Nice);

public sealed class CpuUsageInfo
{
    public DateTime UpdateAt { get; private set; }

    public CpuLoadTicks[] Ticks { get; private set; } = [];

    internal CpuUsageInfo()
    {
        Update();
    }

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
            var ticks = new CpuLoadTicks[processorCount];
            var ptr = (uint*)info;

            for (var i = 0; i < processorCount; i++)
            {
                var offset = i * CPU_STATE_MAX;
                ticks[i] = new CpuLoadTicks(
                    i,
                    ptr[offset + CPU_STATE_USER],
                    ptr[offset + CPU_STATE_SYSTEM],
                    ptr[offset + CPU_STATE_IDLE],
                    ptr[offset + CPU_STATE_NICE]);
            }

            Ticks = ticks;
            UpdateAt = DateTime.Now;

            return true;
        }
        finally
        {
            _ = vm_deallocate(task_self_trap(), info, (nint)(infoCnt * sizeof(int)));
        }
    }
}
