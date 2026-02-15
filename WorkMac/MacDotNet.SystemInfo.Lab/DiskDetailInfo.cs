namespace MacDotNet.SystemInfo.Lab;

using System.Runtime.InteropServices;

using static NativeMethods;

/// <summary>
/// NVMe SMART情報
/// </summary>
public sealed record DiskSmartEntry
{
    public required string BsdName { get; init; }

    /// <summary>
    /// 温度 (℃)
    /// </summary>
    public int? Temperature { get; init; }

    /// <summary>
    /// 寿命 (%)
    /// </summary>
    public int? Life { get; init; }

    /// <summary>
    /// 総読み込み量 (bytes)
    /// </summary>
    public long? TotalRead { get; init; }

    /// <summary>
    /// 総書き込み量 (bytes)
    /// </summary>
    public long? TotalWritten { get; init; }

    /// <summary>
    /// 電源サイクル数
    /// </summary>
    public int? PowerCycles { get; init; }

    /// <summary>
    /// 電源オン時間 (hours)
    /// </summary>
    public int? PowerOnHours { get; init; }
}

/// <summary>
/// ディスクI/O統計
/// </summary>
public sealed record DiskIoStats
{
    public required string BsdName { get; init; }
    public long ReadBytes { get; init; }
    public long WriteBytes { get; init; }
    public long ReadOperations { get; init; }
    public long WriteOperations { get; init; }
}

/// <summary>
/// ディスク詳細情報取得
/// </summary>
public static class DiskDetailInfo
{
    // NVMe SMART UUIDs (IOKit plugin)
    private static readonly Guid KIONVMeSMARTUserClientTypeID = new("AA0FA6F9-C2D6-457F-B10B-59A13253292F");
    private static readonly Guid KIONVMeSMARTInterfaceID = new("CCD1DB19-FD9A-4DAF-BF95-12454B230AB6");

    /// <summary>
    /// NVMe SMART情報を取得 (ディスクごと)
    /// </summary>
    public static DiskSmartEntry? GetSmartInfo(string bsdName)
    {
        // SMART情報取得にはIONVMeSMARTInterfaceを使用
        // これは複雑なプラグインインターフェースを必要とするため、
        // 簡易実装としてIORegistryから一部情報を取得

        var matching = IOServiceMatching("IOBlockStorageDevice");
        if (matching == nint.Zero)
        {
            return null;
        }

        var iterator = nint.Zero;
        if (IOServiceGetMatchingServices(0, matching, ref iterator) != 0)
        {
            return null;
        }

        try
        {
            uint service;
            while ((service = IOIteratorNext(iterator)) != 0)
            {
                try
                {
                    // BSD名を確認
                    var nameKey = CFStringCreateWithCString(nint.Zero, "BSD Name", kCFStringEncodingUTF8);
                    var namePtr = IORegistryEntryCreateCFProperty(service, nameKey, nint.Zero, 0);
                    CFRelease(nameKey);

                    string? currentBsdName = null;
                    if (namePtr != nint.Zero)
                    {
                        currentBsdName = CfStringToManaged(namePtr);
                        CFRelease(namePtr);
                    }

                    if (currentBsdName != bsdName)
                    {
                        continue;
                    }

                    // NVMe SMART対応確認
                    var smartCapableKey = CFStringCreateWithCString(nint.Zero, "NVMe SMART Capable", kCFStringEncodingUTF8);
                    var smartCapablePtr = IORegistryEntryCreateCFProperty(service, smartCapableKey, nint.Zero, 0);
                    CFRelease(smartCapableKey);

                    if (smartCapablePtr == nint.Zero)
                    {
                        continue;
                    }

                    var smartCapable = CFBooleanGetValue(smartCapablePtr);
                    CFRelease(smartCapablePtr);

                    if (!smartCapable)
                    {
                        continue;
                    }

                    // SMART情報を読み取り（プラグイン経由が必要なため、ここでは基本情報のみ）
                    // 完全な実装にはIOCreatePlugInInterfaceForService等が必要
                    return new DiskSmartEntry
                    {
                        BsdName = bsdName,
                        // 実際のSMART値取得には追加実装が必要
                    };
                }
                finally
                {
                    IOObjectRelease(service);
                }
            }
        }
        finally
        {
            IOObjectRelease((uint)iterator);
        }

        return null;
    }

