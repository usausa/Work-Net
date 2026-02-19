namespace WorkHardware;

using System.Runtime.InteropServices;

using static WorkHardware.NativeMethods;

internal static class Program
{
    public static void Main()
    {
        var hw = HardwareInfoProvider.GetHardwareInfo();

        Console.WriteLine("=== Hardware ===");
        Console.WriteLine($"Model:            {hw.Model}");
        Console.WriteLine($"Machine:          {hw.Machine}");
        Console.WriteLine($"Target Type:      {hw.TargetType ?? "(unavailable)"}");
        Console.WriteLine($"CPU Brand:        {hw.CpuBrandString ?? "(unavailable)"}");
        Console.WriteLine($"Packages:         {hw.Packages}");
        Console.WriteLine($"64-bit Capable:   {hw.Cpu64BitCapable}");
        Console.WriteLine($"Byte Order:       {(hw.ByteOrder == 1234 ? "Little Endian" : "Big Endian")} ({hw.ByteOrder})");
        Console.WriteLine();

        Console.WriteLine("=== CPU ===");
        Console.WriteLine($"Physical CPU:     {hw.PhysicalCpu}");
        Console.WriteLine($"Physical CPU Max: {hw.PhysicalCpuMax}");
        Console.WriteLine($"Logical CPU:      {hw.LogicalCpu}");
        Console.WriteLine($"Logical CPU Max:  {hw.LogicalCpuMax}");
        Console.WriteLine($"Active CPU:       {hw.ActiveCpu}");
        Console.WriteLine($"ncpu:             {hw.Ncpu}");
        if (hw.CpuCoreCount > 0)
        {
            Console.WriteLine($"Core Count:       {hw.CpuCoreCount}");
        }

        if (hw.CpuThreadCount > 0)
        {
            Console.WriteLine($"Thread Count:     {hw.CpuThreadCount}");
        }

        Console.WriteLine();

        Console.WriteLine("=== Frequency ===");
        if (hw.CpuFrequency > 0)
        {
            Console.WriteLine($"CPU Frequency:    {hw.CpuFrequency / 1_000_000_000.0:F2} GHz");
        }
        else
        {
            Console.WriteLine($"CPU Frequency:    (unavailable)");
        }

        if (hw.CpuFrequencyMax > 0)
        {
            Console.WriteLine($"CPU Freq Max:     {hw.CpuFrequencyMax / 1_000_000_000.0:F2} GHz");
        }

        if (hw.BusFrequency > 0)
        {
            Console.WriteLine($"Bus Frequency:    {hw.BusFrequency / 1_000_000_000.0:F2} GHz");
        }

        Console.WriteLine($"TB Frequency:     {hw.TbFrequency / 1_000_000.0:F2} MHz");
        Console.WriteLine();

        Console.WriteLine("=== Memory ===");
        Console.WriteLine($"Physical Memory:  {FormatBytes((ulong)hw.MemSize)}");
        Console.WriteLine($"Page Size:        {hw.PageSize} bytes");
        Console.WriteLine();

        Console.WriteLine("=== Cache ===");
        if (hw.CacheLineSize > 0)
        {
            Console.WriteLine($"Cache Line Size:  {hw.CacheLineSize} bytes");
        }

        if (hw.L1ICacheSize > 0)
        {
            Console.WriteLine($"L1 I-Cache:       {FormatBytes((ulong)hw.L1ICacheSize)}");
        }

        if (hw.L1DCacheSize > 0)
        {
            Console.WriteLine($"L1 D-Cache:       {FormatBytes((ulong)hw.L1DCacheSize)}");
        }

        if (hw.L2CacheSize > 0)
        {
            Console.WriteLine($"L2 Cache:         {FormatBytes((ulong)hw.L2CacheSize)}");
        }

        if (hw.L3CacheSize > 0)
        {
            Console.WriteLine($"L3 Cache:         {FormatBytes((ulong)hw.L3CacheSize)}");
        }

        Console.WriteLine();

        // Apple Silicon パフォーマンスレベル
        var perfLevels = HardwareInfoProvider.GetPerformanceLevels();
        if (perfLevels.Length > 0)
        {
            Console.WriteLine("=== Performance Levels (Apple Silicon) ===");
            Console.WriteLine($"Number of Levels: {perfLevels.Length}");
            foreach (var level in perfLevels)
            {
                Console.WriteLine($"  [{level.Index}] {level.Name}:");
                Console.WriteLine($"      Physical CPU:  {level.PhysicalCpu}");
                Console.WriteLine($"      Logical CPU:   {level.LogicalCpu}");
                Console.WriteLine($"      CPUs per L2:   {level.CpusPerL2}");
                Console.WriteLine($"      L2 Cache:      {FormatBytes((ulong)level.L2CacheSize)}");
            }

            Console.WriteLine();
        }

        // カーネル情報
        var kern = HardwareInfoProvider.GetKernelInfo();

        Console.WriteLine("=== Kernel ===");
        Console.WriteLine($"OS Type:          {kern.OsType}");
        Console.WriteLine($"OS Release:       {kern.OsRelease}");
        Console.WriteLine($"OS Version:       {kern.OsVersion}");
        Console.WriteLine($"OS Product Ver:   {kern.OsProductVersion ?? "(unavailable)"}");
        Console.WriteLine($"OS Revision:      {kern.OsRevision}");
        Console.WriteLine($"Kernel Version:   {kern.KernelVersion}");
        Console.WriteLine($"Hostname:         {kern.Hostname}");
        Console.WriteLine($"UUID:             {kern.Uuid}");
        Console.WriteLine();

        Console.WriteLine("=== Kernel Limits ===");
        Console.WriteLine($"Max Processes:    {kern.MaxProc}");
        Console.WriteLine($"Max Files:        {kern.MaxFiles}");
        Console.WriteLine($"Max Files/Proc:   {kern.MaxFilesPerProc}");
        Console.WriteLine($"Arg Max:          {FormatBytes((ulong)kern.ArgMax)}");
        Console.WriteLine($"Secure Level:     {kern.SecureLevel}");
        Console.WriteLine();

        Console.WriteLine("=== Boot Time ===");
        if (kern.BootTime != DateTimeOffset.MinValue)
        {
            Console.WriteLine($"Boot Time:        {kern.BootTime:yyyy-MM-dd HH:mm:ss zzz}");
            Console.WriteLine($"Uptime:           {DateTimeOffset.UtcNow - kern.BootTime:d\\.hh\\:mm\\:ss}");
        }
        else
        {
            Console.WriteLine($"Boot Time:        (unavailable)");
        }
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

// ハードウェア情報
internal sealed record HardwareInfo
{
    // モデル識別子 (hw.model — 例: "Mac14,12")
    public required string Model { get; init; }

    // マシンアーキテクチャ (hw.machine — 例: "arm64", "x86_64")
    public required string Machine { get; init; }

    // ターゲットタイプ (hw.targettype — 例: "J474s"、ボード識別子)
    public string? TargetType { get; init; }

    // CPUブランド名 (machdep.cpu.brand_string — 例: "Apple M2 Pro")
    public string? CpuBrandString { get; init; }

    // 論理CPU数 (hw.logicalcpu)
    public required int LogicalCpu { get; init; }

    // 論理CPU最大数 (hw.logicalcpu_max)
    public required int LogicalCpuMax { get; init; }

    // 物理CPU数 (hw.physicalcpu)
    public required int PhysicalCpu { get; init; }

    // 物理CPU最大数 (hw.physicalcpu_max)
    public required int PhysicalCpuMax { get; init; }

    // アクティブCPU数 (hw.activecpu — 省電力モード等で変動する場合がある)
    public required int ActiveCpu { get; init; }

    // CPU数 (hw.ncpu — 通常はLogicalCpuと同値)
    public required int Ncpu { get; init; }

    // CPUコア数 (machdep.cpu.core_count — Apple Siliconで取得可)
    public int CpuCoreCount { get; init; }

    // CPUスレッド数 (machdep.cpu.thread_count — Apple Siliconで取得可)
    public int CpuThreadCount { get; init; }

    // CPU周波数(Hz) (hw.cpufrequency — Apple Siliconでは取得不可の場合あり)
    public long CpuFrequency { get; init; }

    // CPU最大周波数(Hz) (hw.cpufrequency_max)
    public long CpuFrequencyMax { get; init; }

    // バス周波数(Hz) (hw.busfrequency — Apple Siliconでは取得不可の場合あり)
    public long BusFrequency { get; init; }

    // タイムベース周波数(Hz) (hw.tbfrequency — Mach absolute timeの基準周波数)
    public required long TbFrequency { get; init; }

    // 物理メモリ総量(バイト) (hw.memsize)
    public required long MemSize { get; init; }

    // ページサイズ(バイト) (hw.pagesize)
    public required long PageSize { get; init; }

    // バイトオーダー (hw.byteorder — 1234=Little Endian, 4321=Big Endian)
    public required int ByteOrder { get; init; }

    // キャッシュラインサイズ(バイト) (hw.cachelinesize)
    public long CacheLineSize { get; init; }

    // L1命令キャッシュサイズ(バイト) (hw.l1icachesize)
    public long L1ICacheSize { get; init; }

    // L1データキャッシュサイズ(バイト) (hw.l1dcachesize)
    public long L1DCacheSize { get; init; }

    // L2キャッシュサイズ(バイト) (hw.l2cachesize)
    public long L2CacheSize { get; init; }

    // L3キャッシュサイズ(バイト) (hw.l3cachesize — プロセッサにより存在しない場合あり)
    public long L3CacheSize { get; init; }

    // パッケージ数 (hw.packages — 物理CPUパッケージの数)
    public required int Packages { get; init; }

    // 64ビット対応 (hw.cpu64bit_capable)
    public required bool Cpu64BitCapable { get; init; }
}

// Apple Silicon パフォーマンスレベル情報
internal sealed record PerformanceLevelInfo
{
    // レベルインデックス (0=Performance, 1=Efficiency等)
    public required int Index { get; init; }

    // レベル名 (hw.perflevelN.name — "Performance", "Efficiency"等)
    public required string Name { get; init; }

    // 物理CPU数 (hw.perflevelN.physicalcpu)
    public required int PhysicalCpu { get; init; }

    // 論理CPU数 (hw.perflevelN.logicalcpu)
    public required int LogicalCpu { get; init; }

    // L2キャッシュあたりのCPU数 (hw.perflevelN.cpusperl2)
    public required int CpusPerL2 { get; init; }

    // L2キャッシュサイズ(バイト) (hw.perflevelN.l2cachesize)
    public required int L2CacheSize { get; init; }
}

// カーネル情報
internal sealed record KernelInfo
{
    // OS種別 (kern.ostype — 例: "Darwin")
    public required string OsType { get; init; }

    // OSリリースバージョン (kern.osrelease — 例: "25.2.0")
    public required string OsRelease { get; init; }

    // OSビルドバージョン (kern.osversion — 例: "25C56")
    public required string OsVersion { get; init; }

    // OS製品バージョン (kern.osproductversion — 例: "26.2")
    public string? OsProductVersion { get; init; }

    // OSリビジョン番号 (kern.osrevision)
    public required int OsRevision { get; init; }

    // カーネルバージョン文字列 (kern.version — ビルド詳細を含む完全なバージョン文字列)
    public required string KernelVersion { get; init; }

    // ホスト名 (kern.hostname)
    public required string Hostname { get; init; }

    // システムUUID (kern.uuid)
    public required string Uuid { get; init; }

    // システム起動時刻 (kern.boottime — struct timevalから変換)
    public required DateTimeOffset BootTime { get; init; }

    // プロセス最大数 (kern.maxproc)
    public required int MaxProc { get; init; }

    // オープンファイル最大数 (kern.maxfiles — システム全体)
    public required int MaxFiles { get; init; }

    // プロセスあたりオープンファイル最大数 (kern.maxfilesperproc)
    public required int MaxFilesPerProc { get; init; }

    // コマンドライン引数の最大バイト数 (kern.argmax)
    public required int ArgMax { get; init; }

    // セキュアレベル (kern.securelevel — 0=通常, -1=永久非セキュア, 1=セキュア)
    public required int SecureLevel { get; init; }
}

// ハードウェア情報取得
internal static class HardwareInfoProvider
{
    public static HardwareInfo GetHardwareInfo()
    {
        return new HardwareInfo
        {
            Model = GetSysctlString("hw.model") ?? string.Empty,
            Machine = GetSysctlString("hw.machine") ?? string.Empty,
            TargetType = GetSysctlString("hw.targettype"),
            CpuBrandString = GetSysctlString("machdep.cpu.brand_string"),
            LogicalCpu = GetSysctlInt("hw.logicalcpu"),
            LogicalCpuMax = GetSysctlInt("hw.logicalcpu_max"),
            PhysicalCpu = GetSysctlInt("hw.physicalcpu"),
            PhysicalCpuMax = GetSysctlInt("hw.physicalcpu_max"),
            ActiveCpu = GetSysctlInt("hw.activecpu"),
            Ncpu = GetSysctlInt("hw.ncpu"),
            CpuCoreCount = GetSysctlInt("machdep.cpu.core_count"),
            CpuThreadCount = GetSysctlInt("machdep.cpu.thread_count"),
            CpuFrequency = GetSysctlLong("hw.cpufrequency"),
            CpuFrequencyMax = GetSysctlLong("hw.cpufrequency_max"),
            BusFrequency = GetSysctlLong("hw.busfrequency"),
            TbFrequency = GetSysctlLong("hw.tbfrequency"),
            MemSize = GetSysctlLong("hw.memsize"),
            PageSize = GetSysctlLong("hw.pagesize"),
            ByteOrder = GetSysctlInt("hw.byteorder"),
            CacheLineSize = GetSysctlLong("hw.cachelinesize"),
            L1ICacheSize = GetSysctlLong("hw.l1icachesize"),
            L1DCacheSize = GetSysctlLong("hw.l1dcachesize"),
            L2CacheSize = GetSysctlLong("hw.l2cachesize"),
            L3CacheSize = GetSysctlLong("hw.l3cachesize"),
            Packages = GetSysctlInt("hw.packages"),
            Cpu64BitCapable = GetSysctlInt("hw.cpu64bit_capable") != 0,
        };
    }

    public static PerformanceLevelInfo[] GetPerformanceLevels()
    {
        var count = GetSysctlInt("hw.nperflevels");
        if (count <= 0)
        {
            return [];
        }

        var levels = new PerformanceLevelInfo[count];
        for (var i = 0; i < count; i++)
        {
            levels[i] = new PerformanceLevelInfo
            {
                Index = i,
                Name = GetSysctlString($"hw.perflevel{i}.name") ?? $"Level {i}",
                PhysicalCpu = GetSysctlInt($"hw.perflevel{i}.physicalcpu"),
                LogicalCpu = GetSysctlInt($"hw.perflevel{i}.logicalcpu"),
                CpusPerL2 = GetSysctlInt($"hw.perflevel{i}.cpusperl2"),
                L2CacheSize = GetSysctlInt($"hw.perflevel{i}.l2cachesize"),
            };
        }

        return levels;
    }

    public static unsafe KernelInfo GetKernelInfo()
    {
        // kern.boottime は struct timeval (16バイト)
        var bootTime = DateTimeOffset.MinValue;
        timeval tv;
        var len = (IntPtr)sizeof(timeval);
        if (sysctlbyname("kern.boottime", &tv, ref len, IntPtr.Zero, 0) == 0)
        {
            bootTime = DateTimeOffset.FromUnixTimeSeconds(tv.tv_sec);
        }

        return new KernelInfo
        {
            OsType = GetSysctlString("kern.ostype") ?? string.Empty,
            OsRelease = GetSysctlString("kern.osrelease") ?? string.Empty,
            OsVersion = GetSysctlString("kern.osversion") ?? string.Empty,
            OsProductVersion = GetSysctlString("kern.osproductversion"),
            OsRevision = GetSysctlInt("kern.osrevision"),
            KernelVersion = GetSysctlString("kern.version") ?? string.Empty,
            Hostname = GetSysctlString("kern.hostname") ?? string.Empty,
            Uuid = GetSysctlString("kern.uuid") ?? string.Empty,
            BootTime = bootTime,
            MaxProc = GetSysctlInt("kern.maxproc"),
            MaxFiles = GetSysctlInt("kern.maxfiles"),
            MaxFilesPerProc = GetSysctlInt("kern.maxfilesperproc"),
            ArgMax = GetSysctlInt("kern.argmax"),
            SecureLevel = GetSysctlInt("kern.securelevel"),
        };
    }

    private static unsafe int GetSysctlInt(string name)
    {
        int value;
        var len = (IntPtr)sizeof(int);
        return sysctlbyname(name, &value, ref len, IntPtr.Zero, 0) == 0 ? value : 0;
    }

    private static unsafe long GetSysctlLong(string name)
    {
        long value;
        var len = (IntPtr)sizeof(long);
        return sysctlbyname(name, &value, ref len, IntPtr.Zero, 0) == 0 ? value : 0;
    }

    private static unsafe string? GetSysctlString(string name)
    {
        // 1回目: サイズ取得
        var len = (IntPtr)0;
        if (sysctlbyname(name, null, ref len, IntPtr.Zero, 0) != 0 || len <= 0)
        {
            return null;
        }

        // サイズ上限チェック (スタックオーバーフロー防止)
        if (len > 1024)
        {
            return null;
        }

        // 2回目: 値取得 (2回目の呼び出しでサイズが変わっても、allocatedSize以下しか書き込まれない)
        var allocatedSize = len;
        var buffer = stackalloc byte[(int)allocatedSize];
        return sysctlbyname(name, buffer, ref len, IntPtr.Zero, 0) == 0
            ? Marshal.PtrToStringUTF8((IntPtr)buffer)
            : null;
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
    //------------------------------------------------------------------------
    // Struct
    //------------------------------------------------------------------------

    // 時刻構造体 (sys/time.h — struct timeval)
    [StructLayout(LayoutKind.Sequential)]
    internal struct timeval
    {
        public long tv_sec;     // 秒 (time_t = long on arm64/x86_64)
        public int tv_usec;     // マイクロ秒 (suseconds_t = int)
        private readonly int _padding; // 8バイトアライメント用パディング
    }

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
