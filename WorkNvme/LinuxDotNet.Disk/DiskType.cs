namespace LinuxDotNet.Disk;

/// <summary>
/// Disk type identified by Linux block device major number.
/// These major numbers are specific to block devices (storage) in /sys/block.
/// Note: This differs from Windows BusType which identifies the bus connection type.
/// </summary>
public enum DiskType
{
    Unknown = 0,

    /// <summary>Major 8: SCSI subsystem (includes SATA, SAS, USB storage via sd* driver)</summary>
    Scsi,

    /// <summary>Major 3, 22: Legacy IDE block devices</summary>
    Ide,

    /// <summary>Major 259: NVMe block devices</summary>
    Nvme,

    /// <summary>Major 179: MMC/SD card block devices</summary>
    Mmc,

    /// <summary>Major 252: VirtIO block devices (virtual machines)</summary>
    VirtIO
}
