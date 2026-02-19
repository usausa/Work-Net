namespace WorkCpu;

using System.Runtime.InteropServices;

using static WorkCpu.NativeMethods;

internal static class Program
{
    public static void Main()
    {
        // CPU数情報
        var cpuCount = CpuInfo.GetCpuCount();
        Console.WriteLine("=== CPU Count ===");
        Console.WriteLine($"Logical CPU:    {cpuCount.LogicalCpu}");
        Console.WriteLine($"Physical CPU:   {cpuCount.PhysicalCpu}");
        Console.WriteLine($"ncpu:           {cpuCount.Ncpu}");
        Console.WriteLine($"Active CPU:     {cpuCount.ActiveCpu}");
        if (cpuCount.BrandString is not null)
        {
            Console.WriteLine($"Brand:          {cpuCount.BrandString}");
        }

        if (cpuCount.CpuFrequency > 0)
        {
            Console.WriteLine($"Frequency:      {cpuCount.CpuFrequency / 1_000_000_000.0:F2} GHz");
        }

        if (cpuCount.CacheLineSize > 0)
        {
            Console.WriteLine($"Cache Line:     {cpuCount.CacheLineSize} bytes");
        }

        if (cpuCount.L2CacheSize > 0)
        {
            Console.WriteLine($"L2 Cache:       {cpuCount.L2CacheSize / 1024} KiB");
        }

        Console.WriteLine();

        // ロードアベレージ
        var load = CpuInfo.GetLoadAverage();
        Console.WriteLine("=== Load Average ===");
        Console.WriteLine($"1 min:  {load.Load1:F2}");
        Console.WriteLine($"5 min:  {load.Load5:F2}");
        Console.WriteLine($"15 min: {load.Load15:F2}");
        Console.WriteLine();

        // Per-CPU累積時間
        Console.WriteLine("=== Per-CPU Cumulative Times (ticks) ===");
        var perCpu = CpuInfo.GetPerCpuTimes();
        Console.WriteLine($"{"CPU",4} {"User",10} {"System",10} {"Idle",10} {"Nice",10} {"Total",10}");
        foreach (var cpu in perCpu)
        {
            var total = (ulong)cpu.User + cpu.System + cpu.Idle + cpu.Nice;
            Console.WriteLine($"{cpu.CpuNumber,4} {cpu.User,10} {cpu.System,10} {cpu.Idle,10} {cpu.Nice,10} {total,10}");
        }

        Console.WriteLine();

        // CPU使用率 (1秒間のデルタ)
        Console.WriteLine("=== CPU Usage (1 second) ===");
        Console.WriteLine("Measuring...");
        var prev = CpuInfo.GetCpuLoadTicks();
        Thread.Sleep(1000);
        var curr = CpuInfo.GetCpuLoadTicks();

        long totalUser = 0, totalSystem = 0, totalIdle = 0, totalNice = 0;
        for (var i = 0; i < prev.Length; i++)
        {
            totalUser += curr[i].User - prev[i].User;
            totalSystem += curr[i].System - prev[i].System;
            totalIdle += curr[i].Idle - prev[i].Idle;
            totalNice += curr[i].Nice - prev[i].Nice;
        }

        PrintUsage("Total", totalUser, totalSystem, totalIdle, totalNice);
        Console.WriteLine();

        for (var i = 0; i < prev.Length; i++)
        {
            PrintUsage(
                $"CPU {curr[i].CpuNumber}",
                curr[i].User - prev[i].User,
                curr[i].System - prev[i].System,
                curr[i].Idle - prev[i].Idle,
                curr[i].Nice - prev[i].Nice);
        }
    }

    private static void PrintUsage(string label, long user, long system, long idle, long nice)
    {
        var total = (double)(user + system + idle + nice);
        if (total == 0)
        {
            Console.WriteLine($"{label}: No activity");
            return;
        }

        Console.WriteLine($"{label}:");
        Console.WriteLine($"  User:   {100.0 * user / total,6:F2}%  ({user} ticks)");
        Console.WriteLine($"  System: {100.0 * system / total,6:F2}%  ({system} ticks)");
        Console.WriteLine($"  Idle:   {100.0 * idle / total,6:F2}%  ({idle} ticks)");
        Console.WriteLine($"  Nice:   {100.0 * nice / total,6:F2}%  ({nice} ticks)");
    }
}

// CPU使用率計算用のティック値
internal readonly record struct CpuLoadTicks(int CpuNumber, uint User, uint System, uint Idle, uint Nice);

