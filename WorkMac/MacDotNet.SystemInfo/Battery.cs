namespace MacDotNet.SystemInfo;

using System.Runtime.InteropServices;

using static MacDotNet.SystemInfo.NativeMethods;

public sealed class Battery
{
    public DateTime UpdateAt { get; private set; }

    public bool Supported { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Type { get; private set; } = string.Empty;

    public string? TransportType { get; private set; }

    public string? HardwareSerialNumber { get; private set; }

    public bool IsPresent { get; private set; }

    public string PowerSourceState { get; private set; } = string.Empty;

    public bool IsACConnected => string.Equals(PowerSourceState, kIOPSACPowerValue, StringComparison.Ordinal);

    public bool IsCharging { get; private set; }

    public bool IsCharged { get; private set; }

    public int CurrentCapacity { get; private set; }

    public int MaxCapacity { get; private set; }

    public int BatteryPercent => MaxCapacity > 0 ? (int)(100.0 * CurrentCapacity / MaxCapacity) : 0;

    public int TimeToEmpty { get; private set; } = -1;

    public int TimeToFullCharge { get; private set; } = -1;

    public string? BatteryHealth { get; private set; }

    public string? BatteryHealthCondition { get; private set; }

    public int DesignCycleCount { get; private set; } = -1;

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal Battery()
    {
        Update();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    public unsafe bool Update()
    {
        var blob = IOPSCopyPowerSourcesInfo();
        if (blob == IntPtr.Zero)
        {
            Supported = false;
            return false;
        }

        try
        {
            var sources = IOPSCopyPowerSourcesList(blob);
            if (sources == IntPtr.Zero)
            {
                Supported = false;
                return false;
            }

            try
            {
                var count = CFArrayGetCount(sources);
                if (count == 0)
                {
                    Supported = false;
                    return false;
                }

                var ps = CFArrayGetValueAtIndex(sources, 0);
                var desc = IOPSGetPowerSourceDescription(blob, ps);
                if (desc == IntPtr.Zero)
                {
                    Supported = false;
                    return false;
                }

                Supported = true;
                Name = GetStringValue(desc, kIOPSNameKey) ?? "Unknown";
                Type = GetStringValue(desc, kIOPSTypeKey) ?? "Unknown";
                TransportType = GetStringValue(desc, kIOPSTransportTypeKey);
                HardwareSerialNumber = GetStringValue(desc, kIOPSHardwareSerialNumberKey);
                IsPresent = GetBoolValue(desc, kIOPSIsPresentKey);
                PowerSourceState = GetStringValue(desc, kIOPSPowerSourceStateKey) ?? "Unknown";
                IsCharging = GetBoolValue(desc, kIOPSIsChargingKey);
                IsCharged = GetBoolValue(desc, kIOPSIsChargedKey);
                CurrentCapacity = GetIntValue(desc, kIOPSCurrentCapacityKey);
                MaxCapacity = GetIntValue(desc, kIOPSMaxCapacityKey);
                TimeToEmpty = GetIntValue(desc, kIOPSTimeToEmptyKey, -1);
                TimeToFullCharge = GetIntValue(desc, kIOPSTimeToFullChargeKey, -1);
                BatteryHealth = GetStringValue(desc, kIOPSBatteryHealthKey);
                BatteryHealthCondition = GetStringValue(desc, kIOPSBatteryHealthConditionKey);
                DesignCycleCount = GetIntValue(desc, kIOPSDesignCycleCountKey, -1);

                UpdateAt = DateTime.Now;

                return true;
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

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static unsafe string? GetStringValue(IntPtr dict, string keyName)
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
                ? Marshal.PtrToStringUTF8((IntPtr)buffer)
                : null;
        }
        finally
        {
            CFRelease(key);
        }
    }

    private static int GetIntValue(IntPtr dict, string keyName, int defaultValue = 0)
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

    private static bool GetBoolValue(IntPtr dict, string keyName)
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
