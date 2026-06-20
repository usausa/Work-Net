namespace AgentSampleCore;

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

/// <summary>
/// 各サンプルで共有する「PC情報取得ツール」群。
/// 通常の C# メソッドに <see cref="DescriptionAttribute"/> を付けるだけでツールになる。
/// ツールの呼び出し・引数の解釈・結果の取り込みはフレームワークが自動で行う。
/// ここでは BCL のみで取得できる、クロスプラットフォームで読み取り専用の情報を返す。
/// </summary>
public static class PcTools
{
    private const double BytesPerGb = 1024.0 * 1024 * 1024;
    private const double BytesPerMb = 1024.0 * 1024;

    /// <summary>OS・マシン名・ユーザー名・CPUコア数・.NETバージョン・稼働時間などの基本情報。</summary>
    [Description("OS・マシン名・ユーザー名・CPUコア数・.NETバージョン・稼働時間など、このPCの基本情報を取得します。")]
    public static string GetSystemInfo()
        => string.Join(
            Environment.NewLine,
            [
                $"OS              : {RuntimeInformation.OSDescription}",
                $"アーキテクチャ  : {RuntimeInformation.OSArchitecture}",
                $"マシン名        : {Environment.MachineName}",
                $"ユーザー名      : {Environment.UserName}",
                $"論理プロセッサ数: {Environment.ProcessorCount}",
                $".NET ランタイム : {RuntimeInformation.FrameworkDescription}",
                $"稼働時間        : {TimeSpan.FromMilliseconds(Environment.TickCount64):d\\.hh\\:mm\\:ss}",
            ]);

    /// <summary>メモリ(RAM)の総量とこのプロセスのGCヒープ使用量。</summary>
    [Description("このPCのメモリ(RAM)の総量とプロセスの使用量を取得します。")]
    public static string GetMemoryInfo()
    {
        var gc = GC.GetGCMemoryInfo();
        var totalGb = gc.TotalAvailableMemoryBytes / BytesPerGb;
        var usedMb = GC.GetTotalMemory(forceFullCollection: false) / BytesPerMb;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"利用可能メモリ(総量): {totalGb:F1} GB / このプロセスのGCヒープ使用量: {usedMb:F1} MB");
    }

    /// <summary>ドライブごとの空き容量・総容量。</summary>
    [Description("このPCのドライブごとの空き容量・総容量を取得します。")]
    public static string GetDriveInfo()
    {
        var lines = new List<string>();
        foreach (var d in DriveInfo.GetDrives())
        {
            if (!d.IsReady)
            {
                continue;
            }

            var freeGb = d.AvailableFreeSpace / BytesPerGb;
            var totalGb = d.TotalSize / BytesPerGb;
            lines.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"{d.Name} [{d.DriveType}] 空き {freeGb:F1} GB / 全体 {totalGb:F1} GB"));
        }

        return lines.Count > 0
            ? string.Join(Environment.NewLine, lines)
            : "準備済みのドライブが見つかりませんでした。";
    }

    /// <summary>
    /// メモリ使用量(ワーキングセット)が多い順のプロセス一覧。
    /// 引数つきツールの例。<paramref name="topCount"/> はモデルが文脈から決めて渡す。
    /// </summary>
    /// <param name="topCount">取得する上位プロセス数(1〜20)。</param>
    [Description("メモリ使用量(ワーキングセット)が多い順に、上位のプロセスを取得します。")]
    public static string GetTopProcesses(
        [Description("取得する上位プロセス数(1〜20)。")] int topCount = 5)
    {
        var count = Math.Clamp(topCount, 1, 20);

        var processes = Process.GetProcesses();
        try
        {
            var lines = processes
                .Select(static p => (Name: SafeProcessName(p), WorkingSet: SafeWorkingSet(p)))
                .OrderByDescending(static x => x.WorkingSet)
                .Take(count)
                .Select(static (x, i) => string.Create(
                    CultureInfo.InvariantCulture,
                    $"{i + 1,2}. {x.Name} : {x.WorkingSet / BytesPerMb:F1} MB"))
                .ToList();

            return string.Join(Environment.NewLine, lines);
        }
        finally
        {
            foreach (var p in processes)
            {
                p.Dispose();
            }
        }

        static string SafeProcessName(Process p)
        {
            try
            {
                return p.ProcessName;
            }
            catch (InvalidOperationException)
            {
                return "(unknown)"; // 取得前にプロセスが終了していた場合
            }
        }

        static long SafeWorkingSet(Process p)
        {
            try
            {
                return p.WorkingSet64;
            }
            catch (InvalidOperationException)
            {
                return 0L;
            }
        }
    }
}
