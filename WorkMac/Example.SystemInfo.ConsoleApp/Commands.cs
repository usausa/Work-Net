// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Example.SystemInfo.ConsoleApp;

using System.Globalization;

using MacDotNet.SystemInfo;

using Smart.CommandLine.Hosting;

public static class CommandBuilderExtensions
{
    public static void AddCommands(this ICommandBuilder commands)
    {
        commands.AddCommand<UptimeCommand>();
        commands.AddCommand<LoadAverageCommand>();
        commands.AddCommand<MemoryCommand>();
        commands.AddCommand<CpuCommand>();
        commands.AddCommand<FileSystemCommand>();
        commands.AddCommand<NetworkCommand>();
        commands.AddCommand<ProcessCommand>();
        commands.AddCommand<GpuCommand>();
        commands.AddCommand<HardwareCommand>();
        commands.AddCommand<KernelCommand>();
        commands.AddCommand<BatteryCommand>();
        commands.AddCommand<TemperatureCommand>();
        commands.AddCommand<FanCommand>();
        commands.AddCommand<PowerCommand>();
        commands.AddCommand<VoltageCommand>();
    }
}

// Uptime
[Command("uptime", "Uptime")]
public sealed class UptimeCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var uptime = PlatformProvider.GetUptime();
        Console.WriteLine($"Uptime: {uptime.Uptime}");

        return ValueTask.CompletedTask;
    }
}

// Load Average
[Command("load", "Load Average")]
public sealed class LoadAverageCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var load = PlatformProvider.GetLoadAverage();
        Console.WriteLine("=== Load Average ===");
        Console.WriteLine($"1 min:  {load.Average1:F2}");
        Console.WriteLine($"5 min:  {load.Average5:F2}");
        Console.WriteLine($"15 min: {load.Average15:F2}");

        return ValueTask.CompletedTask;
    }
}

// Memory
[Command("memory", "Memory Info")]
public sealed class MemoryCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var mem = PlatformProvider.GetMemory();
        Console.WriteLine("=== Memory Info ===");
        Console.WriteLine($"Physical Memory:  {FormatBytes(mem.PhysicalMemory)}");
        Console.WriteLine($"Page Size:        {mem.PageSize} bytes");
        Console.WriteLine($"Used:             {FormatBytes(mem.UsedBytes)}");
        Console.WriteLine($"Free:             {FormatBytes(mem.FreeBytes)}");
        Console.WriteLine($"Usage:            {mem.UsagePercent:F1}%");
        Console.WriteLine($"Active:           {FormatBytes(mem.ActiveBytes)}");
        Console.WriteLine($"Wired:            {FormatBytes(mem.WiredBytes)}");
        Console.WriteLine($"Compressor:       {FormatBytes(mem.CompressorBytes)}");

        var swap = PlatformProvider.GetSwap();
        if (swap.Supported)
        {
            Console.WriteLine();
            Console.WriteLine("=== Swap Info ===");
            Console.WriteLine($"Total:            {FormatBytes(swap.TotalBytes)}");
            Console.WriteLine($"Used:             {FormatBytes(swap.UsedBytes)}");
            Console.WriteLine($"Available:        {FormatBytes(swap.AvailableBytes)}");
            Console.WriteLine($"Usage:            {swap.UsagePercent:F1}%");
        }

        return ValueTask.CompletedTask;
    }

    private static string FormatBytes(ulong bytes) => bytes switch
    {
        >= 1UL << 40 => $"{bytes / (double)(1UL << 40):F2} TiB",
        >= 1UL << 30 => $"{bytes / (double)(1UL << 30):F2} GiB",
        >= 1UL << 20 => $"{bytes / (double)(1UL << 20):F2} MiB",
        >= 1UL << 10 => $"{bytes / (double)(1UL << 10):F2} KiB",
        _ => $"{bytes} B"
    };
}

// CPU
[Command("cpu", "CPU Info")]
public sealed class CpuCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var hw = PlatformProvider.GetHardware();
        Console.WriteLine("=== CPU Info ===");
        Console.WriteLine($"Logical CPU:    {hw.LogicalCpu}");
        Console.WriteLine($"Physical CPU:   {hw.PhysicalCpu}");
        Console.WriteLine($"Active CPU:     {hw.ActiveCpu}");
        if (hw.CpuBrandString is not null)
        {
            Console.WriteLine($"Brand:          {hw.CpuBrandString}");
        }

        if (hw.CpuFrequency > 0)
        {
            Console.WriteLine($"Frequency:      {hw.CpuFrequency / 1_000_000_000.0:F2} GHz");
        }

        Console.WriteLine();

        var load = PlatformProvider.GetLoadAverage();
        Console.WriteLine("=== Load Average ===");
        Console.WriteLine($"1 min:  {load.Average1:F2}");
        Console.WriteLine($"5 min:  {load.Average5:F2}");
        Console.WriteLine($"15 min: {load.Average15:F2}");

        return ValueTask.CompletedTask;
    }
}

