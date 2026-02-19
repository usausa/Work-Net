// IOAcceleratorのPerformanceStatisticsプロパティからGPU使用率を取得する。
// Apple Silicon MacではAGXAccelerator系ドライバがIOAcceleratorクラスとして登録されている。
// IOAcceleratorをマッチングキーに使用することで、Intel/Apple Silicon両方に対応可能。
// PerformanceStatisticsは辞書型プロパティで、Device/Renderer/Tiler Utilization %、
// メモリ使用量等のメトリクスを含む。

namespace WorkGpu;

using System.Runtime.InteropServices;

using static WorkGpu.NativeMethods;

internal static class Program
{
    public static void Main()
    {
        var gpus = GpuInfoProvider.GetGpuInfo();
        if (gpus.Length == 0)
        {
            Console.WriteLine("No GPU (IOAccelerator) found.");
            return;
        }

        for (var i = 0; i < gpus.Length; i++)
        {
            var gpu = gpus[i];

            Console.WriteLine($"=== GPU [{i}] ===");
            Console.WriteLine($"Model:            {gpu.Model}");
            Console.WriteLine($"Class:            {gpu.ClassName}");
            Console.WriteLine($"Metal Plugin:     {gpu.MetalPluginName}");
            Console.WriteLine($"Core Count:       {gpu.CoreCount}");
            Console.WriteLine($"Vendor ID:        0x{gpu.VendorId:X4}");
            Console.WriteLine();

            // GPU構成情報
            if (gpu.Configuration is not null)
            {
                var cfg = gpu.Configuration;
                Console.WriteLine("--- Configuration ---");
                Console.WriteLine($"GPU Generation:   {cfg.GpuGeneration}");
                Console.WriteLine($"Num Cores:        {cfg.NumCores}");
                Console.WriteLine($"Num GPs:          {cfg.NumGPs}");
                Console.WriteLine($"Num Frags:        {cfg.NumFragments}");
                Console.WriteLine($"Num mGPUs:        {cfg.NumMGpus}");
                Console.WriteLine($"USC Generation:   {cfg.UscGeneration}");
                Console.WriteLine();
            }

            // パフォーマンス統計
            if (gpu.Performance is not null)
            {
                var perf = gpu.Performance;
                Console.WriteLine("--- Performance ---");
                Console.WriteLine($"Device Utilization:   {perf.DeviceUtilization}%");
                Console.WriteLine($"Renderer Utilization: {perf.RendererUtilization}%");
                Console.WriteLine($"Tiler Utilization:    {perf.TilerUtilization}%");
                Console.WriteLine();

                Console.WriteLine("--- Memory ---");
                Console.WriteLine($"Alloc System Memory:       {FormatBytes(perf.AllocSystemMemory)}");
                Console.WriteLine($"In Use System Memory:      {FormatBytes(perf.InUseSystemMemory)}");
                Console.WriteLine($"In Use System Memory (drv):{FormatBytes(perf.InUseSystemMemoryDriver)}");
                Console.WriteLine($"Tiled Scene Bytes:         {FormatBytes(perf.TiledSceneBytes)}");
                Console.WriteLine($"Allocated PB Size:         {FormatBytes(perf.AllocatedPBSize)}");
                Console.WriteLine();

                Console.WriteLine("--- Recovery ---");
                Console.WriteLine($"Recovery Count:   {perf.RecoveryCount}");
                Console.WriteLine($"Split Scene Count:{perf.SplitSceneCount}");
            }

            Console.WriteLine();
        }
    }

    private static string FormatBytes(long bytes)
    {
        var u = (ulong)(bytes < 0 ? 0 : bytes);
        return u switch
        {
            >= 1UL << 30 => $"{u / (double)(1UL << 30):F2} GiB",
            >= 1UL << 20 => $"{u / (double)(1UL << 20):F2} MiB",
            >= 1UL << 10 => $"{u / (double)(1UL << 10):F2} KiB",
            _ => $"{u} B",
        };
    }
}

// GPU情報
internal sealed record GpuInfo
{
    // GPUモデル名 (例: "Apple M2 Pro")
    public required string Model { get; init; }

    // IOKitドライバクラス名 (例: "AGXAcceleratorG14X")
    public required string ClassName { get; init; }

    // Metalプラグイン名 (例: "AGXMetalG14X")
    public string? MetalPluginName { get; init; }

    // GPUコア数 (gpu-core-count)
    public required int CoreCount { get; init; }

    // ベンダーID (vendor-id、リトルエンディアン)
    public required uint VendorId { get; init; }

    // パフォーマンス統計
    public GpuPerformanceStatistics? Performance { get; init; }

    // GPU構成情報 (GPUConfigurationVariable)
    public GpuConfiguration? Configuration { get; init; }
}

// GPU パフォーマンス統計 (PerformanceStatistics辞書)
internal sealed record GpuPerformanceStatistics
{
    // デバイス全体の使用率(%) — GPU全体の稼働状況
    public required long DeviceUtilization { get; init; }

    // レンダラー使用率(%) — 3Dレンダリングパイプラインの稼働状況
    public required long RendererUtilization { get; init; }

    // タイラー使用率(%) — タイルベースレンダリングの頂点処理稼働状況
    public required long TilerUtilization { get; init; }

    // 確保済みシステムメモリ(バイト) — GPUが確保したシステムメモリの総量
    public required long AllocSystemMemory { get; init; }

