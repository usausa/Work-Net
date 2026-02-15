namespace MacDotNet.SystemInfo.Lab;

using static NativeMethods;

/// <summary>
/// NVMe SMART情報
/// </summary>
public sealed record DiskSmartEntry
{
    public required string BsdName { get; init; }

    /// <summary>
    /// SMART対応
    /// </summary>
    public bool SmartCapable { get; init; }
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
    /// <summary>
    /// NVMe SMART対応確認 (ディスクごと)
    /// </summary>
    public static DiskSmartEntry? GetSmartInfo(string bsdName)
    {
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

                    var smartCapable = false;
                    if (smartCapablePtr != nint.Zero)
                    {
                        smartCapable = CFBooleanGetValue(smartCapablePtr);
                        CFRelease(smartCapablePtr);
                    }

                    return new DiskSmartEntry
                    {
                        BsdName = bsdName,
                        SmartCapable = smartCapable,
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
}