    /// <summary>
    /// ディスクI/O統計を取得
    /// </summary>
    public static DiskIoStats[] GetDiskIoStats()
    {
        var results = new List<DiskIoStats>();

        var matching = IOServiceMatching("IOBlockStorageDriver");
        if (matching == nint.Zero)
        {
            return [];
        }

        var iterator = nint.Zero;
        if (IOServiceGetMatchingServices(0, matching, ref iterator) != 0)
        {
            return [];
        }

        try
        {
            uint service;
            while ((service = IOIteratorNext(iterator)) != 0)
            {
                try
                {
                    if (IORegistryEntryCreateCFProperties(service, out var propsPtr, nint.Zero, 0) != 0)
                    {
                        continue;
                    }

                    if (propsPtr == nint.Zero)
                    {
                        continue;
                    }

                    try
                    {
                        var statsKey = CFStringCreateWithCString(nint.Zero, "Statistics", kCFStringEncodingUTF8);
                        var statsPtr = CFDictionaryGetValue(propsPtr, statsKey);
                        CFRelease(statsKey);

                        if (statsPtr == nint.Zero)
                        {
                            continue;
                        }

                        var readBytes = GetDictionaryLongValue(statsPtr, "Bytes (Read)");
                        var writeBytes = GetDictionaryLongValue(statsPtr, "Bytes (Write)");
                        var readOps = GetDictionaryLongValue(statsPtr, "Operations (Read)");
                        var writeOps = GetDictionaryLongValue(statsPtr, "Operations (Write)");

                        results.Add(new DiskIoStats
                        {
                            BsdName = $"disk{results.Count}",
                            ReadBytes = readBytes,
                            WriteBytes = writeBytes,
                            ReadOperations = readOps,
                            WriteOperations = writeOps,
                        });
                    }
                    finally
                    {
                        CFRelease(propsPtr);
                    }
                }
                finally
                {
                    IOObjectRelease(service);
                }
            }
        }
        finally
        {
            IOObjectRelease((uint)iterator);
        }

        return [.. results];
    }

    private static long GetDictionaryLongValue(nint dict, string keyName)
    {
        var key = CFStringCreateWithCString(nint.Zero, keyName, kCFStringEncodingUTF8);
        var value = CFDictionaryGetValue(dict, key);
        CFRelease(key);

        if (value != nint.Zero && CFGetTypeID(value) == CFNumberGetTypeID())
        {
            long result = 0;
            if (CFNumberGetValue(value, kCFNumberSInt64Type, ref result))
            {
                return result;
            }
        }

        return 0;
    }

    /// <summary>
    /// パージ可能領域を取得 (CSDiskSpaceGetRecoveryEstimate相当)
    /// </summary>
    /// <remarks>
    /// 実際のCSDiskSpaceGetRecoveryEstimateはCoreServices frameworkにあり、
    /// .NETから直接呼び出すには追加のP/Invokeが必要
    /// </remarks>
    public static long GetPurgeableSpace(string mountPoint)
    {
        // 簡易実装: diskutil経由で取得
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/usr/sbin/diskutil",
                Arguments = $"info \"{mountPoint}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process is null)
            {
                return 0;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // "Purgeable Space:" 行を探す
            foreach (var line in output.Split('\n'))
            {
                if (line.Contains("Purgeable", StringComparison.OrdinalIgnoreCase))
                {
                    // 例: "   Purgeable Space:                 5.0 GB (5000000000 Bytes)"
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"\((\d+)\s*Bytes\)");
                    if (match.Success && long.TryParse(match.Groups[1].Value, out var bytes))
                    {
                        return bytes;
                    }
                }
            }
        }
        catch
        {
            // Ignore
        }

        return 0;
    }
}
