namespace MacDotNet.SystemInfo.Lab;

using static NativeMethods;

/// <summary>
/// モデル詳細情報
/// </summary>
public sealed record ModelInfo
{
    public string? ModelId { get; init; }
    public string? SerialNumber { get; init; }
}

/// <summary>
/// コア周波数情報 (Apple Silicon)
/// </summary>
public sealed record CoreFrequencyInfo
{
    public int PerfLevel { get; init; }
    public string? Name { get; init; }
    public int MaxFrequency { get; init; }
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

        return new ModelInfo
        {
            ModelId = modelId,
            SerialNumber = serialNumber,
        };
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
            var maxFreq = GetSysctlInt($"hw.perflevel{level}.cpufreq_max");

            results.Add(new CoreFrequencyInfo
            {
                PerfLevel = level,
                Name = name,
                MaxFrequency = maxFreq,
            });
        }

        return [.. results];
    }
}
