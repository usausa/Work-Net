namespace MacDotNet.SystemInfo.Lab;

using static NativeMethods;

/// <summary>
/// Apple Silicon電力センサー情報
/// </summary>
public sealed class AppleSiliconPowerInfo
{
    private nint channels;
    private nint subscription;

    private double prevCpuEnergy;
    private double prevGpuEnergy;
    private double prevAneEnergy;
    private double prevRamEnergy;

    /// <summary>
    /// CPU電力 (W)
    /// </summary>
    public double CpuPower { get; private set; }

    /// <summary>
    /// GPU電力 (W)
    /// </summary>
    public double GpuPower { get; private set; }

    /// <summary>
    /// ANE (Neural Engine) 電力 (W)
    /// </summary>
    public double AnePower { get; private set; }

    /// <summary>
    /// RAM電力 (W)
    /// </summary>
    public double RamPower { get; private set; }

    /// <summary>
    /// 合計電力 (W)
    /// </summary>
    public double TotalPower => CpuPower + GpuPower + AnePower + RamPower;

    /// <summary>
    /// サポートされているか (Apple Silicon)
    /// </summary>
    public bool Supported { get; }

    private AppleSiliconPowerInfo()
    {
        Supported = InitializeReporting();
    }

    public static AppleSiliconPowerInfo Create() => new();

    private bool InitializeReporting()
    {
        // Apple Siliconのみ対応
        if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture != System.Runtime.InteropServices.Architecture.Arm64)
        {
            return false;
        }

        channels = GetEnergyModelChannels();
        if (channels == nint.Zero)
        {
            return false;
        }

        subscription = IOReportCreateSubscription(nint.Zero, channels, out _, 0, nint.Zero);
        return subscription != nint.Zero;
    }

    public bool Update()
    {
        if (!Supported || subscription == nint.Zero)
        {
            return false;
        }

        var samples = IOReportCreateSamples(subscription, channels, nint.Zero);
        if (samples == nint.Zero)
        {
            return false;
        }

        try
        {
            var channelsKey = CFStringCreateWithCString(nint.Zero, "IOReportChannels", kCFStringEncodingUTF8);
            var channelsArray = CFDictionaryGetValue(samples, channelsKey);
            CFRelease(channelsKey);

            if (channelsArray == nint.Zero || CFGetTypeID(channelsArray) != CFArrayGetTypeID())
            {
                return false;
            }

            double cpuEnergy = 0, gpuEnergy = 0, aneEnergy = 0, ramEnergy = 0;

            var count = CFArrayGetCount(channelsArray);
            for (var i = (nint)0; i < count; i++)
            {
                var item = CFArrayGetValueAtIndex(channelsArray, i);
                if (item == nint.Zero)
                {
                    continue;
                }

                var groupPtr = IOReportChannelGetGroup(item);
                var group = groupPtr != nint.Zero ? CfStringToManaged(groupPtr) : null;
                if (group != "Energy Model")
                {
                    continue;
                }

                var channelNamePtr = IOReportChannelGetChannelName(item);
                var channelName = channelNamePtr != nint.Zero ? CfStringToManaged(channelNamePtr) : null;
                if (string.IsNullOrEmpty(channelName))
                {
                    continue;
                }

                var unitPtr = IOReportChannelGetUnitLabel(item);
                var unit = unitPtr != nint.Zero ? CfStringToManaged(unitPtr) : null;

                var value = (double)IOReportSimpleGetIntegerValue(item, 0);
                var power = ConvertToPower(value, unit);

                if (channelName.EndsWith("CPU Energy", StringComparison.Ordinal))
                {
                    cpuEnergy = power;
                }
                else if (channelName.EndsWith("GPU Energy", StringComparison.Ordinal))
                {
                    gpuEnergy = power;
                }
                else if (channelName.StartsWith("ANE", StringComparison.Ordinal))
                {
                    aneEnergy = power;
                }
                else if (channelName.StartsWith("DRAM", StringComparison.Ordinal))
                {
                    ramEnergy = power;
                }
            }

            // 差分計算
            if (prevCpuEnergy > 0)
            {
                CpuPower = cpuEnergy - prevCpuEnergy;
                GpuPower = gpuEnergy - prevGpuEnergy;
                AnePower = aneEnergy - prevAneEnergy;
                RamPower = ramEnergy - prevRamEnergy;
            }

            prevCpuEnergy = cpuEnergy;
            prevGpuEnergy = gpuEnergy;
            prevAneEnergy = aneEnergy;
            prevRamEnergy = ramEnergy;

            return true;
        }
        finally
        {
            CFRelease(samples);
        }
    }

    private static nint GetEnergyModelChannels()
    {
        var channel = IOReportCopyChannelsInGroup("Energy Model", null, 0, 0, 0);
        if (channel == nint.Zero)
        {
            return nint.Zero;
        }

        var count = 1; // 簡易実装
        return CFDictionaryCreateMutableCopy(nint.Zero, count, channel);
    }

    private static double ConvertToPower(double value, string? unit)
    {
        // 単位に応じて変換
        return unit switch
        {
            "mJ" => value / 1000.0, // mJ to J (W per second)
            "uJ" => value / 1_000_000.0,
            "nJ" => value / 1_000_000_000.0,
            _ => value / 1_000_000_000.0, // Default to nJ
        };
    }
}

