namespace MacDotNet.SystemInfo;

using System.Runtime.InteropServices;

using static MacDotNet.SystemInfo.NativeMethods;

public sealed class MemoryStat
{
    public DateTime UpdateAt { get; private set; }

    public ulong PhysicalMemory { get; private set; }

    public nuint PageSize { get; private set; }

    public uint ActiveCount { get; private set; }

    public uint InactiveCount { get; private set; }

    public uint WireCount { get; private set; }

    public uint FreeCount { get; private set; }

    public ulong ZeroFillCount { get; private set; }

    public ulong ReactivatedCount { get; private set; }

    public ulong Faults { get; private set; }

    public ulong CowFaults { get; private set; }

    public ulong Pageins { get; private set; }

    public ulong Pageouts { get; private set; }

    public ulong Swapins { get; private set; }

    public ulong Swapouts { get; private set; }

    public ulong Lookups { get; private set; }

    public ulong Hits { get; private set; }

    public uint PurgeableCount { get; private set; }

    public ulong Purges { get; private set; }

    public uint SpeculativeCount { get; private set; }

    public uint ThrottledCount { get; private set; }

    public uint CompressorPageCount { get; private set; }

    public ulong Decompressions { get; private set; }

    public ulong Compressions { get; private set; }

    public uint ExternalPageCount { get; private set; }

    public uint InternalPageCount { get; private set; }

    public ulong TotalUncompressedPagesInCompressor { get; private set; }

    public ulong SwappedCount { get; private set; }

    public ulong UsedBytes => ((ulong)ActiveCount + WireCount + CompressorPageCount) * PageSize;

    public ulong FreeBytes => PhysicalMemory > UsedBytes ? PhysicalMemory - UsedBytes : 0;

    public ulong ActiveBytes => (ulong)ActiveCount * PageSize;

    public ulong InactiveBytes => (ulong)InactiveCount * PageSize;

    public ulong WiredBytes => (ulong)WireCount * PageSize;

    public ulong CompressorBytes => (ulong)CompressorPageCount * PageSize;

    public ulong AppMemoryBytes => InternalPageCount > PurgeableCount
        ? ((ulong)InternalPageCount - PurgeableCount) * PageSize
        : 0;

    public double UsagePercent => PhysicalMemory > 0 ? 100.0 * UsedBytes / PhysicalMemory : 0;

    public double CompressionRatio => CompressorPageCount > 0
        ? (double)TotalUncompressedPagesInCompressor / CompressorPageCount
        : 0;

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal MemoryStat()
    {
        PhysicalMemory = Helper.GetSysctlUlong("hw.memsize");
        Update();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public unsafe bool Update()
    {
        var host = mach_host_self();
        var ret = host_page_size(host, out var pageSize);
        if (ret != KERN_SUCCESS)
        {
            return false;
        }

        var count = HOST_VM_INFO64_COUNT;
        vm_statistics64 vmStat;
        ret = host_statistics64(host, HOST_VM_INFO64, &vmStat, ref count);
        if (ret != KERN_SUCCESS)
        {
            return false;
        }

        PageSize = pageSize;
        ActiveCount = vmStat.active_count;
        InactiveCount = vmStat.inactive_count;
        WireCount = vmStat.wire_count;
        FreeCount = vmStat.free_count;
        ZeroFillCount = vmStat.zero_fill_count;
        ReactivatedCount = vmStat.reactivations;
        Faults = vmStat.faults;
        CowFaults = vmStat.cow_faults;
        Pageins = vmStat.pageins;
        Pageouts = vmStat.pageouts;
        Swapins = vmStat.swapins;
        Swapouts = vmStat.swapouts;
        Lookups = vmStat.lookups;
        Hits = vmStat.hits;
        PurgeableCount = vmStat.purgeable_count;
        Purges = vmStat.purges;
        SpeculativeCount = vmStat.speculative_count;
        ThrottledCount = vmStat.throttled_count;
        CompressorPageCount = vmStat.compressor_page_count;
        Decompressions = vmStat.decompressions;
        Compressions = vmStat.compressions;
        ExternalPageCount = vmStat.external_page_count;
        InternalPageCount = vmStat.internal_page_count;
        TotalUncompressedPagesInCompressor = vmStat.total_uncompressed_pages_in_compressor;
        SwappedCount = vmStat.swapped_count;

        UpdateAt = DateTime.Now;

        return true;
    }
}
