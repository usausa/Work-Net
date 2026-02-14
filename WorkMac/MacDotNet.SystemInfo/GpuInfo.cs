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

    public GpuPerformanceStatistics? Performance { get; init; }

    public GpuConfiguration? Configuration { get; init; }
}

public static class GpuInfo
{
    public static GpuEntry[] GetGpus()
    {
        var iter = nint.Zero;
        var kr = IOServiceGetMatchingServices(0, IOServiceMatching("IOAccelerator"), ref iter);
        if (kr != KERN_SUCCESS || iter == nint.Zero)
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
        return new GpuEntry
        {
            Model = GetStringProperty(entry, "model") ?? "(unknown)",
            ClassName = GetStringProperty(entry, "IOClass") ?? "(unknown)",
            MetalPluginName = GetStringProperty(entry, "MetalPluginName"),
            CoreCount = (int)GetNumberProperty(entry, "gpu-core-count"),
            VendorId = GetDataPropertyAsUInt32LE(entry, "vendor-id"),
            Performance = ReadPerformanceStatistics(entry),
            Configuration = ReadGpuConfiguration(entry),
        };
    }

    private static GpuPerformanceStatistics? ReadPerformanceStatistics(uint entry)
    {
        var dict = GetDictionaryProperty(entry, "PerformanceStatistics");
        if (dict == nint.Zero)
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
        if (dict == nint.Zero)
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
        var cfKey = CFStringCreateWithCString(nint.Zero, key, kCFStringEncodingUTF8);
        if (cfKey == nint.Zero)
        {
            return null;
        }

        try
        {
            var val = IORegistryEntryCreateCFProperty(entry, cfKey, nint.Zero, 0);
            if (val == nint.Zero)
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
        var cfKey = CFStringCreateWithCString(nint.Zero, key, kCFStringEncodingUTF8);
        if (cfKey == nint.Zero)
        {
            return 0;
        }

        try
        {
            var val = IORegistryEntryCreateCFProperty(entry, cfKey, nint.Zero, 0);
            if (val == nint.Zero)
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
        var cfKey = CFStringCreateWithCString(nint.Zero, key, kCFStringEncodingUTF8);
        if (cfKey == nint.Zero)
        {
            return 0;
        }

        try
        {
            var val = IORegistryEntryCreateCFProperty(entry, cfKey, nint.Zero, 0);
            if (val == nint.Zero)
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

    private static nint GetDictionaryProperty(uint entry, string key)
    {
        var cfKey = CFStringCreateWithCString(nint.Zero, key, kCFStringEncodingUTF8);
        if (cfKey == nint.Zero)
        {
            return nint.Zero;
        }

        try
        {
            var val = IORegistryEntryCreateCFProperty(entry, cfKey, nint.Zero, 0);
            if (val == nint.Zero)
            {
                return nint.Zero;
            }

            if (CFGetTypeID(val) != CFDictionaryGetTypeID())
            {
                CFRelease(val);
                return nint.Zero;
            }

            return val;
        }
        finally
        {
            CFRelease(cfKey);
        }
    }

    private static long GetDictNumber(nint dict, string key)
    {
        var cfKey = CFStringCreateWithCString(nint.Zero, key, kCFStringEncodingUTF8);
        if (cfKey == nint.Zero)
        {
            return 0;
        }

        try
        {
            var val = CFDictionaryGetValue(dict, cfKey);
            if (val == nint.Zero)
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

    private static unsafe string? CfStringToManaged(nint cfString)
    {
        var ptr = CFStringGetCStringPtr(cfString, kCFStringEncodingUTF8);
        if (ptr != nint.Zero)
        {
            return Marshal.PtrToStringUTF8(ptr);
        }

        var length = CFStringGetLength(cfString);
        if (length <= 0)
        {
            return string.Empty;
        }

        var bufSize = (length * 4) + 1;
        var buf = stackalloc byte[(int)bufSize];
        return CFStringGetCString(cfString, buf, bufSize, kCFStringEncodingUTF8)
            ? Marshal.PtrToStringUTF8((nint)buf)
            : null;
    }
}
