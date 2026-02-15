using MacDotNet.SystemInfo.Lab;

Console.WriteLine("=== MacDotNet.SystemInfo.Lab ===");
Console.WriteLine();

// 1. CPU Load Info
Console.WriteLine("### 1. CPU Load Info ###");
var cpuLoad = CpuLoadInfo.Create();
cpuLoad.Update();
Thread.Sleep(500);
cpuLoad.Update();

Console.WriteLine($"  Logical CPU:      {cpuLoad.LogicalCpu}");
Console.WriteLine($"  Physical CPU:     {cpuLoad.PhysicalCpu}");
Console.WriteLine($"  Hyperthreading:   {cpuLoad.HasHyperthreading}");
Console.WriteLine($"  User Load:        {cpuLoad.UserLoad:P2}");
Console.WriteLine($"  System Load:      {cpuLoad.SystemLoad:P2}");
Console.WriteLine($"  Idle Load:        {cpuLoad.IdleLoad:P2}");
Console.WriteLine($"  Total Load:       {cpuLoad.TotalLoad:P2}");
Console.WriteLine($"  Cores:            {cpuLoad.UsagePerCore.Length}");
for (var i = 0; i < Math.Min(cpuLoad.UsagePerCore.Length, 8); i++)
{
    Console.WriteLine($"    Core {i}: {cpuLoad.UsagePerCore[i]:P2}");
}
if (cpuLoad.ECoreUsage is not null)
{
    Console.WriteLine($"  E-Core Average:   {cpuLoad.ECoreUsage:P2}");
}
if (cpuLoad.PCoreUsage is not null)
{
    Console.WriteLine($"  P-Core Average:   {cpuLoad.PCoreUsage:P2}");
}
Console.WriteLine();

// 2. Memory Pressure
Console.WriteLine("### 2. Memory Pressure Info ###");
var memPressure = MemoryPressureInfo.Create();
Console.WriteLine($"  Level:     {memPressure.Level}");
Console.WriteLine($"  Pressure:  {memPressure.PressureName}");
Console.WriteLine();

// 3. GPU Detail Info
Console.WriteLine("### 3. GPU Detail Info ###");
var gpus = GpuDetailInfo.GetGpuDetails();
foreach (var gpu in gpus)
{
    Console.WriteLine($"  [{gpu.Id}] {gpu.Model ?? "Unknown"}");
    Console.WriteLine($"    IOClass:           {gpu.IOClass}");
    if (gpu.Utilization is not null)
    {
        Console.WriteLine($"    Utilization:       {gpu.Utilization:P0}");
    }
    if (gpu.RenderUtilization is not null)
    {
        Console.WriteLine($"    Render Util:       {gpu.RenderUtilization:P0}");
    }
    if (gpu.TilerUtilization is not null)
    {
        Console.WriteLine($"    Tiler Util:        {gpu.TilerUtilization:P0}");
    }
    if (gpu.Temperature is not null)
    {
        Console.WriteLine($"    Temperature:       {gpu.Temperature}°C");
    }
    if (gpu.FanSpeed is not null)
    {
        Console.WriteLine($"    Fan Speed:         {gpu.FanSpeed}%");
    }
    if (gpu.CoreClock is not null)
    {
        Console.WriteLine($"    Core Clock:        {gpu.CoreClock} MHz");
    }
    if (gpu.MemoryClock is not null)
    {
        Console.WriteLine($"    Memory Clock:      {gpu.MemoryClock} MHz");
    }
    if (gpu.PowerState is not null)
    {
        Console.WriteLine($"    Power State:       {(gpu.PowerState.Value ? "Active" : "Off")}");
    }
}
Console.WriteLine();

// 4. Network Detail Info
Console.WriteLine("### 4. Network Detail Info ###");
var primaryIface = NetworkDetailInfo.GetPrimaryInterface();
Console.WriteLine($"  Primary Interface: {primaryIface ?? "N/A"}");

var interfaces = NetworkDetailInfo.GetNetworkInterfaces();
foreach (var iface in interfaces)
{
    Console.WriteLine($"  [{iface.BsdName}] {iface.DisplayName}");
    Console.WriteLine($"    Type:        {iface.ConnectionType}");
    Console.WriteLine($"    MAC:         {iface.MacAddress}");
    Console.WriteLine($"    Primary:     {iface.IsPrimary}");
    Console.WriteLine($"    Baud Rate:   {iface.BaudRate / 1_000_000.0:F0} Mbps");
    if (!string.IsNullOrEmpty(iface.LocalIpV4))
    {
        Console.WriteLine($"    IPv4:        {iface.LocalIpV4}");
    }
    if (!string.IsNullOrEmpty(iface.LocalIpV6))
    {
        Console.WriteLine($"    IPv6:        {iface.LocalIpV6}");
    }
}
Console.WriteLine();