// FileSystem
[Command("fs", "File System Info")]
public sealed class FileSystemCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var entries = PlatformProvider.GetFileSystems();
        if (entries.Count == 0)
        {
            Console.WriteLine("No file systems found.");
            return ValueTask.CompletedTask;
        }



        foreach (var fs in entries)
        {
            Console.WriteLine($"Mount Point:    {fs.MountPoint}");
            Console.WriteLine($"Device:         {fs.DeviceName}");
            Console.WriteLine($"Type:           {fs.TypeName}");
            Console.WriteLine($"Total Size:     {FormatBytes(fs.TotalSize)}");
            Console.WriteLine($"Free Size:      {FormatBytes(fs.FreeSize)}");
            Console.WriteLine($"Usage:          {fs.UsagePercent:F1}%");
            Console.WriteLine($"Read Only:      {fs.IsReadOnly}");
            Console.WriteLine();
        }

        return ValueTask.CompletedTask;
    }

    private static string FormatBytes(ulong bytes) => bytes switch
    {
        >= 1UL << 40 => $"{bytes / (double)(1UL << 40):F2} TiB",
        >= 1UL << 30 => $"{bytes / (double)(1UL << 30):F2} GiB",
        >= 1UL << 20 => $"{bytes / (double)(1UL << 20):F2} MiB",
        >= 1UL << 10 => $"{bytes / (double)(1UL << 10):F2} KiB",
        _ => $"{bytes} B"
    };
}

// Network
[Command("network", "Network Info")]
public sealed class NetworkCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var interfaces = PlatformProvider.GetNetworkInterfaces();
        if (interfaces.Count == 0)
        {
            Console.WriteLine("No network interfaces found.");
            return ValueTask.CompletedTask;
        }

        foreach (var iface in interfaces)
        {
            Console.WriteLine($"=== {iface.Name} ({iface.InterfaceTypeName}) ===");
            Console.WriteLine($"  State:           {iface.State}");
            if (iface.MacAddress is not null)
            {
                Console.WriteLine($"  MAC Address:     {iface.MacAddress}");
            }

            Console.WriteLine($"  MTU:             {iface.Mtu}");

            foreach (var addr in iface.IPv4Addresses)
            {
                Console.WriteLine($"  IPv4:            {addr.Address}/{addr.PrefixLength}");
            }

            foreach (var addr in iface.IPv6Addresses)
            {
                Console.WriteLine($"  IPv6:            {addr.Address}/{addr.PrefixLength}");
            }

            Console.WriteLine($"  Rx Bytes:        {iface.RxBytes:N0}");
            Console.WriteLine($"  Tx Bytes:        {iface.TxBytes:N0}");
            Console.WriteLine();
        }

        return ValueTask.CompletedTask;
    }
}

// Process
[Command("process", "Process Info")]
public sealed class ProcessCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var processes = PlatformProvider.GetProcesses();
        if (processes.Count == 0)
        {
            Console.WriteLine("No processes found.");
            return ValueTask.CompletedTask;
        }

        var totalThreads = 0;
        foreach (var p in processes)
        {
            totalThreads += p.ThreadCount;
        }

        Console.WriteLine($"Total Processes: {processes.Count}");
        Console.WriteLine($"Total Threads:   {totalThreads}");
        Console.WriteLine();

        Console.WriteLine($"{"PID",6} {"PPID",6} {"THR",4} {"RSS",10} {"NAME"}");
        Console.WriteLine(new string('-', 50));

        foreach (var p in processes.Take(20))
        {
            Console.WriteLine($"{p.Pid,6} {p.ParentPid,6} {p.ThreadCount,4} {FormatBytes(p.ResidentSize),10} {p.Name}");
        }

        if (processes.Count > 20)
        {
            Console.WriteLine($"... and {processes.Count - 20} more processes");
        }

        return ValueTask.CompletedTask;
    }

    private static string FormatBytes(ulong bytes) => bytes switch
    {
        >= 1UL << 30 => $"{bytes / (double)(1UL << 30):F1} GiB",
        >= 1UL << 20 => $"{bytes / (double)(1UL << 20):F1} MiB",
        >= 1UL << 10 => $"{bytes / (double)(1UL << 10):F1} KiB",
        _ => $"{bytes} B"
    };
}