// CPU数情報
internal sealed record CpuCountInfo
{
    // 論理CPU数 (hw.logicalcpu)
    public required int LogicalCpu { get; init; }

    // 物理CPU数 (hw.physicalcpu)
    public required int PhysicalCpu { get; init; }

    // CPU数 (hw.ncpu、通常はLogicalCpuと同値)
    public required int Ncpu { get; init; }

    // アクティブCPU数 (hw.activecpu、省電力モード等で変動する場合がある)
    public required int ActiveCpu { get; init; }

    // CPUブランド名 (machdep.cpu.brand_string、Apple Siliconでは取得不可の場合あり)
    public string? BrandString { get; init; }

    // CPU周波数(Hz) (hw.cpufrequency、Apple Siliconでは取得不可の場合あり)
    public long CpuFrequency { get; init; }

    // キャッシュラインサイズ(バイト) (hw.cachelinesize)
    public int CacheLineSize { get; init; }

    // L2キャッシュサイズ(バイト) (hw.l2cachesize、取得不可の場合あり)
    public long L2CacheSize { get; init; }
}

// ロードアベレージ
internal readonly record struct LoadAverage(double Load1, double Load5, double Load15);

internal static class CpuInfo
{
    // CPU使用率計算用のティック値を取得 (スナップショット)
    public static unsafe CpuLoadTicks[] GetCpuLoadTicks()
    {
        var host = mach_host_self();
        var result = host_processor_info(host, PROCESSOR_CPU_LOAD_INFO, out var processorCount, out var info, out var infoCnt);
        if (result != 0)
        {
            throw new InvalidOperationException($"host_processor_info failed: {result}");
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

            return ticks;
        }
        finally
        {
            _ = vm_deallocate(task_self_trap(), info, (IntPtr)(infoCnt * sizeof(int)));
        }
    }

    // コア毎のCPU累積時間を取得
    // GetCpuLoadTicksと同一データだが、用途の違いを明示するためのエントリーポイント
    // GetCpuLoadTicks: デルタ計算によるCPU使用率算出用
    // GetPerCpuTimes: コア毎の累積CPU時間の参照用
    public static CpuLoadTicks[] GetPerCpuTimes() => GetCpuLoadTicks();

    // CPU数情報を取得
    public static CpuCountInfo GetCpuCount()
    {
        return new CpuCountInfo
        {
            LogicalCpu = GetSysctlInt("hw.logicalcpu"),
            PhysicalCpu = GetSysctlInt("hw.physicalcpu"),
            Ncpu = GetSysctlInt("hw.ncpu"),
            ActiveCpu = GetSysctlInt("hw.activecpu"),
            BrandString = GetSysctlString("machdep.cpu.brand_string"),
            CpuFrequency = GetSysctlLong("hw.cpufrequency"),
            CacheLineSize = GetSysctlInt("hw.cachelinesize"),
            L2CacheSize = GetSysctlLong("hw.l2cachesize"),
        };
    }

    // ロードアベレージを取得
    public static unsafe LoadAverage GetLoadAverage()
    {
        var loadavg = stackalloc double[3];
        var count = getloadavg(loadavg, 3);
        return new LoadAverage(
            count >= 1 ? loadavg[0] : 0,
            count >= 2 ? loadavg[1] : 0,
            count >= 3 ? loadavg[2] : 0);
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
    // host_processor_info flavor (mach/processor_info.h)
    public const int PROCESSOR_CPU_LOAD_INFO = 2;

    // CPU state indices (mach/machine.h)
    public const int CPU_STATE_USER = 0;
    public const int CPU_STATE_SYSTEM = 1;
    public const int CPU_STATE_IDLE = 2;
    public const int CPU_STATE_NICE = 3;
    public const int CPU_STATE_MAX = 4;

    //------------------------------------------------------------------------
    // Mach
    //------------------------------------------------------------------------

    [DllImport("libSystem.dylib")]
    public static extern uint mach_host_self();

    [DllImport("libSystem.dylib")]
    public static extern uint task_self_trap();

    [DllImport("libSystem.dylib")]
    public static extern int host_processor_info(uint host, int flavor, out int processorCount, out IntPtr processorInfo, out int processorInfoCnt);

    [DllImport("libSystem.dylib")]
    public static extern int vm_deallocate(uint targetTask, IntPtr address, IntPtr size);

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

    [DllImport("libc")]
    public static extern unsafe int getloadavg(double* loadavg, int nelem);
}
