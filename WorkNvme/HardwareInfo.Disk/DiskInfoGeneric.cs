namespace HardwareInfo.Disk;

internal sealed class DiskInfoGeneric : IDiskInfo
{
    public uint Index { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    public string PnpDeviceId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string SerialNumber { get; set; } = string.Empty;

    public string FirmwareRevision { get; set; } = string.Empty;

    public ulong Size { get; set; }

    public uint BytesPerSector { get; set; }

    public uint PhysicalBlockSize { get; set; }

    public uint SectorsPerTrack { get; set; }

    public uint TracksPerCylinder { get; set; }

    public uint TotalHeads { get; set; }

    public ulong TotalCylinders { get; set; }

    public ulong TotalTracks { get; set; }

    public ulong TotalSectors { get; set; }

    public uint Partitions { get; set; }

    public bool Removable { get; set; }

    public BusType BusType { get; set; }

    public SmartType SmartType { get; set; }

    public ISmart Smart { get; set; } = default!;

    public void Dispose()
    {
        (Smart as IDisposable)?.Dispose();
    }
}
