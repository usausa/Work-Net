namespace WorkMemory;

using System.Runtime.InteropServices;

using static WorkMemory.NativeMethods;

internal static class Program
{
    public static void Main()
    {
        // メモリ情報
        var memInfo = MemoryInfoProvider.GetMemoryInfo();

        Console.WriteLine("=== Memory Info ===");
        Console.WriteLine($"Physical Memory:  {FormatBytes(memInfo.PhysicalMemory)}");
        Console.WriteLine($"Page Size:        {memInfo.PageSize} bytes");
        Console.WriteLine($"Used:             {FormatBytes(memInfo.UsedBytes)}");
        Console.WriteLine($"Free:             {FormatBytes(memInfo.FreeBytes)}");
        Console.WriteLine($"Usage:            {memInfo.UsagePercent:F1}%");
        Console.WriteLine($"App Memory:       {FormatBytes(memInfo.AppMemoryBytes)}");
        Console.WriteLine();

        // メモリカテゴリ別
        Console.WriteLine("=== Memory Categories ===");
        Console.WriteLine($"Active:           {FormatBytes(memInfo.ActiveBytes)}  ({memInfo.ActiveCount} pages)");
        Console.WriteLine($"Inactive:         {FormatBytes(memInfo.InactiveBytes)}  ({memInfo.InactiveCount} pages)");
        Console.WriteLine($"Wired:            {FormatBytes(memInfo.WiredBytes)}  ({memInfo.WireCount} pages)");
        Console.WriteLine($"Compressor:       {FormatBytes(memInfo.CompressorBytes)}  ({memInfo.CompressorPageCount} pages)");
        Console.WriteLine($"Purgeable:        {FormatBytes((ulong)memInfo.PurgeableCount * memInfo.PageSize)}  ({memInfo.PurgeableCount} pages)");
        Console.WriteLine($"Speculative:      {FormatBytes((ulong)memInfo.SpeculativeCount * memInfo.PageSize)}  ({memInfo.SpeculativeCount} pages)");
        Console.WriteLine($"Throttled:        {FormatBytes((ulong)memInfo.ThrottledCount * memInfo.PageSize)}  ({memInfo.ThrottledCount} pages)");
        Console.WriteLine($"External:         {FormatBytes((ulong)memInfo.ExternalPageCount * memInfo.PageSize)}  ({memInfo.ExternalPageCount} pages)");
        Console.WriteLine($"Internal:         {FormatBytes((ulong)memInfo.InternalPageCount * memInfo.PageSize)}  ({memInfo.InternalPageCount} pages)");
        Console.WriteLine($"Free:             {FormatBytes((ulong)memInfo.FreeCount * memInfo.PageSize)}  ({memInfo.FreeCount} pages)");
        Console.WriteLine($"Zero Fill:        {FormatBytes((ulong)memInfo.ZeroFillCount * memInfo.PageSize)}  ({memInfo.ZeroFillCount} pages)");
        Console.WriteLine($"Reactivated:      {FormatBytes((ulong)memInfo.ReactivatedCount * memInfo.PageSize)}  ({memInfo.ReactivatedCount} pages)");
        Console.WriteLine();

        // ページング統計 (累積値)
        Console.WriteLine("=== Paging Statistics (cumulative) ===");
        Console.WriteLine($"Faults:           {memInfo.Faults}");
        Console.WriteLine($"COW Faults:       {memInfo.CowFaults}");
        Console.WriteLine($"Pageins:          {memInfo.Pageins}");
        Console.WriteLine($"Pageouts:         {memInfo.Pageouts}");
        Console.WriteLine($"Swapins:          {memInfo.Swapins}");
        Console.WriteLine($"Swapouts:         {memInfo.Swapouts}");
        Console.WriteLine($"Lookups:          {memInfo.Lookups}");
        Console.WriteLine($"Hits:             {memInfo.Hits}");
        Console.WriteLine($"Purges:           {memInfo.Purges}");
        Console.WriteLine();

        // コンプレッサー統計
        Console.WriteLine("=== Compressor Statistics ===");
        Console.WriteLine($"Compressor Pages: {memInfo.CompressorPageCount}");
        Console.WriteLine($"Decompressions:   {memInfo.Decompressions}");
        Console.WriteLine($"Compressions:     {memInfo.Compressions}");
        Console.WriteLine($"Swapped Pages:    {memInfo.SwappedCount}");
        Console.WriteLine($"Compression Ratio:{(memInfo.CompressionRatio > 0 ? $" {memInfo.CompressionRatio:F2}x" : " N/A")}");
        Console.WriteLine();

        // スワップ情報
        var swapInfo = MemoryInfoProvider.GetSwapInfo();
        Console.WriteLine("=== Swap Info ===");
        if (swapInfo is not null)
        {
            Console.WriteLine($"Total:            {FormatBytes(swapInfo.TotalBytes)}");
            Console.WriteLine($"Used:             {FormatBytes(swapInfo.UsedBytes)}");
            Console.WriteLine($"Available:        {FormatBytes(swapInfo.AvailableBytes)}");
            Console.WriteLine($"Usage:            {swapInfo.UsagePercent:F1}%");
            Console.WriteLine($"Page Size:        {swapInfo.PageSize} bytes");
            Console.WriteLine($"Encrypted:        {swapInfo.IsEncrypted}");
        }
        else
        {
            Console.WriteLine("Swap info not available.");
        }

        Console.WriteLine();

        // ページサイズ比較
        Console.WriteLine("=== Page Size Comparison ===");
        Console.WriteLine($"host_page_size:          {memInfo.PageSize}");
        Console.WriteLine($"vm.pagesize (sysctl):    {MemoryInfoProvider.GetSysctlPageSize()}");
        Console.WriteLine($"Environment.SystemPage:  {MemoryInfoProvider.GetDotNetPageSize()}");
    }

