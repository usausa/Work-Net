namespace WorkCpu;

using System.Runtime.InteropServices;

using static WorkCpu.NativeMethods;

internal static class Program
{
    public static void Main()
    {
        Console.WriteLine("Measuring CPU usage for 1 second...");
        Console.WriteLine();

        var prev = CpuInfo.GetCpuLoadTicks();
        Thread.Sleep(1000);
        var curr = CpuInfo.GetCpuLoadTicks();

        // Total
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

        // Per-core
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

internal readonly record struct CpuLoadTicks(int CpuNumber, uint User, uint System, uint Idle, uint Nice);

internal static class CpuInfo
{
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
            _ = vm_deallocate(task_self_trap(), info, (nint)(infoCnt * sizeof(int)));
        }
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
    public const int PROCESSOR_CPU_LOAD_INFO = 2;

    public const int CPU_STATE_USER = 0;
    public const int CPU_STATE_SYSTEM = 1;
    public const int CPU_STATE_IDLE = 2;
    public const int CPU_STATE_NICE = 3;
    public const int CPU_STATE_MAX = 4;

    [DllImport("libSystem.dylib")]
    public static extern uint mach_host_self();

    [DllImport("libSystem.dylib")]
    public static extern uint task_self_trap();

    [DllImport("libSystem.dylib")]
    public static extern int host_processor_info(uint host, int flavor, out int processorCount, out nint processorInfo, out int processorInfoCnt);

    [DllImport("libSystem.dylib")]
    public static extern int vm_deallocate(uint targetTask, nint address, nint size);
}
