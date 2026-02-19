namespace MacDotNet.SystemInfo;

#pragma warning disable CA1024
public static class PlatformProvider
{
    //--------------------------------------------------------------------------------
    // System
    //--------------------------------------------------------------------------------

    public static KernelInfo GetKernel() => KernelInfo.Create();

    public static Uptime GetUptime() => new();

    public static HardwareInfo GetHardware() => HardwareInfo.Create();

    public static IReadOnlyList<PerformanceLevelEntry> GetPerformanceLevels() => HardwareInfo.GetPerformanceLevels();

    //--------------------------------------------------------------------------------
    // Load
    //--------------------------------------------------------------------------------

    public static LoadAverage GetLoadAverage() => new();

    public static CpuUsageStat GetCpuUsageStat() => CpuUsageStat.Create();

    //--------------------------------------------------------------------------------
    // Memory
    //--------------------------------------------------------------------------------

    public static MemoryStat GetMemoryStat() => new();

    public static SwapStat GetSwapStat() => new();

    //--------------------------------------------------------------------------------
    // Storage
    //--------------------------------------------------------------------------------

    public static IReadOnlyList<FileSystemEntry> GetFileSystems() => FileSystemInfo.GetFileSystems();

    //--------------------------------------------------------------------------------
    // Network
    //--------------------------------------------------------------------------------

    public static IReadOnlyList<NetworkInterfaceEntry> GetNetworkInterfaces() => NetworkInfo.GetNetworkInterfaces();

    //--------------------------------------------------------------------------------
    // Process
    //--------------------------------------------------------------------------------

    public static IReadOnlyList<ProcessEntry> GetProcesses() => ProcessInfo.GetProcesses();

    //--------------------------------------------------------------------------------
    // GPU
    //--------------------------------------------------------------------------------

    public static IReadOnlyList<GpuEntry> GetGpus() => GpuInfo.GetGpus();

    //--------------------------------------------------------------------------------
    // Power
    //--------------------------------------------------------------------------------

    public static Battery GetBattery() => new();

    public static BatteryDetail GetBatteryDetail() => BatteryDetail.Create();

    public static AppleSiliconPower GetAppleSiliconPower() => AppleSiliconPower.Create();

    //--------------------------------------------------------------------------------
    // Sensor
    //--------------------------------------------------------------------------------

    public static IReadOnlyList<SmcSensorReading> GetTemperatureSensors() => SmcInfo.GetTemperatureSensors();

    public static IReadOnlyList<SmcSensorReading> GetPowerReadings() => SmcInfo.GetPowerReadings();

    public static IReadOnlyList<SmcSensorReading> GetVoltageReadings() => SmcInfo.GetVoltageReadings();

    public static IReadOnlyList<SmcFanEntry> GetFans() => SmcInfo.GetFanInfo();

    public static double? GetTotalSystemPower() => SmcInfo.GetTotalSystemPower();
}

