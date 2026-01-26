using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RaspberryPiMonitor;

/// <summary>
/// Raspberry Piのハードウェアモニタリング情報を取得するクラス
/// </summary>
public partial class HardwareMonitor
{
    private const string VcGenCmd = "vcgencmd";

    /// <summary>
    /// 温度情報を取得
    /// </summary>
    public async Task<double?> GetTemperatureAsync()
    {
        var output = await ExecuteCommandAsync("measure_temp");
        var match = TemperatureRegex().Match(output);
        return match.Success ? double.Parse(match.Groups[1].Value) : null;
    }

    /// <summary>
    /// クロック周波数を取得
    /// </summary>
    public async Task<long?> GetClockFrequencyAsync(string clock)
    {
        var output = await ExecuteCommandAsync($"measure_clock {clock}");
        var match = FrequencyRegex().Match(output);
        return match.Success ? long.Parse(match.Groups[1].Value) : null;
    }

    /// <summary>
    /// 電圧を取得
    /// </summary>
    public async Task<double?> GetVoltageAsync(string component)
    {
        var output = await ExecuteCommandAsync($"measure_volts {component}");
        var match = VoltageRegex().Match(output);
        return match.Success ? double.Parse(match.Groups[1].Value) : null;
    }

    /// <summary>
    /// スロットル状態を取得
    /// </summary>
    public async Task<string?> GetThrottledAsync()
    {
        var output = await ExecuteCommandAsync("get_throttled");
        var match = ThrottledRegex().Match(output);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// メモリ割り当てを取得
    /// </summary>
    public async Task<int?> GetMemoryAsync(string component)
    {
        var output = await ExecuteCommandAsync($"get_mem {component}");
        // 出力例: "arm=448M" または "gpu=64M"
        var match = MemoryRegex().Match(output);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    /// <summary>
    /// 設定値を取得
    /// </summary>
    public async Task<Dictionary<string, string>> GetConfigAsync(string configType = "int")
    {
        var output = await ExecuteCommandAsync($"get_config {configType}");
        var config = new Dictionary<string, string>();

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                config[parts[0].Trim()] = parts[1].Trim();
            }
        }

        return config;
    }

    /// <summary>
    /// すべてのモニタリング情報を取得
    /// </summary>
    public async Task<HardwareInfo> GetAllInfoAsync()
    {
        var info = new HardwareInfo
        {
            Temperature = await GetTemperatureAsync(),
            ArmFrequency = await GetClockFrequencyAsync("arm"),
            CoreFrequency = await GetClockFrequencyAsync("core"),
            CoreVoltage = await GetVoltageAsync("core"),
            SdramCVoltage = await GetVoltageAsync("sdram_c"),
            SdramIVoltage = await GetVoltageAsync("sdram_i"),
            SdramPVoltage = await GetVoltageAsync("sdram_p"),
            Throttled = await GetThrottledAsync(),
            ArmMemory = await GetMemoryAsync("arm"),
            GpuMemory = await GetMemoryAsync("gpu"),
            Config = await GetConfigAsync()
        };

        return info;
    }

    /// <summary>
    /// vcgencmdコマンドを実行
    /// </summary>
    private static async Task<string> ExecuteCommandAsync(string arguments)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = VcGenCmd,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
                return string.Empty;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing vcgencmd: {ex.Message}");
            return string.Empty;
        }
    }

    // 正規表現パターン（C# 11のソース生成機能を使用）
    [GeneratedRegex(@"temp=([\d.]+)")]
    private static partial Regex TemperatureRegex();

    [GeneratedRegex(@"frequency\(\d+\)=(\d+)")]
    private static partial Regex FrequencyRegex();

    [GeneratedRegex(@"volt=([\d.]+)V")]
    private static partial Regex VoltageRegex();

    [GeneratedRegex(@"throttled=(.+)")]
    private static partial Regex ThrottledRegex();

    [GeneratedRegex(@"=(\d+)M")]  // 修正: "arm=448M" から "448" を抽出
    private static partial Regex MemoryRegex();
}

