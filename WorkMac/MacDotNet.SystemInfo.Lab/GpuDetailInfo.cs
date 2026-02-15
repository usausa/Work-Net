namespace MacDotNet.SystemInfo.Lab;

using System.Runtime.InteropServices;

using static NativeMethods;

/// <summary>
/// GPU詳細情報
/// </summary>
public sealed record GpuDetailEntry
{
    public required string Id { get; init; }

    public string? Model { get; init; }

    public string? Vendor { get; init; }

    public string? IOClass { get; init; }

    /// <summary>
    /// GPU使用率 (0.0-1.0)
    /// </summary>
    public double? Utilization { get; init; }

    /// <summary>
    /// レンダラー使用率 (0.0-1.0)
    /// </summary>
    public double? RenderUtilization { get; init; }

    /// <summary>
    /// タイラー使用率 (0.0-1.0)
    /// </summary>
    public double? TilerUtilization { get; init; }

    /// <summary>
    /// 温度 (℃)
    /// </summary>
    public int? Temperature { get; init; }

    /// <summary>
    /// ファン速度 (%)
    /// </summary>
    public int? FanSpeed { get; init; }

    /// <summary>
    /// コアクロック (MHz)
    /// </summary>
    public int? CoreClock { get; init; }

    /// <summary>
    /// メモリクロック (MHz)
    /// </summary>
    public int? MemoryClock { get; init; }

    /// <summary>
    /// 電源状態 (true = Active)
    /// </summary>
    public bool? PowerState { get; init; }
}

/// <summary>
/// GPU詳細情報取得
/// </summary>
public static class GpuDetailInfo
{
    private const string kIOAcceleratorClassName = "IOAccelerator";

