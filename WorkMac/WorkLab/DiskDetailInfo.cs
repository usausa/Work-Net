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
    public string? ProductName { get; init; }
    public string? Interconnect { get; init; }
    public string? Location { get; init; }
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
        if (matching == IntPtr.Zero)
        {
            return null;
        }

        var iterator = IntPtr.Zero;
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
                    var nameKey = CFStringCreateWithCString(IntPtr.Zero, "BSD Name", kCFStringEncodingUTF8);
                    var namePtr = IORegistryEntryCreateCFProperty(service, nameKey, IntPtr.Zero, 0);
                    CFRelease(nameKey);

                    string? currentBsdName = null;
                    if (namePtr != IntPtr.Zero)
                    {
                        currentBsdName = CfStringToManaged(namePtr);
                        CFRelease(namePtr);
                    }

                    if (currentBsdName != bsdName)
                    {
                        continue;
                    }

                    // NVMe SMART対応確認
                    var smartCapableKey = CFStringCreateWithCString(IntPtr.Zero, "NVMe SMART Capable", kCFStringEncodingUTF8);
                    var smartCapablePtr = IORegistryEntryCreateCFProperty(service, smartCapableKey, IntPtr.Zero, 0);
                    CFRelease(smartCapableKey);

                    var smartCapable = false;
                    if (smartCapablePtr != IntPtr.Zero)
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
        if (matching == IntPtr.Zero)
        {
            return [];
        }

        var iterator = IntPtr.Zero;
        if (IOServiceGetMatchingServices(0, matching, ref iterator) != 0)
        {
            return [];
        }

        var ioServicePlane = Marshal.StringToHGlobalAnsi("IOService");

        try
        {
            uint service;
            while ((service = IOIteratorNext(iterator)) != 0)
            {
                try
                {
                    if (IORegistryEntryCreateCFProperties(service, out var propsPtr, IntPtr.Zero, 0) != 0)
                    {
                        continue;
                    }

                    if (propsPtr == IntPtr.Zero)
                    {
                        continue;
                    }

                    try
                    {
                        var statsKey = CFStringCreateWithCString(IntPtr.Zero, "Statistics", kCFStringEncodingUTF8);
                        var statsPtr = CFDictionaryGetValue(propsPtr, statsKey);
                        CFRelease(statsKey);

                        if (statsPtr == IntPtr.Zero)
                        {
                            continue;
                        }

                        var readBytes = GetDictionaryLongValue(statsPtr, "Bytes (Read)");
                        var writeBytes = GetDictionaryLongValue(statsPtr, "Bytes (Write)");
                        var readOps = GetDictionaryLongValue(statsPtr, "Operations (Read)");
                        var writeOps = GetDictionaryLongValue(statsPtr, "Operations (Write)");

                        // 子IOMediaからBSD名を取得
                        var bsdName = GetChildBsdName(service, ioServicePlane);

                        // 親IOBlockStorageDeviceからデバイス情報を取得
                        var (productName, interconnect, location) = GetParentDeviceInfo(service, ioServicePlane);

                        results.Add(new DiskIoStats
                        {
                            BsdName = bsdName ?? "(no media)",
                            ProductName = productName,
                            Interconnect = interconnect,
                            Location = location,
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
            Marshal.FreeHGlobal(ioServicePlane);
            IOObjectRelease((uint)iterator);
        }

        return [.. results];
    }

    private static string? GetChildBsdName(uint service, IntPtr plane)
    {
        if (IORegistryEntryGetChildIterator(service, plane, out var childIterator) != 0)
        {
            return null;
        }

        try
        {
            var child = IOIteratorNext(childIterator);
            if (child == 0)
            {
                return null;
            }

            try
            {
                var nameKey = CFStringCreateWithCString(IntPtr.Zero, "BSD Name", kCFStringEncodingUTF8);
                var namePtr = IORegistryEntryCreateCFProperty(child, nameKey, IntPtr.Zero, 0);
                CFRelease(nameKey);

                if (namePtr == IntPtr.Zero)
                {
                    return null;
                }

                var name = CfStringToManaged(namePtr);
                CFRelease(namePtr);
                return name;
            }
            finally
            {
                IOObjectRelease(child);
            }
        }
        finally
        {
            IOObjectRelease((uint)childIterator);
        }
    }

    private static (string? ProductName, string? Interconnect, string? Location) GetParentDeviceInfo(uint service, IntPtr plane)
    {
        if (IORegistryEntryGetParentEntry(service, plane, out var parent) != 0)
        {
            return (null, null, null);
        }

        try
        {
            var productName = GetNestedDictionaryStringValue(parent, "Device Characteristics", "Product Name");
            var interconnect = GetNestedDictionaryStringValue(parent, "Protocol Characteristics", "Physical Interconnect");
            var location = GetNestedDictionaryStringValue(parent, "Protocol Characteristics", "Physical Interconnect Location");
            return (productName, interconnect, location);
        }
        finally
        {
            IOObjectRelease(parent);
        }
    }

    private static string? GetNestedDictionaryStringValue(uint entry, string dictKey, string valueKey)
    {
        var dictCfKey = CFStringCreateWithCString(IntPtr.Zero, dictKey, kCFStringEncodingUTF8);
        var dictPtr = IORegistryEntryCreateCFProperty(entry, dictCfKey, IntPtr.Zero, 0);
        CFRelease(dictCfKey);

        if (dictPtr == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var valueCfKey = CFStringCreateWithCString(IntPtr.Zero, valueKey, kCFStringEncodingUTF8);
            var valuePtr = CFDictionaryGetValue(dictPtr, valueCfKey);
            CFRelease(valueCfKey);

            if (valuePtr == IntPtr.Zero)
            {
                return null;
            }

            return CfStringToManaged(valuePtr);
        }
        finally
        {
            CFRelease(dictPtr);
        }
    }

    private static long GetDictionaryLongValue(IntPtr dict, string keyName)
    {
        var key = CFStringCreateWithCString(IntPtr.Zero, keyName, kCFStringEncodingUTF8);
        var value = CFDictionaryGetValue(dict, key);
        CFRelease(key);

        if (value != IntPtr.Zero && CFGetTypeID(value) == CFNumberGetTypeID())
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
