namespace TuiAgentSampleCore;

using System.Globalization;
using System.Runtime.InteropServices;

/// <summary>
/// ツール呼び出しの結果を「それらしく」見せるため、BCL だけで取得できる実 PC 情報を返す。
/// </summary>
internal static class SystemProbe
{
    public static string SystemInfo() =>
        string.Join(
            Environment.NewLine,
            [
                $"os            : {RuntimeInformation.OSDescription}",
                $"arch          : {RuntimeInformation.OSArchitecture}",
                $"machine       : {Environment.MachineName}",
                $"user          : {Environment.UserName}",
                $"logical_cpus  : {Environment.ProcessorCount}",
                $"runtime       : {RuntimeInformation.FrameworkDescription}",
            ]);

    public static string MemoryInfo()
    {
        var gc = GC.GetGCMemoryInfo();
        var totalGb = gc.TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024);
        var usedMb = GC.GetTotalMemory(forceFullCollection: false) / (1024.0 * 1024);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"total_ram_gb  : {totalGb:F1}{Environment.NewLine}gc_heap_mb    : {usedMb:F1}");
    }

    public static int ProcessorCount => Environment.ProcessorCount;

    public static string MachineName => Environment.MachineName;

    public static double TotalMemoryGigabytes =>
        GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024);
}