    private static string FormatBytes(ulong bytes) => bytes switch
    {
        >= 1UL << 40 => $"{bytes / (double)(1UL << 40):F2} TiB",
        >= 1UL << 30 => $"{bytes / (double)(1UL << 30):F2} GiB",
        >= 1UL << 20 => $"{bytes / (double)(1UL << 20):F2} MiB",
        >= 1UL << 10 => $"{bytes / (double)(1UL << 10):F2} KiB",
        _ => $"{bytes} B",
    };
}

// メモリ情報
internal sealed record MemoryInfo
{
    // 物理メモリ総量(バイト) — hw.memsize
    public required ulong PhysicalMemory { get; init; }

    // カーネルのページサイズ(バイト) — host_page_size
    public required nuint PageSize { get; init; }

    // アクティブページ数 — 最近使用されたメモリ
    public required uint ActiveCount { get; init; }

    // 非アクティブページ数 — 最近使用されていないメモリ
    public required uint InactiveCount { get; init; }

    // ワイヤードページ数 — カーネルが占有するスワップ不可メモリ
    public required uint WireCount { get; init; }

    // 空きページ数 — 未使用メモリ
    public required uint FreeCount { get; init; }

    // ゼロフィルページ数 — ゼロで初期化されたページの累積
    public required ulong ZeroFillCount { get; init; }

    // 再アクティブ化ページ数 — 非アクティブから復帰したページの累積
    public required ulong ReactivatedCount { get; init; }

    // ページフォルト数 — ページフォルトの累積
    public required ulong Faults { get; init; }

    // コピーオンライトフォルト数 — COWフォルトの累積
    public required ulong CowFaults { get; init; }

    // ページイン数 — ディスクからメモリへ読み込んだページの累積
    public required ulong Pageins { get; init; }

    // ページアウト数 — メモリからディスクへ書き出したページの累積
    public required ulong Pageouts { get; init; }

    // スワップイン数 — スワップからメモリへ読み込んだページの累積
    public required ulong Swapins { get; init; }

    // スワップアウト数 — メモリからスワップへ書き出したページの累積
    public required ulong Swapouts { get; init; }

    // VMオブジェクトキャッシュ検索回数 — キャッシュルックアップの累積
    public required ulong Lookups { get; init; }

    // VMオブジェクトキャッシュヒット回数 — キャッシュヒットの累積
    public required ulong Hits { get; init; }

    // パージ可能ページ数 — システムが必要に応じて解放可能なメモリ
    public required uint PurgeableCount { get; init; }

    // パージ数 — パージ可能ページが解放された累積回数
    public required ulong Purges { get; init; }

    // 投機的読み込みページ数 — 先読みで読み込まれたページ
    public required uint SpeculativeCount { get; init; }

    // スロットル済みページ数 — スロットリングされたページ
    public required uint ThrottledCount { get; init; }

    // コンプレッサーページ数 — メモリ圧縮で圧縮されたデータを保持するページ
    public required uint CompressorPageCount { get; init; }

    // 展開回数 — コンプレッサーがページを展開した累積回数
    public required ulong Decompressions { get; init; }

    // 圧縮回数 — コンプレッサーがページを圧縮した累積回数
    public required ulong Compressions { get; init; }

    // 外部ページ数 — ファイルバックドメモリ(ファイルキャッシュ等)
    public required uint ExternalPageCount { get; init; }

    // 内部ページ数 — 匿名メモリ(アプリケーションのヒープ等)
    public required uint InternalPageCount { get; init; }

    // コンプレッサー内の非圧縮換算ページ数 — 圧縮前の合計ページ数
    public required ulong TotalUncompressedPagesInCompressor { get; init; }

    // コンプレッサー格納ページのうちスワップに保存されているページ数 (rev2)
    public required ulong SwappedCount { get; init; }

