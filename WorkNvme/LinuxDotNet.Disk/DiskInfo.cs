namespace LinuxDotNet.Disk;

using System.Runtime.Versioning;
using System.Text.RegularExpressions;

using static LinuxDotNet.Disk.NativeMethods;

[SupportedOSPlatform("linux")]
public static class DiskInfo
{
    private const string SysBlockPath = "/sys/block";

    public static IReadOnlyList<IDiskInfo> GetInformation()
    {
        var list = new List<IDiskInfo>();

        if (!Directory.Exists(SysBlockPath))
        {
            return list;
        }

        var directories = Directory.GetDirectories(SysBlockPath)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Cast<string>()
            .ToList();

        uint index = 0;
        foreach (var deviceName in directories)
        {
            // Skip loop, ram and dm devices
            if (deviceName.StartsWith("loop", StringComparison.Ordinal) ||
                deviceName.StartsWith("ram", StringComparison.Ordinal) ||
                deviceName.StartsWith("dm-", StringComparison.Ordinal))
            {
                continue;
            }

            // Only physical disks
            if (!IsPhysicalDisk(deviceName))
            {
                continue;
            }

            var (major, minor) = GetDeviceNumbers(deviceName);
            if (major == -1)
            {
                continue;
            }

            var busType = GetBusTypeFromMajor(major);
            var devicePath = $"/dev/{deviceName}";

            var info = new DiskInfoGeneric
            {
                Index = index++,
                DeviceName = devicePath,
                BusType = busType,
                Removable = ReadSysfsBool(Path.Combine(SysBlockPath, deviceName, "removable")) ?? false
            };

            // Get size information
            var sectors = ReadSysfsUlong(Path.Combine(SysBlockPath, deviceName, "size"));
            var logicalBlockSize = ReadSysfsUint(Path.Combine(SysBlockPath, deviceName, "queue", "logical_block_size")) ?? 512;
            var physicalBlockSize = ReadSysfsUint(Path.Combine(SysBlockPath, deviceName, "queue", "physical_block_size")) ?? 512;

            info.TotalSectors = sectors ?? 0;
            info.LogicalBlockSize = logicalBlockSize;
            info.PhysicalBlockSize = physicalBlockSize;
            info.Size = (sectors ?? 0) * logicalBlockSize;

            // Get device-specific information
            if (major == MajorNvme)
            {
                GetNvmeInfo(deviceName, info);
                info.SmartType = SmartType.Nvme;
                info.Smart = new SmartNvme(devicePath);
                info.Smart.Update();
            }
            else if (major == MajorMmc)
            {
                GetMmcInfo(deviceName, info);
                info.SmartType = SmartType.Unsupported;
                info.Smart = SmartUnsupported.Default;
            }
            else if (major == MajorScsi || major == MajorIde1 || major == MajorIde2)
            {
                GetScsiInfo(deviceName, info);
                var smart = new SmartGeneric(devicePath);
                if (smart.Update())
                {
                    info.SmartType = SmartType.Generic;
                    info.Smart = smart;
                }
                else
                {
                    smart.Dispose();
                    info.SmartType = SmartType.Unsupported;
                    info.Smart = SmartUnsupported.Default;
                }
            }
            else
            {
                GetScsiInfo(deviceName, info);
                info.SmartType = SmartType.Unsupported;
                info.Smart = SmartUnsupported.Default;
            }

            list.Add(info);
        }

        list.Sort((x, y) => (int)x.Index - (int)y.Index);

        return list;
    }

    private static bool IsPhysicalDisk(string deviceName)
    {
        // NVMe: nvme0n1 is disk, nvme0n1p1 is partition
        if (deviceName.StartsWith("nvme", StringComparison.Ordinal))
        {
            return !Regex.IsMatch(deviceName, @"nvme\d+n\d+p\d+");
        }

        // SATA/SCSI: sda is disk, sda1 is partition
        if (deviceName.StartsWith("sd", StringComparison.Ordinal) ||
            deviceName.StartsWith("hd", StringComparison.Ordinal))
        {
            return !Regex.IsMatch(deviceName, @"^[sh]d[a-z]+\d+$");
        }

        // VirtIO: vda is disk, vda1 is partition
        if (deviceName.StartsWith("vd", StringComparison.Ordinal))
        {
            return !Regex.IsMatch(deviceName, @"^vd[a-z]+\d+$");
        }

        // MMC: mmcblk0 is disk, mmcblk0p1 is partition
        if (deviceName.StartsWith("mmcblk", StringComparison.Ordinal))
        {
            return !Regex.IsMatch(deviceName, @"^mmcblk\d+p\d+$");
        }

        return false;
    }

