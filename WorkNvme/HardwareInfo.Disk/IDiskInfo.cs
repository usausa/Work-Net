namespace HardwareInfo.Disk;

public interface IDiskInfo : IDisposable
{
    public uint Index { get; }

    public string DeviceId { get; }

    public string PnpDeviceId { get; }

    public string Status { get; }

    public string Model { get; }

    public string SerialNumber { get; }

    public string FirmwareRevision { get; }

    public ulong Size { get; }

    public uint BytesPerSector { get; }

    /// <summary>
    /// Alias for BytesPerSector for cross-platform compatibility.
    /// </summary>
    public uint LogicalBlockSize => BytesPerSector;

    public uint PhysicalBlockSize { get; }

    public uint SectorsPerTrack { get; } // TotalSectors / TotalTracks

    public uint TracksPerCylinder { get; } // TotalTracks / TotalCylinders

    public uint TotalHeads { get; }

    public ulong TotalCylinders { get; }

    public ulong TotalTracks { get; }

    public ulong TotalSectors { get; }

    public uint Partitions { get; }

    public bool Removable { get; }

    public BusType BusType { get; }

    public SmartType SmartType { get; }

    public ISmart Smart { get; }
}
