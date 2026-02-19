namespace MacDotNet.SystemInfo.Lab;

using static NativeMethods;

/// <summary>
/// バッテリー詳細情報
/// </summary>
public sealed class BatteryDetailInfo
{
    /// <summary>
    /// サポートされているか
    /// </summary>
    public bool Supported { get; private set; }

    /// <summary>
    /// 電圧 (V)
    /// </summary>
    public double Voltage { get; private set; }

    /// <summary>
    /// 電流 (mA)
    /// </summary>
    public int Amperage { get; private set; }

    /// <summary>
    /// 温度 (℃)
    /// </summary>
    public double Temperature { get; private set; }

    /// <summary>
    /// サイクル数
    /// </summary>
    public int CycleCount { get; private set; }

    /// <summary>
    /// 現在の容量 (mAh)
    /// </summary>
    public int CurrentCapacity { get; private set; }

    /// <summary>
    /// 設計容量 (mAh)
    /// </summary>
    public int DesignCapacity { get; private set; }

    /// <summary>
    /// 最大容量 (mAh)
    /// </summary>
    public int MaxCapacity { get; private set; }

    /// <summary>
    /// バッテリー健康度 (%)
    /// </summary>
    public int Health { get; private set; }

    /// <summary>
    /// AC電源ワット数
    /// </summary>
    public int AcWatts { get; private set; }

    /// <summary>
    /// 充電電流 (mA)
    /// </summary>
    public int ChargingCurrent { get; private set; }

    /// <summary>
    /// 充電電圧 (mV)
    /// </summary>
    public int ChargingVoltage { get; private set; }

    /// <summary>
    /// 最適化充電が有効
    /// </summary>
    public bool OptimizedChargingEngaged { get; private set; }

    private uint batteryService;

    private BatteryDetailInfo()
    {
        var matching = IOServiceMatching("AppleSmartBattery");
        if (matching != IntPtr.Zero)
        {
            batteryService = IOServiceGetMatchingService(0, matching);
            if (batteryService != 0)
            {
                Update();
                Supported = DesignCapacity > 0;
            }
        }
    }

    public static BatteryDetailInfo Create() => new();

    public bool Update()
    {
        if (!Supported)
        {
            return false;
        }

        // IOPowerSources経由でAC電源情報
        AcWatts = GetAcAdapterWatts();

        // IORegistry経由でバッテリー詳細
        Voltage = GetPropertyDouble("Voltage") / 1000.0;
        Amperage = GetPropertyInt("Amperage");
        Temperature = GetPropertyDouble("Temperature") / 100.0;
        CycleCount = GetPropertyInt("CycleCount");
        CurrentCapacity = GetPropertyInt("AppleRawCurrentCapacity");
        DesignCapacity = GetPropertyInt("DesignCapacity");
        MaxCapacity = GetPropertyInt(IsArmMac() ? "AppleRawMaxCapacity" : "MaxCapacity");

        if (DesignCapacity > 0)
        {
            Health = (int)Math.Round(100.0 * MaxCapacity / DesignCapacity);
        }

        // ChargerData
        var chargerData = GetChargerData();
        if (chargerData is not null)
        {
            ChargingCurrent = chargerData.Value.current;
            ChargingVoltage = chargerData.Value.voltage;
        }

        // 最適化充電
        OptimizedChargingEngaged = GetPropertyInt("OptimizedBatteryChargingEngaged") == 1;

        return true;
    }

    private int GetPropertyInt(string name)
    {
        var keyPtr = CFStringCreateWithCString(IntPtr.Zero, name, kCFStringEncodingUTF8);
        var valuePtr = IORegistryEntryCreateCFProperty(batteryService, keyPtr, IntPtr.Zero, 0);
        CFRelease(keyPtr);

        if (valuePtr == IntPtr.Zero)
        {
            return 0;
        }

        try
        {
            if (CFGetTypeID(valuePtr) == CFNumberGetTypeID())
            {
                if (CFNumberGetValue(valuePtr, kCFNumberSInt32Type, out var result))
                {
                    return result;
                }
            }
        }
        finally
        {
            CFRelease(valuePtr);
        }

        return 0;
    }

    private double GetPropertyDouble(string name)
    {
        var keyPtr = CFStringCreateWithCString(IntPtr.Zero, name, kCFStringEncodingUTF8);
        var valuePtr = IORegistryEntryCreateCFProperty(batteryService, keyPtr, IntPtr.Zero, 0);
        CFRelease(keyPtr);

        if (valuePtr == IntPtr.Zero)
        {
            return 0;
        }

        try
        {
            if (CFGetTypeID(valuePtr) == CFNumberGetTypeID())
            {
                var result = 0.0;
                if (CFNumberGetValue(valuePtr, kCFNumberFloat64Type, ref result))
                {
                    return result;
                }

                // Fallback to int
                if (CFNumberGetValue(valuePtr, kCFNumberSInt32Type, out var intResult))
                {
                    return intResult;
                }
            }
        }
        finally
        {
            CFRelease(valuePtr);
        }

        return 0;
    }

    private (int current, int voltage)? GetChargerData()
    {
        var keyPtr = CFStringCreateWithCString(IntPtr.Zero, "ChargerData", kCFStringEncodingUTF8);
        var valuePtr = IORegistryEntryCreateCFProperty(batteryService, keyPtr, IntPtr.Zero, 0);
        CFRelease(keyPtr);

        if (valuePtr == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            if (CFGetTypeID(valuePtr) == CFDictionaryGetTypeID())
            {
                var currentKey = CFStringCreateWithCString(IntPtr.Zero, "ChargingCurrent", kCFStringEncodingUTF8);
                var currentPtr = CFDictionaryGetValue(valuePtr, currentKey);
                CFRelease(currentKey);

                var voltageKey = CFStringCreateWithCString(IntPtr.Zero, "ChargingVoltage", kCFStringEncodingUTF8);
                var voltagePtr = CFDictionaryGetValue(valuePtr, voltageKey);
                CFRelease(voltageKey);

                int current = 0, voltage = 0;
                if (currentPtr != IntPtr.Zero && CFNumberGetValue(currentPtr, kCFNumberSInt32Type, out var c))
                {
                    current = c;
                }

                if (voltagePtr != IntPtr.Zero && CFNumberGetValue(voltagePtr, kCFNumberSInt32Type, out var v))
                {
                    voltage = v;
                }

                return (current, voltage);
            }
        }
        finally
        {
            CFRelease(valuePtr);
        }

        return null;
    }

    private static int GetAcAdapterWatts()
    {
        var adapterDetails = IOPSCopyExternalPowerAdapterDetails();
        if (adapterDetails == IntPtr.Zero)
        {
            return 0;
        }

        try
        {
            var wattsKey = CFStringCreateWithCString(IntPtr.Zero, kIOPSPowerAdapterWattsKey, kCFStringEncodingUTF8);
            var wattsPtr = CFDictionaryGetValue(adapterDetails, wattsKey);
            CFRelease(wattsKey);

            if (wattsPtr != IntPtr.Zero && CFNumberGetValue(wattsPtr, kCFNumberSInt32Type, out var watts))
            {
                return watts;
            }
        }
        finally
        {
            CFRelease(adapterDetails);
        }

        return 0;
    }

    private static bool IsArmMac()
    {
        return System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64;
    }
}
