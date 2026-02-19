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
/// コアクラスタ情報 (Apple Silicon)
/// </summary>
public sealed record CoreClusterInfo
{
    public int PerfLevel { get; init; }
    public string? Name { get; init; }
    public int LogicalCpu { get; init; }
    public int PhysicalCpu { get; init; }
    public long L1ICacheSize { get; init; }
    public long L1DCacheSize { get; init; }
    public long L2CacheSize { get; init; }
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
        if (matching != IntPtr.Zero)
        {
            var service = IOServiceGetMatchingService(0, matching);
            if (service != 0)
            {
                try
                {
                    var key = CFStringCreateWithCString(IntPtr.Zero, "IOPlatformSerialNumber", kCFStringEncodingUTF8);
                    var value = IORegistryEntryCreateCFProperty(service, key, IntPtr.Zero, 0);
                    CFRelease(key);

                    if (value != IntPtr.Zero)
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
    /// E-Core/P-Coreクラスタ情報を取得 (Apple Silicon)
    /// </summary>
    public static CoreClusterInfo[] GetCoreClusterInfo()
    {
        var results = new List<CoreClusterInfo>();

        var nperflevels = GetSysctlInt("hw.nperflevels");
        if (nperflevels <= 0)
        {
            return [];
        }

        for (var level = 0; level < nperflevels; level++)
        {
            var prefix = $"hw.perflevel{level}";

            results.Add(new CoreClusterInfo
            {
                PerfLevel = level,
                Name = GetSysctlString($"{prefix}.name"),
                LogicalCpu = GetSysctlInt($"{prefix}.logicalcpu"),
                PhysicalCpu = GetSysctlInt($"{prefix}.physicalcpu"),
                L1ICacheSize = GetSysctlLong($"{prefix}.l1icachesize"),
                L1DCacheSize = GetSysctlLong($"{prefix}.l1dcachesize"),
                L2CacheSize = GetSysctlLong($"{prefix}.l2cachesize"),
            });
        }

        return [.. results];
    }
}
