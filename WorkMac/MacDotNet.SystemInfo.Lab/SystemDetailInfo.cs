namespace MacDotNet.SystemInfo.Lab;

using System.Diagnostics;
using System.Runtime.InteropServices;

using static NativeMethods;

/// <summary>
/// モデル詳細情報
/// </summary>
public sealed record ModelInfo
{
    public string? ModelId { get; init; }
    public string? ModelName { get; init; }
    public int? Year { get; init; }
    public string? SerialNumber { get; init; }
}

/// <summary>
/// ディスプレイ情報
/// </summary>
public sealed record DisplayInfo
{
    public int DisplayId { get; init; }
    public string? Name { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public double RefreshRate { get; init; }
    public bool IsBuiltIn { get; init; }
    public bool IsMain { get; init; }
}

/// <summary>
/// メモリスロット情報
/// </summary>
public sealed record DimmSlotInfo
{
    public int? Bank { get; init; }
    public string? Channel { get; init; }
    public string? Type { get; init; }
    public string? Size { get; init; }
    public string? Speed { get; init; }
}

/// <summary>
/// コア周波数情報 (Apple Silicon)
/// </summary>
public sealed record CoreFrequencyInfo
{
    public int PerfLevel { get; init; }
    public string? Name { get; init; }
    public int[] Frequencies { get; init; } = [];
}

/// <summary>
/// システム詳細情報取得
/// </summary>
public static class SystemDetailInfo
{
    /// <summary>
    /// モデル詳細情報を取得
    /// </summary>
    public static ModelInfo GetModelInfo()
    {
        var modelId = GetSysctlString("hw.model");
        string? serialNumber = null;

        // シリアル番号はIOPlatformExpertDevice経由
        var matching = IOServiceMatching("IOPlatformExpertDevice");
        if (matching != nint.Zero)
        {
            var service = IOServiceGetMatchingService(0, matching);
            if (service != 0)
            {
                try
                {
                    var key = CFStringCreateWithCString(nint.Zero, "IOPlatformSerialNumber", kCFStringEncodingUTF8);
                    var value = IORegistryEntryCreateCFProperty(service, key, nint.Zero, 0);
                    CFRelease(key);

                    if (value != nint.Zero)
                    {
                        serialNumber = CfStringToManaged(value);
                        CFRelease(value);
                    }
                }
                finally
                {
                    IOObjectRelease(service);
                }
            }
        }

        // モデル名と年はsystem_profiler経由
        var (modelName, year) = GetModelNameAndYear(modelId);

        return new ModelInfo
        {
            ModelId = modelId,
            ModelName = modelName,
            Year = year,
            SerialNumber = serialNumber,
        };
    }

    private static (string? modelName, int? year) GetModelNameAndYear(string? modelId)
    {
        if (string.IsNullOrEmpty(modelId))
        {
            return (null, null);
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/sbin/system_profiler",
                Arguments = "SPHardwareDataType -json",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return (null, null);
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var modelName = ExtractJsonValue(output, "\"machine_model\"")
                            ?? ExtractJsonValue(output, "\"model_name\"");

            return (modelName, null);
        }
        catch
        {
            return (null, null);
        }
    }

    /// <summary>
    /// ディスプレイ情報を取得
    /// </summary>
    public static DisplayInfo[] GetDisplays()
    {
        var results = new List<DisplayInfo>();

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/sbin/system_profiler",
                Arguments = "SPDisplaysDataType -json",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return [];
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // 簡易JSONパース
            ParseDisplaysFromJson(output, results);
        }
        catch
        {
            // Ignore
        }

        return [.. results];
    }

