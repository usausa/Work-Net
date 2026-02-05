namespace LinuxDotNet.Disk;

using System.Runtime.Versioning;
using System.Text.RegularExpressions;

[SupportedOSPlatform("linux")]
public static class DiskInfoExtensions
{
    private const string SysBlockPath = "/sys/block";
    private const string ProcMountsPath = "/proc/mounts";

    public static IEnumerable<PartitionInfo> GetPartitions(this IDiskInfo disk)
    {
        var deviceName = Path.GetFileName(disk.DeviceName);
        if (string.IsNullOrEmpty(deviceName))
        {
            yield break;
        }

        var blockPath = Path.Combine(SysBlockPath, deviceName);
        if (!Directory.Exists(blockPath))
        {
            yield break;
        }

        var mountPoints = GetMountPoints();
        var partitionPattern = GetPartitionPattern(deviceName);

        var directories = Directory.GetDirectories(blockPath)
            .Select(Path.GetFileName)
            .Where(name => name is not null && Regex.IsMatch(name, partitionPattern))
            .OrderBy(name => name)
            .Cast<string>();

        uint index = 0;
        foreach (var partName in directories)
        {
            var partPath = Path.Combine(blockPath, partName);
            var sizePath = Path.Combine(partPath, "size");

            var sectors = ReadSysfsUlong(sizePath) ?? 0;
            var partDevicePath = $"/dev/{partName}";

            mountPoints.TryGetValue(partDevicePath, out var mountInfo);

            yield return new PartitionInfo
            {
                Index = index++,
                DeviceName = partDevicePath,
                Name = partName,
                Size = sectors * disk.LogicalBlockSize,
                MountPoint = mountInfo?.MountPoint,
                FileSystem = mountInfo?.FileSystem
            };
        }
    }

    private static string GetPartitionPattern(string deviceName)
    {
        // NVMe: nvme0n1 -> nvme0n1p\d+
        if (deviceName.StartsWith("nvme", StringComparison.Ordinal))
        {
            return $"^{Regex.Escape(deviceName)}p\\d+$";
        }

        // SATA/SCSI/VirtIO: sda -> sda\d+, vda -> vda\d+
        return $"^{Regex.Escape(deviceName)}\\d+$";
    }

    private static Dictionary<string, (string MountPoint, string FileSystem)?> GetMountPoints()
    {
        var result = new Dictionary<string, (string MountPoint, string FileSystem)?>(StringComparer.Ordinal);

        if (!File.Exists(ProcMountsPath))
        {
            return result;
        }

        try
        {
            var lines = File.ReadAllLines(ProcMountsPath);
            foreach (var line in lines)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    var device = parts[0];
                    var mountPoint = parts[1];
                    var fileSystem = parts[2];

                    if (device.StartsWith("/dev/", StringComparison.Ordinal))
                    {
                        result[device] = (mountPoint, fileSystem);
                    }
                }
            }
        }
        catch
        {
            // Ignore errors reading mount points
        }

        return result;
    }

    private static ulong? ReadSysfsUlong(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var content = File.ReadAllText(path).Trim();
            return ulong.TryParse(content, out var value) ? value : null;
        }
        catch
        {
            return null;
        }
    }
}
