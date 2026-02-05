namespace LinuxDotNet.Disk;

internal sealed class DiskInfoGeneric : IDiskInfo
{
    public uint Index { get; set; }

    public string DeviceName { get; set; } = default!;

    public string Model { get; set; } = default!;

    public string SerialNumber { get; set; } = default!;

    public string FirmwareRevision { get; set; } = default!;

    public ulong Size { get; set; }

    public uint LogicalBlockSize { get; set; }

    public uint PhysicalBlockSize { get; set; }

    public ulong TotalSectors { get; set; }

    public bool Removable { get; set; }

    public BusType BusType { get; set; }

    public SmartType SmartType { get; set; }

    public ISmart Smart { get; set; } = default!;

    public void Dispose()
    {
        (Smart as IDisposable)?.Dispose();
    }
}
