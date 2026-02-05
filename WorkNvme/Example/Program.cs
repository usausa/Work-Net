using LinuxDotNet.Disk;

namespace Example;

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine("=== Linux Disk Information ===\n");

        var disks = DiskInfo.GetInformation();

        if (disks.Count == 0)
        {
            Console.WriteLine("No physical disks found.");
            Console.WriteLine("Note: This program requires root privileges to read SMART data.");
            return;
        }

        foreach (var disk in disks)
        {
            PrintDiskInfo(disk);
            Console.WriteLine();
        }

        Console.WriteLine($"Total: {disks.Count} disk(s) found.");
    }

    private static void PrintDiskInfo(IDiskInfo disk)
    {
        Console.WriteLine($"Disk #{disk.Index}: {disk.DeviceName}");
        Console.WriteLine($"  Model:          {(string.IsNullOrEmpty(disk.Model) ? "N/A" : disk.Model)}");
        Console.WriteLine($"  Serial:         {(string.IsNullOrEmpty(disk.SerialNumber) ? "N/A" : disk.SerialNumber)}");
        Console.WriteLine($"  Firmware:       {(string.IsNullOrEmpty(disk.FirmwareRevision) ? "N/A" : disk.FirmwareRevision)}");
        Console.WriteLine($"  Size:           {FormatSize(disk.Size)}");
        Console.WriteLine($"  Disk Type:      {disk.DiskType}");
        Console.WriteLine($"  Removable:      {disk.Removable}");
        Console.WriteLine($"  SMART Type:     {disk.SmartType}");

        // Print partitions
        var partitions = disk.GetPartitions().ToList();
        if (partitions.Count > 0)
        {
            Console.WriteLine($"  Partitions:");
            foreach (var partition in partitions)
            {
                var mountInfo = !string.IsNullOrEmpty(partition.MountPoint)
                    ? $" -> {partition.MountPoint} ({partition.FileSystem})"
                    : string.Empty;
                Console.WriteLine($"    {partition.Name}: {FormatSize(partition.Size)}{mountInfo}");
            }
        }

        // Print SMART data
        if (disk.SmartType == SmartType.Nvme)
        {
            PrintNvmeSmart((ISmartNvme)disk.Smart);
        }
        else if (disk.SmartType == SmartType.Generic)
        {
            PrintGenericSmart((ISmartGeneric)disk.Smart);
        }
    }

    private static void PrintNvmeSmart(ISmartNvme smart)
    {
        Console.WriteLine("  SMART (NVMe):");
        Console.WriteLine($"    Temperature:              {smart.Temperature}°C");
        Console.WriteLine($"    Available Spare:          {smart.AvailableSpare}%");
        Console.WriteLine($"    Percentage Used:          {smart.PercentageUsed}%");
        Console.WriteLine($"    Data Units Read:          {smart.DataUnitRead} ({FormatDataUnits(smart.DataUnitRead)})");
        Console.WriteLine($"    Data Units Written:       {smart.DataUnitWritten} ({FormatDataUnits(smart.DataUnitWritten)})");
        Console.WriteLine($"    Power Cycles:             {smart.PowerCycles}");
        Console.WriteLine($"    Power On Hours:           {smart.PowerOnHours}");
        Console.WriteLine($"    Unsafe Shutdowns:         {smart.UnsafeShutdowns}");
        Console.WriteLine($"    Media Errors:             {smart.MediaErrors}");
    }

    private static void PrintGenericSmart(ISmartGeneric smart)
    {
        Console.WriteLine("  SMART (Generic):");
        Console.WriteLine("    ID   FLAG   CUR  WOR  RAW");
        Console.WriteLine("    ---  ----   ---  ---  --------");

        foreach (var id in smart.GetSupportedIds())
        {
            var attr = smart.GetAttribute(id);
            if (attr.HasValue)
            {
                Console.WriteLine($"    {(byte)id,3}  0x{attr.Value.Flags:X4} {attr.Value.CurrentValue,3}  {attr.Value.WorstValue,3}  {attr.Value.RawValue}");
            }
        }
    }

    private static string FormatSize(ulong bytes)
    {
        const ulong TB = 1UL << 40;
        const ulong GB = 1UL << 30;
        const ulong MB = 1UL << 20;

        if (bytes >= TB)
        {
            return $"{(double)bytes / TB:F2} TB";
        }
        if (bytes >= GB)
        {
            return $"{(double)bytes / GB:F2} GB";
        }
        if (bytes >= MB)
        {
            return $"{(double)bytes / MB:F2} MB";
        }
        return $"{bytes} bytes";
    }

    private static string FormatDataUnits(ulong units)
    {
        // 1 data unit = 512 * 1000 bytes
        var bytes = units * 512 * 1000;
        return FormatSize(bytes);
    }
}