    public static GpuDetailEntry[] GetGpuDetails()
    {
        var results = new List<GpuDetailEntry>();

        var matching = IOServiceMatching(kIOAcceleratorClassName);
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
            var index = 0;
            while ((service = IOIteratorNext(iterator)) != 0)
            {
                try
                {
                    var entry = ReadGpuService(service, index);
                    if (entry is not null)
                    {
                        results.Add(entry);
                    }

                    index++;
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

    private static GpuDetailEntry? ReadGpuService(uint service, int index)
    {
        if (IORegistryEntryCreateCFProperties(service, out var propertiesPtr, nint.Zero, 0) != 0)
        {
            return null;
        }

        if (propertiesPtr == nint.Zero)
        {
            return null;
        }

        try
        {
            var ioClass = GetDictionaryStringValue(propertiesPtr, "IOClass");
            var model = GetDictionaryStringValue(propertiesPtr, "model");

            // PerformanceStatistics辞書を取得
            var perfStatsKey = CFStringCreateWithCString(nint.Zero, "PerformanceStatistics", kCFStringEncodingUTF8);
            var perfStatsPtr = CFDictionaryGetValue(propertiesPtr, perfStatsKey);
            CFRelease(perfStatsKey);

            int? utilization = null;
            int? renderUtil = null;
            int? tilerUtil = null;
            int? temperature = null;
            int? fanSpeed = null;
            int? coreClock = null;
            int? memoryClock = null;

            if (perfStatsPtr != nint.Zero)
            {
                utilization = GetDictionaryIntValue(perfStatsPtr, "Device Utilization %")
                              ?? GetDictionaryIntValue(perfStatsPtr, "GPU Activity(%)");
                renderUtil = GetDictionaryIntValue(perfStatsPtr, "Renderer Utilization %");
                tilerUtil = GetDictionaryIntValue(perfStatsPtr, "Tiler Utilization %");
                temperature = GetDictionaryIntValue(perfStatsPtr, "Temperature(C)");
                fanSpeed = GetDictionaryIntValue(perfStatsPtr, "Fan Speed(%)");
                coreClock = GetDictionaryIntValue(perfStatsPtr, "Core Clock(MHz)");
                memoryClock = GetDictionaryIntValue(perfStatsPtr, "Memory Clock(MHz)");
            }

            // AGCInfo (電源状態)
            bool? powerState = null;
            var agcInfoKey = CFStringCreateWithCString(nint.Zero, "AGCInfo", kCFStringEncodingUTF8);
            var agcInfoPtr = CFDictionaryGetValue(propertiesPtr, agcInfoKey);
            CFRelease(agcInfoKey);

            if (agcInfoPtr != nint.Zero)
            {
                var poweredOff = GetDictionaryIntValue(agcInfoPtr, "poweredOffByAGC");
                if (poweredOff is not null)
                {
                    powerState = poweredOff == 0;
                }
            }

            // 温度がない場合はSMC経由で取得を試みる
            if (temperature is null or 0 or 128)
            {
                // Intel GPU: TCGC, AMD GPU: TGDD
                if (ioClass?.Contains("intel", StringComparison.OrdinalIgnoreCase) == true)
                {
                    temperature = SmcHelper.ReadSmcTemperature("TCGC");
                }
                else if (ioClass?.Contains("amd", StringComparison.OrdinalIgnoreCase) == true)
                {
                    temperature = SmcHelper.ReadSmcTemperature("TGDD");
                }
            }

            return new GpuDetailEntry
            {
                Id = $"GPU#{index}",
                Model = model,
                IOClass = ioClass,
                Utilization = utilization is not null ? Math.Min(utilization.Value, 100) / 100.0 : null,
                RenderUtilization = renderUtil is not null ? Math.Min(renderUtil.Value, 100) / 100.0 : null,
                TilerUtilization = tilerUtil is not null ? Math.Min(tilerUtil.Value, 100) / 100.0 : null,
                Temperature = temperature,
                FanSpeed = fanSpeed,
                CoreClock = coreClock,
                MemoryClock = memoryClock,
                PowerState = powerState,
            };
        }
        finally
        {
            CFRelease(propertiesPtr);
        }
    }

    private static string? GetDictionaryStringValue(nint dict, string keyName)
    {
        var key = CFStringCreateWithCString(nint.Zero, keyName, kCFStringEncodingUTF8);
        var value = CFDictionaryGetValue(dict, key);
        CFRelease(key);

        if (value == nint.Zero)
        {
            return null;
        }

        if (CFGetTypeID(value) == CFStringGetTypeID())
        {
            return CfStringToManaged(value);
        }

        if (CFGetTypeID(value) == CFDataGetTypeID())
        {
            var length = CFDataGetLength(value);
            var bytes = CFDataGetBytePtr(value);
            if (bytes != nint.Zero && length > 0)
            {
                return Marshal.PtrToStringUTF8(bytes, (int)length)?.TrimEnd('\0');
            }
        }

        return null;
    }

    private static int? GetDictionaryIntValue(nint dict, string keyName)
    {
        var key = CFStringCreateWithCString(nint.Zero, keyName, kCFStringEncodingUTF8);
        var value = CFDictionaryGetValue(dict, key);
        CFRelease(key);

        if (value != nint.Zero && CFGetTypeID(value) == CFNumberGetTypeID())
        {
            if (CFNumberGetValue(value, kCFNumberSInt32Type, out var intValue))
            {
                return intValue;
            }
        }

        return null;
    }
}

/// <summary>
/// SMCヘルパー
/// </summary>
internal static class SmcHelper
{
    private static uint smcConnection;
    private static bool initialized;

    public static int? ReadSmcTemperature(string key)
    {
        if (!EnsureConnection())
        {
            return null;
        }

        var value = ReadSmcValue(key);
        if (value is not null && value != 128)
        {
            return (int)value;
        }

        return null;
    }

    public static double? ReadSmcValue(string key)
    {
        if (!EnsureConnection())
        {
            return null;
        }

        return ReadKey(key);
    }

    private static bool EnsureConnection()
    {
        if (initialized)
        {
            return smcConnection != 0;
        }

        initialized = true;

        var matching = IOServiceMatching("AppleSMC");
        if (matching == nint.Zero)
        {
            return false;
        }

        var service = IOServiceGetMatchingService(0, matching);
        if (service == 0)
        {
            return false;
        }

        try
        {
            if (IOServiceOpen(service, mach_task_self(), 0, out smcConnection) != 0)
            {
                return false;
            }
        }
        finally
        {
            IOObjectRelease(service);
        }

        return true;
    }

    private static unsafe double? ReadKey(string key)
    {
        var keyInfo = new SMCKeyData_t();
        keyInfo.key = KeyToUInt32(key);
        keyInfo.data8 = SMC_CMD_READ_KEYINFO;

        var outputSize = (nuint)Marshal.SizeOf<SMCKeyData_t>();
        if (IOConnectCallStructMethod(smcConnection, KERNEL_INDEX_SMC, &keyInfo, outputSize, &keyInfo, &outputSize) != 0)
        {
            return null;
        }

        var dataType = keyInfo.keyInfo.dataType;
        var dataSize = keyInfo.keyInfo.dataSize;

        keyInfo.keyInfo.dataSize = dataSize;
        keyInfo.data8 = SMC_CMD_READ_BYTES;

        if (IOConnectCallStructMethod(smcConnection, KERNEL_INDEX_SMC, &keyInfo, outputSize, &keyInfo, &outputSize) != 0)
        {
            return null;
        }

        return ConvertSmcValue(dataType, dataSize, keyInfo.bytes);
    }

    private static unsafe double? ConvertSmcValue(uint dataType, uint dataSize, byte* bytes)
    {
        if (dataType == DATA_TYPE_FLT && dataSize == 4)
        {
            var value = *(float*)bytes;
            return BitConverter.IsLittleEndian
                ? BitConverter.Int32BitsToSingle(System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(*(int*)bytes))
                : value;
        }

        if (dataType == DATA_TYPE_SP78 && dataSize == 2)
        {
            var raw = (ushort)((bytes[0] << 8) | bytes[1]);
            return raw / 256.0;
        }

        if (dataType == DATA_TYPE_FPE2 && dataSize == 2)
        {
            var raw = (ushort)((bytes[0] << 8) | bytes[1]);
            return raw / 4.0;
        }

        if (dataType == DATA_TYPE_IOFT && dataSize == 8)
        {
            // ioft = 64-bit float (big-endian)
            var longValue = ((long)bytes[0] << 56) | ((long)bytes[1] << 48) | ((long)bytes[2] << 40) | ((long)bytes[3] << 32)
                            | ((long)bytes[4] << 24) | ((long)bytes[5] << 16) | ((long)bytes[6] << 8) | bytes[7];
            return BitConverter.Int64BitsToDouble(longValue);
        }

        if (dataType == DATA_TYPE_UI8 && dataSize == 1)
        {
            return bytes[0];
        }

        if (dataType == DATA_TYPE_UI16 && dataSize == 2)
        {
            return (ushort)((bytes[0] << 8) | bytes[1]);
        }

        if (dataType == DATA_TYPE_UI32 && dataSize == 4)
        {
            return (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
        }

        return null;
    }
}
