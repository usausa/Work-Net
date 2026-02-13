namespace WorkBattery;

using System.Globalization;
using System.Runtime.InteropServices;

using static WorkBattery.NativeMethods;

internal static class Program
{
    public static void Main()
    {
        var batteries = BatteryInfoProvider.GetBatteryInfo();
        if (batteries.Length == 0)
        {
            Console.WriteLine("No battery found.");
            return;
        }

        for (var i = 0; i < batteries.Length; i++)
        {
            var b = batteries[i];
            if (i > 0)
            {
                Console.WriteLine();
            }

            Console.WriteLine($"=== Battery {i} ===");
            Console.WriteLine($"Name:                  {b.Name}");
            Console.WriteLine($"Type:                  {b.Type}");
            Console.WriteLine($"Transport Type:        {b.TransportType ?? "N/A"}");
            Console.WriteLine($"Serial Number:         {b.HardwareSerialNumber ?? "N/A"}");
            Console.WriteLine($"Is Present:            {b.IsPresent}");
            Console.WriteLine($"Power Source State:     {b.PowerSourceState}");
            Console.WriteLine($"Is AC Connected:       {b.IsACConnected}");
            Console.WriteLine($"Is Charging:           {b.IsCharging}");
            Console.WriteLine($"Is Charged:            {b.IsCharged}");
            Console.WriteLine($"Current Capacity:      {b.CurrentCapacity}");
            Console.WriteLine($"Max Capacity:          {b.MaxCapacity}");
            Console.WriteLine($"Battery Percent:       {b.BatteryPercent}%");
            Console.WriteLine($"Time to Empty:         {FormatMinutes(b.TimeToEmpty)}");
            Console.WriteLine($"Time to Full Charge:   {FormatMinutes(b.TimeToFullCharge)}");
            Console.WriteLine($"Battery Health:        {b.BatteryHealth ?? "N/A"}");
            Console.WriteLine($"Health Condition:      {b.BatteryHealthCondition ?? "N/A"}");
            Console.WriteLine($"Design Cycle Count:    {(b.DesignCycleCount >= 0 ? b.DesignCycleCount.ToString(CultureInfo.InvariantCulture) : "N/A")}");
        }
    }

    private static string FormatMinutes(int minutes) => minutes switch
    {
        -1 => "Unknown",
        _ => $"{minutes} min"
    };
}

// バッテリー情報
internal sealed record BatteryInfo
{
    // バッテリー名称
    public required string Name { get; init; }

    // バッテリー種別 (InternalBattery等)
    public required string Type { get; init; }

    // 接続方式 (Internal, USB等)
    public string? TransportType { get; init; }

    // シリアル番号
    public string? HardwareSerialNumber { get; init; }

    // バッテリー有無
    public required bool IsPresent { get; init; }

    // 電源の状態 ("AC Power", "Battery Power", "Off Line")
    public required string PowerSourceState { get; init; }

    // AC電源接続中か (PowerSourceStateから算出)
    public bool IsACConnected => string.Equals(PowerSourceState, kIOPSACPowerValue, StringComparison.Ordinal);

    // 充電中か
    public required bool IsCharging { get; init; }

    // 充電完了か
    public required bool IsCharged { get; init; }

    // 現在の容量
    public required int CurrentCapacity { get; init; }

    // 最大容量
    public required int MaxCapacity { get; init; }

    // バッテリーパーセント (0-100、MaxCapacityから算出)
    public int BatteryPercent => MaxCapacity > 0 ? (int)(100.0 * CurrentCapacity / MaxCapacity) : 0;

    // 空になるまでの時間(分)、不明の場合は-1
    public required int TimeToEmpty { get; init; }

    // フル充電までの時間(分)、不明の場合は-1
    public required int TimeToFullCharge { get; init; }

    // バッテリー健康状態 ("Good", "Fair", "Poor")
    public string? BatteryHealth { get; init; }

    // バッテリー健康状態の詳細条件 (正常時はnull、劣化時は "Check Battery" 等)
    public string? BatteryHealthCondition { get; init; }

    // 設計サイクル数、取得できない場合は-1
    public required int DesignCycleCount { get; init; }
}

// バッテリー情報取得
internal static class BatteryInfoProvider
{
    public static unsafe BatteryInfo[] GetBatteryInfo()
    {
        var blob = IOPSCopyPowerSourcesInfo();
        if (blob == IntPtr.Zero)
        {
            return [];
        }

        try
        {
            var sources = IOPSCopyPowerSourcesList(blob);
            if (sources == IntPtr.Zero)
            {
                return [];
            }

            try
            {
                var count = CFArrayGetCount(sources);
                var result = new List<BatteryInfo>();

                for (long i = 0; i < count; i++)
                {
                    var ps = CFArrayGetValueAtIndex(sources, i);
                    var desc = IOPSGetPowerSourceDescription(blob, ps);
                    if (desc == IntPtr.Zero)
                    {
                        continue;
                    }

                    var info = new BatteryInfo
                    {
                        Name = GetStringValue(desc, kIOPSNameKey) ?? "Unknown",
                        Type = GetStringValue(desc, kIOPSTypeKey) ?? "Unknown",
                        TransportType = GetStringValue(desc, kIOPSTransportTypeKey),
                        HardwareSerialNumber = GetStringValue(desc, kIOPSHardwareSerialNumberKey),
                        IsPresent = GetBoolValue(desc, kIOPSIsPresentKey),
                        PowerSourceState = GetStringValue(desc, kIOPSPowerSourceStateKey) ?? "Unknown",
                        IsCharging = GetBoolValue(desc, kIOPSIsChargingKey),
                        IsCharged = GetBoolValue(desc, kIOPSIsChargedKey),
                        CurrentCapacity = GetIntValue(desc, kIOPSCurrentCapacityKey),
                        MaxCapacity = GetIntValue(desc, kIOPSMaxCapacityKey),
                        TimeToEmpty = GetIntValue(desc, kIOPSTimeToEmptyKey, -1),
                        TimeToFullCharge = GetIntValue(desc, kIOPSTimeToFullChargeKey, -1),
                        BatteryHealth = GetStringValue(desc, kIOPSBatteryHealthKey),
                        BatteryHealthCondition = GetStringValue(desc, kIOPSBatteryHealthConditionKey),
                        DesignCycleCount = GetIntValue(desc, kIOPSDesignCycleCountKey, -1),
                    };

                    result.Add(info);
                }

                return [.. result];
            }
            finally
            {
                CFRelease(sources);
            }
        }
        finally
        {
            CFRelease(blob);
        }
    }