// GPU
[Command("gpu", "GPU Info")]
public sealed class GpuCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var gpus = PlatformProvider.GetGpus();
        if (gpus.Count == 0)
        {
            Console.WriteLine("No GPU (IOAccelerator) found.");
            return ValueTask.CompletedTask;
        }

        for (var i = 0; i < gpus.Count; i++)
        {
            var gpu = gpus[i];

            Console.WriteLine($"=== GPU [{i}] ===");
            Console.WriteLine($"Model:            {gpu.Model}");
            Console.WriteLine($"Class:            {gpu.ClassName}");
            Console.WriteLine($"Core Count:       {gpu.CoreCount}");

            if (gpu.Performance is not null)
            {
                var perf = gpu.Performance;
                Console.WriteLine();
                Console.WriteLine("--- Performance ---");
                Console.WriteLine($"Device Utilization:   {perf.DeviceUtilization}%");
                Console.WriteLine($"Renderer Utilization: {perf.RendererUtilization}%");
                Console.WriteLine($"Tiler Utilization:    {perf.TilerUtilization}%");
            }

            Console.WriteLine();
        }

        return ValueTask.CompletedTask;
    }
}

// Hardware
[Command("hardware", "Hardware Info")]
public sealed class HardwareCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var hw = PlatformProvider.GetHardware();

        Console.WriteLine("=== Hardware ===");
        Console.WriteLine($"Model:            {hw.Model}");
        Console.WriteLine($"Machine:          {hw.Machine}");
        Console.WriteLine($"CPU Brand:        {hw.CpuBrandString ?? "(unavailable)"}");
        Console.WriteLine();

        Console.WriteLine("=== CPU ===");
        Console.WriteLine($"Physical CPU:     {hw.PhysicalCpu}");
        Console.WriteLine($"Logical CPU:      {hw.LogicalCpu}");
        Console.WriteLine($"Active CPU:       {hw.ActiveCpu}");
        Console.WriteLine();

        Console.WriteLine("=== Memory ===");
        Console.WriteLine($"Physical Memory:  {FormatBytes((ulong)hw.MemSize)}");
        Console.WriteLine($"Page Size:        {hw.PageSize} bytes");
        Console.WriteLine();

        var perfLevels = PlatformProvider.GetPerformanceLevels();
        if (perfLevels.Count > 0)
        {
            Console.WriteLine("=== Performance Levels (Apple Silicon) ===");
            foreach (var level in perfLevels)
            {
                Console.WriteLine($"  [{level.Index}] {level.Name}: {level.PhysicalCpu} physical, {level.LogicalCpu} logical");
            }
        }

        return ValueTask.CompletedTask;
    }

    private static string FormatBytes(ulong bytes) => bytes switch
    {
        >= 1UL << 40 => $"{bytes / (double)(1UL << 40):F2} TiB",
        >= 1UL << 30 => $"{bytes / (double)(1UL << 30):F2} GiB",
        >= 1UL << 20 => $"{bytes / (double)(1UL << 20):F2} MiB",
        >= 1UL << 10 => $"{bytes / (double)(1UL << 10):F2} KiB",
        _ => $"{bytes} B"
    };
}

// Kernel
[Command("kernel", "Kernel Info")]
public sealed class KernelCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var kern = PlatformProvider.GetKernel();

        Console.WriteLine("=== Kernel ===");
        Console.WriteLine($"OS Type:          {kern.OsType}");
        Console.WriteLine($"OS Release:       {kern.OsRelease}");
        Console.WriteLine($"OS Version:       {kern.OsVersion}");
        Console.WriteLine($"OS Product Ver:   {kern.OsProductVersion ?? "(unavailable)"}");
        Console.WriteLine();

        Console.WriteLine("=== Boot Time ===");
        if (kern.BootTime != DateTimeOffset.MinValue)
        {
            Console.WriteLine($"Boot Time:        {kern.BootTime:yyyy-MM-dd HH:mm:ss zzz}");
            Console.WriteLine($"Uptime:           {DateTimeOffset.UtcNow - kern.BootTime:d\\.hh\\:mm\\:ss}");
        }

        return ValueTask.CompletedTask;
    }

}