    // 使用メモリ(バイト) — Activity Monitor準拠: (active + wired + compressor) * pageSize
    public ulong UsedBytes => ((ulong)ActiveCount + WireCount + CompressorPageCount) * PageSize;

    // 空きメモリ(バイト) — 物理メモリから使用分を引いた値
    public ulong FreeBytes => PhysicalMemory > UsedBytes ? PhysicalMemory - UsedBytes : 0;

    // アクティブメモリ(バイト)
    public ulong ActiveBytes => (ulong)ActiveCount * PageSize;

    // 非アクティブメモリ(バイト)
    public ulong InactiveBytes => (ulong)InactiveCount * PageSize;

    // ワイヤードメモリ(バイト)
    public ulong WiredBytes => (ulong)WireCount * PageSize;

    // コンプレッサーメモリ(バイト)
    public ulong CompressorBytes => (ulong)CompressorPageCount * PageSize;

    // アプリケーションメモリ(バイト) — 内部ページからパージ可能ページを除いたもの
    public ulong AppMemoryBytes => InternalPageCount > PurgeableCount
        ? ((ulong)InternalPageCount - PurgeableCount) * PageSize
        : 0;

    // メモリ使用率(パーセント)
    public double UsagePercent => PhysicalMemory > 0 ? 100.0 * UsedBytes / PhysicalMemory : 0;

    // コンプレッサー圧縮率 — 圧縮前/圧縮後の比率(高いほど効率的)
    public double CompressionRatio => CompressorPageCount > 0
        ? (double)TotalUncompressedPagesInCompressor / CompressorPageCount
        : 0;
}

// スワップ情報
internal sealed record SwapInfo
{
    // スワップ総量(バイト)
    public required ulong TotalBytes { get; init; }

    // スワップ利用可能量(バイト)
    public required ulong AvailableBytes { get; init; }

    // スワップ使用量(バイト)
    public required ulong UsedBytes { get; init; }

    // スワップのページサイズ(バイト)
    public required int PageSize { get; init; }

    // スワップが暗号化されているか
    public required bool IsEncrypted { get; init; }

    // スワップ使用率(パーセント)
    public double UsagePercent => TotalBytes > 0 ? 100.0 * UsedBytes / TotalBytes : 0;
}

// メモリ情報取得
internal static class MemoryInfoProvider
{
    public static unsafe MemoryInfo GetMemoryInfo()
    {
        // ページサイズ取得
        var host = mach_host_self();
        var ret = host_page_size(host, out var pageSize);
        if (ret != KERN_SUCCESS)
        {
            throw new InvalidOperationException($"host_page_size failed: {ret}");
        }

        // VM統計取得
        var count = HOST_VM_INFO64_COUNT;
        vm_statistics64 vmStat;
        ret = host_statistics64(host, HOST_VM_INFO64, &vmStat, ref count);
        if (ret != KERN_SUCCESS)
        {
            throw new InvalidOperationException($"host_statistics64 failed: {ret}");
        }

        // 物理メモリ取得 (hw.memsize)
        var physicalMemory = GetSysctlUlong("hw.memsize");

        return new MemoryInfo
        {
            PhysicalMemory = physicalMemory,
            PageSize = pageSize,
            ActiveCount = vmStat.active_count,
            InactiveCount = vmStat.inactive_count,
            WireCount = vmStat.wire_count,
            FreeCount = vmStat.free_count,
            ZeroFillCount = vmStat.zero_fill_count,
            ReactivatedCount = vmStat.reactivations,
            Faults = vmStat.faults,
            CowFaults = vmStat.cow_faults,
            Pageins = vmStat.pageins,
            Pageouts = vmStat.pageouts,
            Swapins = vmStat.swapins,
            Swapouts = vmStat.swapouts,
            Lookups = vmStat.lookups,
            Hits = vmStat.hits,
            PurgeableCount = vmStat.purgeable_count,
            Purges = vmStat.purges,
            SpeculativeCount = vmStat.speculative_count,
            ThrottledCount = vmStat.throttled_count,
            CompressorPageCount = vmStat.compressor_page_count,
            Decompressions = vmStat.decompressions,
            Compressions = vmStat.compressions,
            ExternalPageCount = vmStat.external_page_count,
            InternalPageCount = vmStat.internal_page_count,
            TotalUncompressedPagesInCompressor = vmStat.total_uncompressed_pages_in_compressor,
            SwappedCount = vmStat.swapped_count,
        };
    }

    public static unsafe SwapInfo? GetSwapInfo()
    {
        xsw_usage swap;
        var len = (IntPtr)sizeof(xsw_usage);
        if (sysctlbyname("vm.swapusage", &swap, ref len, IntPtr.Zero, 0) != 0)
        {
            return null;
        }

        return new SwapInfo
        {
            TotalBytes = swap.xsu_total,
            AvailableBytes = swap.xsu_avail,
            UsedBytes = swap.xsu_used,
            PageSize = swap.xsu_pagesize,
            IsEncrypted = swap.xsu_encrypted != 0,
        };
    }