    private static void ParseDisplaysFromJson(string json, List<DisplayInfo> results)
    {
        // 簡易実装: spdisplays_ndrvs 配列からディスプレイ情報を抽出
        var displayIndex = 0;
        var searchPos = 0;

        while (true)
        {
            var namePos = json.IndexOf("\"_name\"", searchPos, StringComparison.Ordinal);
            if (namePos < 0)
            {
                break;
            }

            var name = ExtractJsonValueAt(json, namePos);
            var resolution = ExtractJsonValueAt(json, json.IndexOf("\"_spdisplays_resolution\"", namePos, StringComparison.Ordinal));
            var main = json.IndexOf("\"spdisplays_main\"", namePos, StringComparison.Ordinal) > 0 &&
                       json.IndexOf("\"spdisplays_main\"", namePos, StringComparison.Ordinal) <
                       json.IndexOf("\"_name\"", namePos + 1, StringComparison.Ordinal);

            var width = 0;
            var height = 0;
            if (!string.IsNullOrEmpty(resolution))
            {
                var parts = resolution.Split('x', StringSplitOptions.TrimEntries);
                if (parts.Length >= 2)
                {
                    int.TryParse(parts[0].Split(' ')[0], out width);
                    int.TryParse(parts[1].Split(' ')[0], out height);
                }
            }

            results.Add(new DisplayInfo
            {
                DisplayId = displayIndex++,
                Name = name,
                Width = width,
                Height = height,
                IsMain = main,
            });

            searchPos = namePos + 1;
        }
    }

    /// <summary>
    /// メモリスロット情報を取得
    /// </summary>
    public static DimmSlotInfo[] GetDimmSlots()
    {
        var results = new List<DimmSlotInfo>();

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/sbin/system_profiler",
                Arguments = "SPMemoryDataType -json",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return [];
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // 簡易実装
            ParseDimmSlotsFromJson(output, results);
        }
        catch
        {
            // Ignore
        }

        return [.. results];
    }

    private static void ParseDimmSlotsFromJson(string json, List<DimmSlotInfo> results)
    {
        var searchPos = 0;
        while (true)
        {
            var dimmPos = json.IndexOf("\"dimm_size\"", searchPos, StringComparison.Ordinal);
            if (dimmPos < 0)
            {
                break;
            }

            var size = ExtractJsonValueAt(json, dimmPos);
            var type = ExtractJsonValueAt(json, json.IndexOf("\"dimm_type\"", dimmPos, StringComparison.Ordinal));
            var speed = ExtractJsonValueAt(json, json.IndexOf("\"dimm_speed\"", dimmPos, StringComparison.Ordinal));

            results.Add(new DimmSlotInfo
            {
                Size = size,
                Type = type,
                Speed = speed,
            });

            searchPos = dimmPos + 1;
        }
    }

    /// <summary>
    /// E-Core/P-Core周波数情報を取得 (Apple Silicon)
    /// </summary>
    public static CoreFrequencyInfo[] GetCoreFrequencies()
    {
        var results = new List<CoreFrequencyInfo>();

        var nperflevels = GetSysctlInt("hw.nperflevels");
        if (nperflevels <= 0)
        {
            return [];
        }

        for (var level = 0; level < nperflevels; level++)
        {
            var name = GetSysctlString($"hw.perflevel{level}.name");
            var freqs = new List<int>();

            // 周波数リストを取得 (hw.perflevel{n}.{freq_list}等)
            // 簡易実装: cpufreq_maxのみ取得
            var maxFreq = GetSysctlInt($"hw.perflevel{level}.cpufreq_max");
            if (maxFreq > 0)
            {
                freqs.Add(maxFreq);
            }

            results.Add(new CoreFrequencyInfo
            {
                PerfLevel = level,
                Name = name,
                Frequencies = [.. freqs],
            });
        }

        return [.. results];
    }

    private static string? ExtractJsonValue(string json, string key)
    {
        var keyIndex = json.IndexOf(key, StringComparison.Ordinal);
        if (keyIndex < 0)
        {
            return null;
        }

        return ExtractJsonValueAt(json, keyIndex);
    }

    private static string? ExtractJsonValueAt(string json, int keyIndex)
    {
        if (keyIndex < 0)
        {
            return null;
        }

        var colonIndex = json.IndexOf(':', keyIndex);
        if (colonIndex < 0)
        {
            return null;
        }

        var valueStart = json.IndexOf('"', colonIndex);
        if (valueStart < 0)
        {
            return null;
        }

        var valueEnd = json.IndexOf('"', valueStart + 1);
        if (valueEnd < 0)
        {
            return null;
        }

        return json.Substring(valueStart + 1, valueEnd - valueStart - 1);
    }
}