    private static (int Major, int Minor) GetDeviceNumbers(string deviceName)
    {
        var devPath = Path.Combine(SysBlockPath, deviceName, "dev");
        var devStr = ReadSysfsString(devPath);

        if (devStr is null)
        {
            return (-1, -1);
        }

        var parts = devStr.Split(':');
        if (parts.Length != 2)
        {
            return (-1, -1);
        }

        var major = int.TryParse(parts[0], out var maj) ? maj : -1;
        var minor = int.TryParse(parts[1], out var min) ? min : -1;

        return (major, minor);
    }

    private static BusType GetBusTypeFromMajor(int major)
    {
        return major switch
        {
            MajorNvme => BusType.Nvme,
            MajorScsi => BusType.Sata,
            MajorIde1 or MajorIde2 => BusType.Ide,
            MajorMmc => BusType.Mmc,
            MajorVirtIO => BusType.VirtIO,
            _ => BusType.Unknown
        };
    }

    private static void GetNvmeInfo(string deviceName, DiskInfoGeneric info)
    {
        var devicePath = Path.Combine(SysBlockPath, deviceName, "device");

        info.Model = ReadSysfsString(Path.Combine(devicePath, "model")) ?? string.Empty;
        info.SerialNumber = ReadSysfsString(Path.Combine(devicePath, "serial")) ?? string.Empty;
        info.FirmwareRevision = ReadSysfsString(Path.Combine(devicePath, "firmware_rev")) ?? string.Empty;
    }

    private static void GetScsiInfo(string deviceName, DiskInfoGeneric info)
    {
        var devicePath = Path.Combine(SysBlockPath, deviceName, "device");

        var vendor = ReadSysfsString(Path.Combine(devicePath, "vendor"));
        var model = ReadSysfsString(Path.Combine(devicePath, "model"));

        if (!string.IsNullOrEmpty(vendor) && !string.IsNullOrEmpty(model))
        {
            info.Model = $"{vendor} {model}";
        }
        else if (!string.IsNullOrEmpty(model))
        {
            info.Model = model;
        }
        else if (!string.IsNullOrEmpty(vendor))
        {
            info.Model = vendor;
        }
        else
        {
            info.Model = string.Empty;
        }

        info.FirmwareRevision = ReadSysfsString(Path.Combine(devicePath, "rev")) ?? string.Empty;
        info.SerialNumber = string.Empty; // SCSI serial usually not available in sysfs
    }

    private static void GetMmcInfo(string deviceName, DiskInfoGeneric info)
    {
        var devicePath = Path.Combine(SysBlockPath, deviceName, "device");

        var name = ReadSysfsString(Path.Combine(devicePath, "name"));
        var type = ReadSysfsString(Path.Combine(devicePath, "type"));

        if (!string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(name))
        {
            info.Model = $"{type} {name}";
        }
        else if (!string.IsNullOrEmpty(name))
        {
            info.Model = name;
        }
        else
        {
            info.Model = string.Empty;
        }

        info.SerialNumber = ReadSysfsString(Path.Combine(devicePath, "cid")) ?? string.Empty;
        info.FirmwareRevision = ReadSysfsString(Path.Combine(devicePath, "fwrev")) ?? string.Empty;
    }

    private static string? ReadSysfsString(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var content = File.ReadAllText(path).Trim();
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
        catch
        {
            return null;
        }
    }

    private static ulong? ReadSysfsUlong(string path)
    {
        var str = ReadSysfsString(path);
        return str is not null && ulong.TryParse(str, out var value) ? value : null;
    }

    private static uint? ReadSysfsUint(string path)
    {
        var str = ReadSysfsString(path);
        return str is not null && uint.TryParse(str, out var value) ? value : null;
    }

    private static bool? ReadSysfsBool(string path)
    {
        var str = ReadSysfsString(path);
        return str switch
        {
            "1" => true,
            "0" => false,
            _ => null
        };
    }
}