    // .NETランタイムのページサイズ (比較検証用)
    public static int GetDotNetPageSize() => Environment.SystemPageSize;

    // sysctlによるページサイズ (比較検証用)
    public static int GetSysctlPageSize() => GetSysctlInt("vm.pagesize");

    private static unsafe int GetSysctlInt(string name)
    {
        int value;
        var len = (IntPtr)sizeof(int);
        return sysctlbyname(name, &value, ref len, IntPtr.Zero, 0) == 0 ? value : 0;
    }

    private static unsafe ulong GetSysctlUlong(string name)
    {
        ulong value;
        var len = (IntPtr)sizeof(ulong);
        return sysctlbyname(name, &value, ref len, IntPtr.Zero, 0) == 0 ? value : 0;
    }
}

// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
#pragma warning disable CA2101
#pragma warning disable CA5392
#pragma warning disable CS8981
internal static class NativeMethods
{
    // host_statistics64のflavor (mach/host_info.h)
    public const int HOST_VM_INFO64 = 4;

    // 成功コード (mach/kern_return.h)
    public const int KERN_SUCCESS = 0;

    // vm_statistics64のサイズ (natural_t単位: 160bytes / sizeof(natural_t))
    public const int HOST_VM_INFO64_COUNT = 40;

    //------------------------------------------------------------------------
    // Struct
    //------------------------------------------------------------------------

    // VM統計情報 (mach/vm_statistics.h — vm_statistics64)
    // natural_t = uint (32bit), uint64_t = ulong (64bit)
    [StructLayout(LayoutKind.Sequential)]
    internal struct vm_statistics64
    {
        public uint free_count;                   // 空きページ数
        public uint active_count;                 // アクティブページ数
        public uint inactive_count;               // 非アクティブページ数
        public uint wire_count;                   // ワイヤードページ数
        public ulong zero_fill_count;             // ゼロフィルページ要求の累積
        public ulong reactivations;               // 再アクティブ化ページの累積
        public ulong pageins;                     // ページインの累積
        public ulong pageouts;                    // ページアウトの累積
        public ulong faults;                      // ページフォルトの累積
        public ulong cow_faults;                  // コピーオンライトフォルトの累積
        public ulong lookups;                     // VMオブジェクトキャッシュ検索の累積
        public ulong hits;                        // VMオブジェクトキャッシュヒットの累積
        public ulong purges;                      // パージの累積
        public uint purgeable_count;              // パージ可能ページ数
        public uint speculative_count;            // 投機的読み込みページ数
        public ulong decompressions;              // コンプレッサー展開の累積
        public ulong compressions;                // コンプレッサー圧縮の累積
        public ulong swapins;                     // スワップインの累積
        public ulong swapouts;                    // スワップアウトの累積
        public uint compressor_page_count;        // コンプレッサーが使用中のページ数
        public uint throttled_count;              // スロットル済みページ数
        public uint external_page_count;          // 外部ページ数(ファイルバックド)
        public uint internal_page_count;          // 内部ページ数(匿名メモリ)
        public ulong total_uncompressed_pages_in_compressor; // コンプレッサー内の非圧縮換算ページ数
        public ulong swapped_count;              // コンプレッサー格納ページのうちスワップに保存されているページ数 (rev2)
    }

    // スワップ使用情報 (sysvm.h — xsw_usage)
    [StructLayout(LayoutKind.Sequential)]
    internal struct xsw_usage
    {
        public ulong xsu_total;                   // スワップ総量(バイト)
        public ulong xsu_avail;                   // スワップ利用可能量(バイト)
        public ulong xsu_used;                    // スワップ使用量(バイト)
        public int xsu_pagesize;                  // スワップのページサイズ
        public int xsu_encrypted;                 // スワップの暗号化状態 (boolean_t = int)
    }

    //------------------------------------------------------------------------
    // Mach
    //------------------------------------------------------------------------

    [DllImport("libSystem.dylib")]
    public static extern uint mach_host_self();

    [DllImport("libSystem.dylib")]
    public static extern unsafe int host_statistics64(
        uint host_priv,
        int flavor,
        vm_statistics64* host_info_out,
        ref int host_info_outCnt);

    [DllImport("libSystem.dylib")]
    public static extern int host_page_size(uint host, out nuint page_size);

    //------------------------------------------------------------------------
    // libc
    //------------------------------------------------------------------------

    [DllImport("libc")]
    public static extern unsafe int sysctlbyname(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        void* oldp,
        ref IntPtr oldlenp,
        IntPtr newp,
        IntPtr newlen);
}