// 5. Battery Detail Info
Console.WriteLine("### 5. Battery Detail Info ###");
var battery = BatteryDetailInfo.Create();
if (battery.Supported)
{
    Console.WriteLine($"  Voltage:           {battery.Voltage:F3} V");
    Console.WriteLine($"  Amperage:          {battery.Amperage} mA");
    Console.WriteLine($"  Temperature:       {battery.Temperature:F1}°C");
    Console.WriteLine($"  Cycle Count:       {battery.CycleCount}");
    Console.WriteLine($"  Current Capacity:  {battery.CurrentCapacity} mAh");
    Console.WriteLine($"  Design Capacity:   {battery.DesignCapacity} mAh");
    Console.WriteLine($"  Max Capacity:      {battery.MaxCapacity} mAh");
    Console.WriteLine($"  Health:            {battery.Health}%");
    Console.WriteLine($"  AC Watts:          {battery.AcWatts} W");
    Console.WriteLine($"  Charging Current:  {battery.ChargingCurrent} mA");
    Console.WriteLine($"  Charging Voltage:  {battery.ChargingVoltage} mV");
    Console.WriteLine($"  Optimized Charge:  {battery.OptimizedChargingEngaged}");
}
else
{
    Console.WriteLine("  Battery not supported");
}
Console.WriteLine();

// 6. Apple Silicon Power Info
Console.WriteLine("### 6. Apple Silicon Power Info ###");
var power = AppleSiliconPowerInfo.Create();
if (power.Supported)
{
    power.Update();
    Thread.Sleep(500);
    power.Update();

    Console.WriteLine($"  CPU Power:   {power.CpuPower:F2} W");
    Console.WriteLine($"  GPU Power:   {power.GpuPower:F2} W");
    Console.WriteLine($"  ANE Power:   {power.AnePower:F2} W");
    Console.WriteLine($"  RAM Power:   {power.RamPower:F2} W");
    Console.WriteLine($"  Total:       {power.TotalPower:F2} W");
}
else
{
    Console.WriteLine("  Apple Silicon not detected");
}

// Fan info
var (fanCount, fanModes) = SensorDetailInfo.GetFanModes();
if (fanCount > 0)
{
    Console.WriteLine($"  Fan Count:   {fanCount}");
    foreach (var (id, mode) in fanModes)
    {
        Console.WriteLine($"    Fan {id}: {mode}");
    }
}

var fastestFan = SensorDetailInfo.GetFastestFan();
if (fastestFan is not null)
{
    Console.WriteLine($"  Fastest Fan: #{fastestFan.Value.id} @ {fastestFan.Value.rpm:F0} RPM");
}

var totalPower = SensorDetailInfo.GetTotalSystemPower();
if (totalPower is not null)
{
    Console.WriteLine($"  Total System Power (PSTR): {totalPower:F2} W");
}
Console.WriteLine();

// 7. Disk Detail Info
Console.WriteLine("### 7. Disk Detail Info ###");
var diskIo = DiskDetailInfo.GetDiskIoStats();
foreach (var disk in diskIo)
{
    var deviceLabel = disk.ProductName ?? "Unknown";
    var locationLabel = disk.Location is not null ? $", {disk.Location}" : string.Empty;
    var interconnectLabel = disk.Interconnect is not null ? $" ({disk.Interconnect}{locationLabel})" : string.Empty;
    Console.WriteLine($"  [{disk.BsdName}] {deviceLabel}{interconnectLabel}");
    Console.WriteLine($"    Read:  {disk.ReadBytes / (1024.0 * 1024.0):F2} MB ({disk.ReadOperations} ops)");
    Console.WriteLine($"    Write: {disk.WriteBytes / (1024.0 * 1024.0):F2} MB ({disk.WriteOperations} ops)");
}
Console.WriteLine();

// 8. System Detail Info
Console.WriteLine("### 8. System Detail Info ###");
var model = SystemDetailInfo.GetModelInfo();
Console.WriteLine($"  Model ID:      {model.ModelId}");
Console.WriteLine($"  Serial Number: {model.SerialNumber}");

var clusters = SystemDetailInfo.GetCoreClusterInfo();
if (clusters.Length > 0)
{
    Console.WriteLine($"  Core Clusters (Apple Silicon):");
    foreach (var cluster in clusters)
    {
        Console.WriteLine($"    [{cluster.PerfLevel}] {cluster.Name}: {cluster.PhysicalCpu}C/{cluster.LogicalCpu}T, L2={cluster.L2CacheSize / (1024 * 1024)}MB");
    }
}

Console.WriteLine();
Console.WriteLine("=== Done ===");