/// <summary>
/// HIDセンサー情報 (Apple Silicon)
/// </summary>
public sealed record HidSensorEntry
{
    public required string Key { get; init; }
    public required string Type { get; init; }
    public required double Value { get; init; }
}

/// <summary>
/// センサー詳細情報取得
/// </summary>
public static class SensorDetailInfo
{
    /// <summary>
    /// 平均/最高CPU温度を計算
    /// </summary>
    public static (double average, double max) GetCpuTemperatureStats(IEnumerable<(string key, double value)> sensors)
    {
        var cpuTemps = sensors
            .Where(static s => s.key.Contains("CPU", StringComparison.OrdinalIgnoreCase) ||
                               s.key.StartsWith("TC", StringComparison.Ordinal) ||
                               s.key.Contains("pACC", StringComparison.Ordinal) ||
                               s.key.Contains("eACC", StringComparison.Ordinal))
            .Select(static s => s.value)
            .Where(static v => v > 0 && v < 120)
            .ToList();

        if (cpuTemps.Count == 0)
        {
            return (0, 0);
        }

        return (cpuTemps.Average(), cpuTemps.Max());
    }

    /// <summary>
    /// 平均/最高GPU温度を計算
    /// </summary>
    public static (double average, double max) GetGpuTemperatureStats(IEnumerable<(string key, double value)> sensors)
    {
        var gpuTemps = sensors
            .Where(static s => s.key.Contains("GPU", StringComparison.OrdinalIgnoreCase) ||
                               s.key.StartsWith("TG", StringComparison.Ordinal))
            .Select(static s => s.value)
            .Where(static v => v > 0 && v < 120)
            .ToList();

        if (gpuTemps.Count == 0)
        {
            return (0, 0);
        }

        return (gpuTemps.Average(), gpuTemps.Max());
    }

    /// <summary>
    /// 電流センサー値を取得 (SMCキー "I*")
    /// </summary>
    public static double? GetCurrentSensor(string key)
    {
        if (!key.StartsWith('I'))
        {
            return null;
        }

        return SmcHelper.ReadSmcValue(key);
    }

    /// <summary>
    /// ファンモードを取得
    /// </summary>
    public static (int fanCount, (int id, string mode)[] fanModes) GetFanModes()
    {
        var fanCount = (int)(SmcHelper.ReadSmcValue("FNum") ?? 0);
        if (fanCount <= 0)
        {
            return (0, []);
        }

        var modes = new (int id, string mode)[fanCount];
        var fsValue = (int)(SmcHelper.ReadSmcValue("FS! ") ?? 0);

        for (var i = 0; i < fanCount; i++)
        {
            var modeValue = SmcHelper.ReadSmcValue($"F{i}Md");
            string mode;

            if (modeValue is not null)
            {
                mode = (int)modeValue switch
                {
                    0 => "Automatic",
                    1 => "Forced",
                    _ => "Unknown",
                };
            }
            else
            {
                // FS! から推定
                mode = fsValue switch
                {
                    0 => "Automatic",
                    3 => "Forced",
                    1 when i == 0 => "Forced",
                    2 when i == 1 => "Forced",
                    _ => "Automatic",
                };
            }

            modes[i] = (i, mode);
        }

        return (fanCount, modes);
    }

    /// <summary>
    /// 最速ファンの情報を取得
    /// </summary>
    public static (int id, double rpm, double minRpm, double maxRpm)? GetFastestFan()
    {
        var fanCount = (int)(SmcHelper.ReadSmcValue("FNum") ?? 0);
        if (fanCount <= 1)
        {
            return null;
        }

        var fastest = (id: -1, rpm: 0.0, minRpm: 0.0, maxRpm: 0.0);
        for (var i = 0; i < fanCount; i++)
        {
            var rpm = SmcHelper.ReadSmcValue($"F{i}Ac") ?? 0;
            if (rpm > fastest.rpm)
            {
                fastest = (i, rpm, SmcHelper.ReadSmcValue($"F{i}Mn") ?? 0, SmcHelper.ReadSmcValue($"F{i}Mx") ?? 0);
            }
        }

        return fastest.id >= 0 ? fastest : null;
    }

    /// <summary>
    /// 総消費電力 (PSTR) を取得
    /// </summary>
    public static double? GetTotalSystemPower()
    {
        return SmcHelper.ReadSmcValue("PSTR");
    }
}
