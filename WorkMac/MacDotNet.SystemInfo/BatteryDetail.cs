namespace MacDotNet.SystemInfo;

using static MacDotNet.SystemInfo.NativeMethods;

public sealed class BatteryDetail
{
    public bool Supported { get; private set; }

    public double Voltage { get; private set; }

    public int Amperage { get; private set; }

    public double Temperature { get; private set; }

    public int CycleCount { get; private set; }

    public int CurrentCapacity { get; private set; }

    public int DesignCapacity { get; private set; }

    public int MaxCapacity { get; private set; }

    public int Health { get; private set; }

    public int AcWatts { get; private set; }

    public int ChargingCurrent { get; private set; }

    public int ChargingVoltage { get; private set; }

    public bool OptimizedChargingEngaged { get; private set; }

    private readonly uint batteryService;

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private BatteryDetail()
    {
        var matching = IOServiceMatching("AppleSmartBattery");
        if (matching != IntPtr.Zero)
        {
            batteryService = IOServiceGetMatchingService(0, matching);
            Supported = batteryService != 0;
        }

        if (Supported)
        {
            Update();
        }
    }

    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    public static BatteryDetail Create() => new();

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public bool Update()
    {
        if (!Supported)
        {
            return false;
        }

        AcWatts = GetAcAdapterWatts();

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

        var chargerData = GetChargerData();
        if (chargerData is not null)
        {
            ChargingCurrent = chargerData.Value.current;
            ChargingVoltage = chargerData.Value.voltage;
        }

        OptimizedChargingEngaged = GetPropertyInt("OptimizedBatteryChargingEngaged") == 1;

        return true;
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

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

    private static bool IsArmMac() =>
        System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture ==
        System.Runtime.InteropServices.Architecture.Arm64;
}
