namespace MacDotNet.SystemInfo;

using System.Runtime.InteropServices;

using static MacDotNet.SystemInfo.NativeMethods;

public sealed record GpuPerformanceStatistics
{
    public required long DeviceUtilization { get; init; }

    public required long RendererUtilization { get; init; }

    public required long TilerUtilization { get; init; }

    public required long AllocSystemMemory { get; init; }

    public required long InUseSystemMemory { get; init; }

    public required long InUseSystemMemoryDriver { get; init; }

    public required long TiledSceneBytes { get; init; }

    public required long AllocatedPBSize { get; init; }

    public required long RecoveryCount { get; init; }

    public required long SplitSceneCount { get; init; }

    public long Temperature { get; init; }

    public long FanSpeed { get; init; }

    public long CoreClock { get; init; }

    public long MemoryClock { get; init; }
}

public sealed record GpuConfiguration
{
    public required int GpuGeneration { get; init; }

    public required int NumCores { get; init; }

    public required int NumGPs { get; init; }

    public required int NumFragments { get; init; }

    public required int NumMGpus { get; init; }

    public required int UscGeneration { get; init; }
}

public sealed record GpuEntry
{
    public required string Model { get; init; }

    public required string ClassName { get; init; }

    public string? MetalPluginName { get; init; }

    public required int CoreCount { get; init; }

    public required uint VendorId { get; init; }

    public int? Temperature { get; init; }

    public int? FanSpeed { get; init; }

    public int? CoreClock { get; init; }

    public int? MemoryClock { get; init; }

    public bool? PowerState { get; init; }

    public GpuPerformanceStatistics? Performance { get; init; }

    public GpuConfiguration? Configuration { get; init; }
}

public static class GpuInfo
{
    public static GpuEntry[] GetGpus()
    {
        var iter = IntPtr.Zero;
        var kr = IOServiceGetMatchingServices(0, IOServiceMatching("IOAccelerator"), ref iter);
        if (kr != KERN_SUCCESS || iter == IntPtr.Zero)
        {
            return [];
        }

        try
        {
            var results = new List<GpuEntry>();
            uint entry;
            while ((entry = IOIteratorNext(iter)) != 0)
            {
                try
                {
                    results.Add(ReadGpuEntry(entry));
                }
                finally
                {
                    IOObjectRelease(entry);
                }
            }

            return [.. results];
        }
        finally
        {
            IOObjectRelease(iter);
        }
    }

    private static GpuEntry ReadGpuEntry(uint entry)
    {
        var ioClass = GetStringProperty(entry, "IOClass");
        var performance = ReadPerformanceStatistics(entry);

        int? temperature = null;
        int? fanSpeed = null;
        int? coreClock = null;
        int? memoryClock = null;
        bool? powerState = null;

        if (performance is not null)
        {
            temperature = performance.Temperature > 0 && performance.Temperature < 128 ? (int)performance.Temperature : null;
            fanSpeed = performance.FanSpeed > 0 ? (int)performance.FanSpeed : null;
            coreClock = performance.CoreClock > 0 ? (int)performance.CoreClock : null;
            memoryClock = performance.MemoryClock > 0 ? (int)performance.MemoryClock : null;
        }

        var agcInfo = GetDictionaryProperty(entry, "AGCInfo");
        if (agcInfo != IntPtr.Zero)
        {
            var poweredOff = GetDictNumber(agcInfo, "poweredOffByAGC");
            powerState = poweredOff == 0;
            CFRelease(agcInfo);
        }

        if (temperature is null)
        {
            if (ioClass?.Contains("intel", StringComparison.OrdinalIgnoreCase) == true)
            {
                temperature = SmcInfo.ReadSmcTemperature("TCGC");
            }
            else if (ioClass?.Contains("amd", StringComparison.OrdinalIgnoreCase) == true)
            {
                temperature = SmcInfo.ReadSmcTemperature("TGDD");
            }
        }

        return new GpuEntry
        {
            Model = GetStringProperty(entry, "model") ?? "(unknown)",
            ClassName = ioClass ?? "(unknown)",
            MetalPluginName = GetStringProperty(entry, "MetalPluginName"),
            CoreCount = (int)GetNumberProperty(entry, "gpu-core-count"),
            VendorId = GetDataPropertyAsUInt32LE(entry, "vendor-id"),
            Temperature = temperature,
            FanSpeed = fanSpeed,
            CoreClock = coreClock,
            MemoryClock = memoryClock,
            PowerState = powerState,
            Performance = performance,
            Configuration = ReadGpuConfiguration(entry),
        };
    }

    private static GpuPerformanceStatistics? ReadPerformanceStatistics(uint entry)
    {
        var dict = GetDictionaryProperty(entry, "PerformanceStatistics");
        if (dict == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            return new GpuPerformanceStatistics
            {
                DeviceUtilization = GetDictNumber(dict, "Device Utilization %"),
                RendererUtilization = GetDictNumber(dict, "Renderer Utilization %"),
                TilerUtilization = GetDictNumber(dict, "Tiler Utilization %"),
                AllocSystemMemory = GetDictNumber(dict, "Alloc system memory"),
                InUseSystemMemory = GetDictNumber(dict, "In use system memory"),
                InUseSystemMemoryDriver = GetDictNumber(dict, "In use system memory (driver)"),
                TiledSceneBytes = GetDictNumber(dict, "TiledSceneBytes"),
                AllocatedPBSize = GetDictNumber(dict, "Allocated PB Size"),
                RecoveryCount = GetDictNumber(dict, "recoveryCount"),
                SplitSceneCount = GetDictNumber(dict, "SplitSceneCount"),
                Temperature = GetDictNumber(dict, "Temperature(C)"),
                FanSpeed = GetDictNumber(dict, "Fan Speed(%)"),
                CoreClock = GetDictNumber(dict, "Core Clock(MHz)"),
                MemoryClock = GetDictNumber(dict, "Memory Clock(MHz)"),
            };
        }
        finally
        {
            CFRelease(dict);
        }
    }

