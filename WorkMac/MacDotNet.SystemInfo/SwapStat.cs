namespace MacDotNet.SystemInfo;

using static MacDotNet.SystemInfo.NativeMethods;

public sealed class SwapStat
{
    public DateTime UpdateAt { get; private set; }

    public bool Supported { get; private set; }

    public ulong TotalBytes { get; private set; }

    public ulong AvailableBytes { get; private set; }

    public ulong UsedBytes { get; private set; }

    public int PageSize { get; private set; }

    public bool IsEncrypted { get; private set; }

    public double UsagePercent => TotalBytes > 0 ? 100.0 * UsedBytes / TotalBytes : 0;

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal SwapStat()
    {
        Update();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public unsafe bool Update()
    {
        xsw_usage swap;
        var len = (IntPtr)sizeof(xsw_usage);
        if (sysctlbyname("vm.swapusage", &swap, ref len, IntPtr.Zero, 0) != 0)
        {
            Supported = false;
            return false;
        }

        Supported = true;
        TotalBytes = swap.xsu_total;
        AvailableBytes = swap.xsu_avail;
        UsedBytes = swap.xsu_used;
        PageSize = swap.xsu_pagesize;
        IsEncrypted = swap.xsu_encrypted != 0;

        UpdateAt = DateTime.Now;

        return true;
    }
}
