namespace MacDotNet.SystemInfo;

#pragma warning disable CA1024
public static class PlatformProvider
{
    public static UptimeInfo GetUptime() => new();

    public static LoadAverageInfo GetLoadAverage() => new();

    public static MemoryInfo GetMemory() => new();

    public static SwapInfo GetSwap() => new();

    public static CpuUsageInfo GetCpuUsage() => new();

    public static CpuLoadInfo GetCpuLoad() => CpuLoadInfo.Create();

    public static IReadOnlyList<FileSystemEntry> GetFileSystems() => FileSystemInfo.GetFileSystems();

    public static IReadOnlyList<NetworkInterfaceEntry> GetNetworkInterfaces() => NetworkInfo.GetNetworkInterfaces();

    public static IReadOnlyList<ProcessEntry> GetProcesses() => ProcessInfo.GetProcesses();

    public static IReadOnlyList<GpuEntry> GetGpus() => GpuInfo.GetGpus();

    public static HardwareInfo GetHardware() => HardwareInfo.Create();

    public static IReadOnlyList<PerformanceLevelEntry> GetPerformanceLevels() => HardwareInfo.GetPerformanceLevels();

    public static KernelInfo GetKernel() => KernelInfo.Create();

    public static BatteryInfo GetBattery() => new();

    public static BatteryDetailInfo GetBatteryDetail() => BatteryDetailInfo.Create();

    public static AppleSiliconPowerInfo GetAppleSiliconPower() => AppleSiliconPowerInfo.Create();

    public static IReadOnlyList<SmcSensorReading> GetTemperatureSensors() => SmcInfo.GetTemperatureSensors();

    public static IReadOnlyList<SmcSensorReading> GetPowerReadings() => SmcInfo.GetPowerReadings();

    public static IReadOnlyList<SmcSensorReading> GetVoltageReadings() => SmcInfo.GetVoltageReadings();

    public static IReadOnlyList<SmcFanEntry> GetFans() => SmcInfo.GetFanInfo();

    public static double? GetTotalSystemPower() => SmcInfo.GetTotalSystemPower();
}