// Battery
[Command("battery", "Battery Info")]
public sealed class BatteryCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var battery = PlatformProvider.GetBattery();
        if (!battery.Supported)
        {
            Console.WriteLine("No battery found.");
            return ValueTask.CompletedTask;
        }

        Console.WriteLine("=== Battery ===");
        Console.WriteLine($"Name:                  {battery.Name}");
        Console.WriteLine($"Type:                  {battery.Type}");
        Console.WriteLine($"Is Present:            {battery.IsPresent}");
        Console.WriteLine($"Power Source State:    {battery.PowerSourceState}");
        Console.WriteLine($"Is Charging:           {battery.IsCharging}");
        Console.WriteLine($"Is Charged:            {battery.IsCharged}");
        Console.WriteLine($"Current Capacity:      {battery.CurrentCapacity}");
        Console.WriteLine($"Max Capacity:          {battery.MaxCapacity}");
        Console.WriteLine($"Battery Percent:       {battery.BatteryPercent}%");
        Console.WriteLine($"Time to Empty:         {FormatMinutes(battery.TimeToEmpty)}");
        Console.WriteLine($"Time to Full Charge:   {FormatMinutes(battery.TimeToFullCharge)}");
        Console.WriteLine($"Battery Health:        {battery.BatteryHealth ?? "N/A"}");
        Console.WriteLine($"Design Cycle Count:    {(battery.DesignCycleCount >= 0 ? battery.DesignCycleCount.ToString(CultureInfo.InvariantCulture) : "N/A")}");

        return ValueTask.CompletedTask;
    }

    private static string FormatMinutes(int minutes) => minutes switch
    {
        -1 => "Unknown",
        _ => $"{minutes} min"
    };
}

// Temperature
[Command("temp", "Temperature Sensors")]
public sealed class TemperatureCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var temps = PlatformProvider.GetTemperatureSensors();
        Console.WriteLine("=== Temperature Sensors ===");
        if (temps.Count > 0)
        {
            Console.WriteLine($"{"Key",-6} {"Value",8}  Description");
            Console.WriteLine(new string('-', 50));
            foreach (var s in temps.Take(30))
            {
                Console.WriteLine($"{s.Key,-6} {s.Value,7:F1} C  {s.Description}");
            }

            if (temps.Count > 30)
            {
                Console.WriteLine($"... and {temps.Count - 30} more sensors");
            }
        }
        else
        {
            Console.WriteLine("No temperature sensors found.");
        }

        return ValueTask.CompletedTask;
    }
}

// Fan
[Command("fan", "Fan Info")]
public sealed class FanCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var fans = PlatformProvider.GetFans();
        Console.WriteLine("=== Fan Info ===");
        if (fans.Count > 0)
        {
            Console.WriteLine($"Fan count: {fans.Count}");
            foreach (var fan in fans)
            {
                Console.WriteLine($"  Fan {fan.Index}:");
                Console.WriteLine($"    Actual:  {fan.ActualRpm:F0} RPM");
                Console.WriteLine($"    Min:     {fan.MinRpm:F0} RPM");
                Console.WriteLine($"    Max:     {fan.MaxRpm:F0} RPM");
                Console.WriteLine($"    Target:  {fan.TargetRpm:F0} RPM");
            }
        }
        else
        {
            Console.WriteLine("No fans found.");
        }

        return ValueTask.CompletedTask;
    }
}

// Power
[Command("power", "Power Readings")]
public sealed class PowerCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var powers = PlatformProvider.GetPowerReadings();
        Console.WriteLine("=== Power Readings ===");
        if (powers.Count > 0)
        {
            Console.WriteLine($"{"Key",-6} {"Value",8}  Description");
            Console.WriteLine(new string('-', 50));
            foreach (var p in powers)
            {
                Console.WriteLine($"{p.Key,-6} {p.Value,7:F2} W  {p.Description}");
            }
        }
        else
        {
            Console.WriteLine("No power readings found.");
        }

        return ValueTask.CompletedTask;
    }
}

// Voltage
[Command("voltage", "Voltage Readings")]
public sealed class VoltageCommand : ICommandHandler
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        var voltages = PlatformProvider.GetVoltageReadings();
        Console.WriteLine("=== Voltage Readings ===");
        if (voltages.Count > 0)
        {
            Console.WriteLine($"{"Key",-6} {"Value",8}  Description");
            Console.WriteLine(new string('-', 50));
            foreach (var v in voltages)
            {
                Console.WriteLine($"{v.Key,-6} {v.Value,7:F3} V  {v.Description}");
            }
        }
        else
        {
            Console.WriteLine("No voltage readings found.");
        }

        return ValueTask.CompletedTask;
    }
}