/// <summary>
/// ハードウェア情報を格納するレコード
/// </summary>
public record HardwareInfo
{
    public double? Temperature { get; init; }
    public long? ArmFrequency { get; init; }
    public long? CoreFrequency { get; init; }
    public double? CoreVoltage { get; init; }
    public double? SdramCVoltage { get; init; }
    public double? SdramIVoltage { get; init; }
    public double? SdramPVoltage { get; init; }
    public string? Throttled { get; init; }
    public int? ArmMemory { get; init; }
    public int? GpuMemory { get; init; }
    public Dictionary<string, string>? Config { get; init; }

    public override string ToString()
    {
        return $"""
            Temperature: {Temperature}°C
            ARM Frequency: {ArmFrequency / 1_000_000.0:F2} MHz
            Core Frequency: {CoreFrequency / 1_000_000.0:F2} MHz
            Core Voltage: {CoreVoltage}V
            SDRAM_C Voltage: {SdramCVoltage}V
            SDRAM_I Voltage: {SdramIVoltage}V
            SDRAM_P Voltage: {SdramPVoltage}V
            Throttled: {Throttled}
            ARM Memory: {ArmMemory}MB
            GPU Memory: {GpuMemory}MB
            """;
    }
}

/// <summary>
/// 使用例
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        var monitor = new HardwareMonitor();

        try
        {
            // 個別に取得
            Console.WriteLine("=== 個別情報取得 ===");
            var temp = await monitor.GetTemperatureAsync();
            Console.WriteLine($"Temperature: {temp}°C");

            var armFreq = await monitor.GetClockFrequencyAsync("arm");
            Console.WriteLine($"ARM Frequency: {armFreq} Hz ({armFreq / 1_000_000.0:F2} MHz)");

            var coreVolt = await monitor.GetVoltageAsync("core");
            Console.WriteLine($"Core Voltage: {coreVolt}V");

            var armMem = await monitor.GetMemoryAsync("arm");
            Console.WriteLine($"ARM Memory: {armMem}MB");

            var gpuMem = await monitor.GetMemoryAsync("gpu");
            Console.WriteLine($"GPU Memory: {gpuMem}MB");

            // すべての情報を一度に取得
            Console.WriteLine("\n=== 全情報取得 ===");
            var info = await monitor.GetAllInfoAsync();
            Console.WriteLine(info);

            // 設定情報の表示
            Console.WriteLine("\n=== 主要設定値 ===");
            if (info.Config != null)
            {
                var importantKeys = new[]
                {
                    "arm_freq", "arm_freq_min", "core_freq", "gpu_freq",
                    "total_mem", "arm_64bit", "over_voltage_avs", "sdram_freq"
                };

                foreach (var key in importantKeys)
                {
                    if (info.Config.TryGetValue(key, out var value))
                    {
                        Console.WriteLine($"{key}: {value}");
                    }
                }
            }

            // スロットル状態の詳細表示
            Console.WriteLine("\n=== スロットル状態 ===");
            if (info.Throttled != null)
            {
                var throttledValue = Convert.ToInt32(info.Throttled, 16);
                Console.WriteLine($"Throttled: {info.Throttled}");
                Console.WriteLine($"  Under-voltage detected: {(throttledValue & 0x1) != 0}");
                Console.WriteLine($"  ARM frequency capped: {(throttledValue & 0x2) != 0}");
                Console.WriteLine($"  Currently throttled: {(throttledValue & 0x4) != 0}");
                Console.WriteLine($"  Soft temperature limit: {(throttledValue & 0x8) != 0}");
                Console.WriteLine($"  Under-voltage occurred: {(throttledValue & 0x10000) != 0}");
                Console.WriteLine($"  ARM freq cap occurred: {(throttledValue & 0x20000) != 0}");
                Console.WriteLine($"  Throttling occurred: {(throttledValue & 0x40000) != 0}");
                Console.WriteLine($"  Soft temp limit occurred: {(throttledValue & 0x80000) != 0}");
            }

            // 継続的なモニタリング例
            Console.WriteLine("\n=== 継続モニタリング (5秒間隔、3回) ===");
            for (int i = 0; i < 3; i++)
            {
                var currentTemp = await monitor.GetTemperatureAsync();
                var currentFreq = await monitor.GetClockFrequencyAsync("arm");
                var currentVolt = await monitor.GetVoltageAsync("core");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Temp: {currentTemp}°C, Freq: {currentFreq / 1_000_000.0:F2} MHz, Volt: {currentVolt}V");

                if (i < 2) // 最後の反復では待たない
                    await Task.Delay(5000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