    // 使用中システムメモリ(バイト) — GPUが実際に使用しているシステムメモリ量
    public required long InUseSystemMemory { get; init; }

    // ドライバ使用中システムメモリ(バイト) — ドライバが使用しているシステムメモリ量
    public required long InUseSystemMemoryDriver { get; init; }

    // タイルドシーンバイト数 — タイルベースレンダリングのシーンデータサイズ
    public required long TiledSceneBytes { get; init; }

    // 確保済みパラメータバッファサイズ(バイト)
    public required long AllocatedPBSize { get; init; }

    // GPU回復回数 — GPUリセットが発生した回数(通常0)
    public required long RecoveryCount { get; init; }

    // シーン分割回数 — レンダリングシーンが分割された回数
    public required long SplitSceneCount { get; init; }
}

// GPU構成情報 (GPUConfigurationVariable辞書)
internal sealed record GpuConfiguration
{
    // GPU世代番号 (例: 14 = Apple GPU Generation 14)
    public required int GpuGeneration { get; init; }

    // コア数 (物理コア数、gpu-core-countと異なる場合あり)
    public required int NumCores { get; init; }

    // GPグループ数 (Geometry Processor数)
    public required int NumGPs { get; init; }

    // フラグメントプロセッサ数
    public required int NumFragments { get; init; }

    // マルチGPU数
    public required int NumMGpus { get; init; }

    // USCジェネレーション (Unified Shader Core世代)
    public required int UscGeneration { get; init; }
}

// GPU情報取得
// IOAccelerator (IOKitのGPUアクセラレータクラス) からプロパティを取得する。
// Apple SiliconではAGXAcceleratorG14X等のサブクラスがIOAcceleratorとして登録されている。
internal static class GpuInfoProvider
{
    public static GpuInfo[] GetGpuInfo()
    {
        var iter = IntPtr.Zero;
        var kr = IOServiceGetMatchingServices(0, IOServiceMatching("IOAccelerator"), ref iter);
        if (kr != KERN_SUCCESS || iter == IntPtr.Zero)
        {
            return [];
        }

        try
        {
            var results = new List<GpuInfo>();
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

    private static GpuInfo ReadGpuEntry(uint entry)
    {
        return new GpuInfo
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

    // IORegistry文字列プロパティ取得
    private static string? GetStringProperty(uint entry, string key)
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

    // IORegistry数値プロパティ取得
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

    // IORegistryデータプロパティからリトルエンディアンuint32を取得
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

    // IORegistry辞書プロパティ取得 (呼び出し元がCFReleaseすること)
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

    // CFDictionaryから数値を取得
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

    // CFStringをマネージド文字列に変換
    private static unsafe string? CfStringToManaged(IntPtr cfString)
    {
        var ptr = CFStringGetCStringPtr(cfString, kCFStringEncodingUTF8);
        if (ptr != IntPtr.Zero)
        {
            return Marshal.PtrToStringUTF8(ptr);
        }

        var length = CFStringGetLength(cfString);
        if (length <= 0)
        {
            return string.Empty;
        }

        // CFStringGetCStringPtrが失敗した場合のフォールバック
        var bufSize = (length * 4) + 1;
        var buf = stackalloc byte[(int)bufSize];
        return CFStringGetCString(cfString, buf, bufSize, kCFStringEncodingUTF8)
            ? Marshal.PtrToStringUTF8((IntPtr)buf)
            : null;
    }
}

// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
#pragma warning disable CA2101
#pragma warning disable CA5392
#pragma warning disable CS8981
internal static class NativeMethods
{
    // 成功コード (mach/kern_return.h)
    public const int KERN_SUCCESS = 0;

    // CFString エンコーディング
    public const uint kCFStringEncodingUTF8 = 0x08000100;

    // CFNumber タイプ
    public const int kCFNumberSInt64Type = 4;

    //------------------------------------------------------------------------
    // IOKit
    //------------------------------------------------------------------------

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static extern int IOServiceGetMatchingServices(uint mainPort, IntPtr matching, ref IntPtr existing);

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static extern uint IOIteratorNext(IntPtr iterator);

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static extern int IOObjectRelease(IntPtr @object);

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static extern int IOObjectRelease(uint @object);

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static extern IntPtr IOServiceMatching(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static extern IntPtr IORegistryEntryCreateCFProperty(
        uint entry,
        IntPtr key,
        IntPtr allocator,
        uint options);

    //------------------------------------------------------------------------
    // CoreFoundation
    //------------------------------------------------------------------------

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static extern IntPtr CFStringCreateWithCString(IntPtr alloc,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string cStr,
        uint encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static extern IntPtr CFStringGetCStringPtr(IntPtr theString, uint encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static extern IntPtr CFStringGetLength(IntPtr theString);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static extern unsafe bool CFStringGetCString(IntPtr theString, byte* buffer, IntPtr bufferSize, uint encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool CFNumberGetValue(IntPtr number, int theType, ref long valuePtr);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static extern nuint CFGetTypeID(IntPtr cf);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static extern nuint CFStringGetTypeID();

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static extern nuint CFNumberGetTypeID();

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static extern nuint CFDictionaryGetTypeID();

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static extern nuint CFDataGetTypeID();

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static extern IntPtr CFDictionaryGetValue(IntPtr theDict, IntPtr key);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static extern IntPtr CFDataGetLength(IntPtr theData);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static extern IntPtr CFDataGetBytePtr(IntPtr theData);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static extern void CFRelease(IntPtr cf);
}