    private static unsafe string? GetStringValue(nint dict, string keyName)
    {
        var key = CFStringCreateWithCString(IntPtr.Zero, keyName, kCFStringEncodingUTF8);
        if (key == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var value = CFDictionaryGetValue(dict, key);
            if (value == IntPtr.Zero)
            {
                return null;
            }

            var buffer = stackalloc byte[256];
            return CFStringGetCString(value, buffer, 256, kCFStringEncodingUTF8)
                ? Marshal.PtrToStringUTF8((nint)buffer)
                : null;
        }
        finally
        {
            CFRelease(key);
        }
    }

    private static int GetIntValue(nint dict, string keyName, int defaultValue = 0)
    {
        var key = CFStringCreateWithCString(IntPtr.Zero, keyName, kCFStringEncodingUTF8);
        if (key == IntPtr.Zero)
        {
            return defaultValue;
        }

        try
        {
            var value = CFDictionaryGetValue(dict, key);
            if (value == IntPtr.Zero)
            {
                return defaultValue;
            }

            return CFNumberGetValue(value, kCFNumberSInt32Type, out var result) ? result : defaultValue;
        }
        finally
        {
            CFRelease(key);
        }
    }

    private static bool GetBoolValue(nint dict, string keyName)
    {
        var key = CFStringCreateWithCString(IntPtr.Zero, keyName, kCFStringEncodingUTF8);
        if (key == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            var value = CFDictionaryGetValue(dict, key);
            return value != IntPtr.Zero && CFBooleanGetValue(value);
        }
        finally
        {
            CFRelease(key);
        }
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
    // IOPowerSources dictionary keys (IOPSKeys.h)
    public const string kIOPSNameKey = "Name";
    public const string kIOPSTypeKey = "Type";
    public const string kIOPSTransportTypeKey = "Transport Type";
    public const string kIOPSHardwareSerialNumberKey = "Hardware Serial Number";
    public const string kIOPSIsPresentKey = "Is Present";
    public const string kIOPSPowerSourceStateKey = "Power Source State";
    public const string kIOPSIsChargingKey = "Is Charging";
    public const string kIOPSIsChargedKey = "Is Charged";
    public const string kIOPSCurrentCapacityKey = "Current Capacity";
    public const string kIOPSMaxCapacityKey = "Max Capacity";
    public const string kIOPSTimeToEmptyKey = "Time to Empty";
    public const string kIOPSTimeToFullChargeKey = "Time to Full Charge";
    public const string kIOPSBatteryHealthKey = "BatteryHealth";
    public const string kIOPSBatteryHealthConditionKey = "BatteryHealthCondition";
    public const string kIOPSDesignCycleCountKey = "DesignCycleCount9C";

    // IOPowerSources state values (IOPSKeys.h)
    public const string kIOPSACPowerValue = "AC Power";

    // CFStringEncoding (CFString.h)
    public const uint kCFStringEncodingUTF8 = 0x08000100;

    // CFNumberType (CFNumber.h)
    public const long kCFNumberSInt32Type = 3;

    //------------------------------------------------------------------------
    // CoreFoundation
    //------------------------------------------------------------------------

    private const string CoreFoundationLib = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [DllImport(CoreFoundationLib)]
    public static extern void CFRelease(nint cf);

    [DllImport(CoreFoundationLib)]
    public static extern long CFArrayGetCount(nint theArray);

    [DllImport(CoreFoundationLib)]
    public static extern nint CFArrayGetValueAtIndex(nint theArray, long idx);

    [DllImport(CoreFoundationLib)]
    public static extern nint CFDictionaryGetValue(nint theDict, nint key);

    [DllImport(CoreFoundationLib)]
    public static extern nint CFStringCreateWithCString(nint alloc, [MarshalAs(UnmanagedType.LPUTF8Str)] string cStr, uint encoding);

    [DllImport(CoreFoundationLib)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern unsafe bool CFStringGetCString(nint theString, byte* buffer, long bufferSize, uint encoding);

    [DllImport(CoreFoundationLib)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool CFNumberGetValue(nint number, long theType, out int value);

    [DllImport(CoreFoundationLib)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool CFBooleanGetValue(nint boolean);

    //------------------------------------------------------------------------
    // IOKit Power Sources
    //------------------------------------------------------------------------

    private const string IOKitLib = "/System/Library/Frameworks/IOKit.framework/IOKit";

    [DllImport(IOKitLib)]
    public static extern nint IOPSCopyPowerSourcesInfo();

    [DllImport(IOKitLib)]
    public static extern nint IOPSCopyPowerSourcesList(nint blob);

    // 戻り値はunowned参照のため、CFReleaseしないこと
    [DllImport(IOKitLib)]
    public static extern nint IOPSGetPowerSourceDescription(nint blob, nint ps);
}