    private static GpuConfiguration? ReadGpuConfiguration(uint entry)
    {
        var dict = GetDictionaryProperty(entry, "GPUConfigurationVariable");
        if (dict == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            return new GpuConfiguration
            {
                GpuGeneration = (int)GetDictNumber(dict, "gpu_gen"),
                NumCores = (int)GetDictNumber(dict, "num_cores"),
                NumGPs = (int)GetDictNumber(dict, "num_gps"),
                NumFragments = (int)GetDictNumber(dict, "num_frags"),
                NumMGpus = (int)GetDictNumber(dict, "num_mgpus"),
                UscGeneration = (int)GetDictNumber(dict, "usc_gen"),
            };
        }
        finally
        {
            CFRelease(dict);
        }
    }

    private static unsafe string? GetStringProperty(uint entry, string key)
    {
        var cfKey = CFStringCreateWithCString(IntPtr.Zero, key, kCFStringEncodingUTF8);
        if (cfKey == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var val = IORegistryEntryCreateCFProperty(entry, cfKey, IntPtr.Zero, 0);
            if (val == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                if (CFGetTypeID(val) != CFStringGetTypeID())
                {
                    return null;
                }

                return CfStringToManaged(val);
            }
            finally
            {
                CFRelease(val);
            }
        }
        finally
        {
            CFRelease(cfKey);
        }
    }

    private static long GetNumberProperty(uint entry, string key)
    {
        var cfKey = CFStringCreateWithCString(IntPtr.Zero, key, kCFStringEncodingUTF8);
        if (cfKey == IntPtr.Zero)
        {
            return 0;
        }

        try
        {
            var val = IORegistryEntryCreateCFProperty(entry, cfKey, IntPtr.Zero, 0);
            if (val == IntPtr.Zero)
            {
                return 0;
            }

            try
            {
                if (CFGetTypeID(val) != CFNumberGetTypeID())
                {
                    return 0;
                }

                long result = 0;
                CFNumberGetValue(val, kCFNumberSInt64Type, ref result);
                return result;
            }
            finally
            {
                CFRelease(val);
            }
        }
        finally
        {
            CFRelease(cfKey);
        }
    }

    private static uint GetDataPropertyAsUInt32LE(uint entry, string key)
    {
        var cfKey = CFStringCreateWithCString(IntPtr.Zero, key, kCFStringEncodingUTF8);
        if (cfKey == IntPtr.Zero)
        {
            return 0;
        }

        try
        {
            var val = IORegistryEntryCreateCFProperty(entry, cfKey, IntPtr.Zero, 0);
            if (val == IntPtr.Zero)
            {
                return 0;
            }

            try
            {
                if (CFGetTypeID(val) != CFDataGetTypeID())
                {
                    return 0;
                }

                var len = CFDataGetLength(val);
                if (len < 4)
                {
                    return 0;
                }

                var ptr = CFDataGetBytePtr(val);
                return (uint)(Marshal.ReadByte(ptr, 0)
                    | (Marshal.ReadByte(ptr, 1) << 8)
                    | (Marshal.ReadByte(ptr, 2) << 16)
                    | (Marshal.ReadByte(ptr, 3) << 24));
            }
            finally
            {
                CFRelease(val);
            }
        }
        finally
        {
            CFRelease(cfKey);
        }
    }

    private static IntPtr GetDictionaryProperty(uint entry, string key)
    {
        var cfKey = CFStringCreateWithCString(IntPtr.Zero, key, kCFStringEncodingUTF8);
        if (cfKey == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        try
        {
            var val = IORegistryEntryCreateCFProperty(entry, cfKey, IntPtr.Zero, 0);
            if (val == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            if (CFGetTypeID(val) != CFDictionaryGetTypeID())
            {
                CFRelease(val);
                return IntPtr.Zero;
            }

            return val;
        }
        finally
        {
            CFRelease(cfKey);
        }
    }

    private static long GetDictNumber(IntPtr dict, string key)
    {
        var cfKey = CFStringCreateWithCString(IntPtr.Zero, key, kCFStringEncodingUTF8);
        if (cfKey == IntPtr.Zero)
        {
            return 0;
        }

        try
        {
            var val = CFDictionaryGetValue(dict, cfKey);
            if (val == IntPtr.Zero)
            {
                return 0;
            }

            if (CFGetTypeID(val) != CFNumberGetTypeID())
            {
                return 0;
            }

            long result = 0;
            CFNumberGetValue(val, kCFNumberSInt64Type, ref result);
            return result;
        }
        finally
        {
            CFRelease(cfKey);
        }
    }
}
