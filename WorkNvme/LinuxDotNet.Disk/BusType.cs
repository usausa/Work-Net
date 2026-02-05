namespace LinuxDotNet.Disk;

public enum BusType
{
    Unknown = 0,
    Scsi,
    Ata,
    Sata,
    Nvme,
    Usb,
    Mmc,
    VirtIO,
    Ide
}
