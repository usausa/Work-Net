namespace HardwareInfo.Disk;

using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Microsoft.Win32.SafeHandles;

using static HardwareInfo.Disk.NativeMethods;

[SupportedOSPlatform("windows")]
public static class DiskInfo
{
    public static IReadOnlyList<IDiskInfo> GetInformation()
    {
        var list = new List<IDiskInfo>();

        using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
        foreach (var disk in searcher.Get())
        {
            var info = new DiskInfoGeneric
            {
                Index = ConvertToUInt32(disk.Properties["Index"].Value),
                DeviceId = disk.Properties["DeviceID"].Value as string ?? string.Empty,
                PnpDeviceId = disk.Properties["PNPDeviceID"].Value as string ?? string.Empty,
                Status = disk.Properties["Status"].Value as string ?? string.Empty,
                Model = disk.Properties["Model"].Value as string ?? string.Empty,
                SerialNumber = (disk.Properties["SerialNumber"].Value as string)?.Trim() ?? string.Empty,
                FirmwareRevision = (disk.Properties["FirmwareRevision"].Value as string)?.Trim() ?? string.Empty,
                Size = ConvertToUInt64(disk.Properties["Size"].Value),
                BytesPerSector = ConvertToUInt32(disk.Properties["BytesPerSector"].Value),
                SectorsPerTrack = ConvertToUInt32(disk.Properties["SectorsPerTrack"].Value),
                TracksPerCylinder = ConvertToUInt32(disk.Properties["TracksPerCylinder"].Value),
                TotalHeads = ConvertToUInt32(disk.Properties["TotalHeads"].Value),
                TotalCylinders = ConvertToUInt64(disk.Properties["TotalCylinders"].Value),
                TotalTracks = ConvertToUInt64(disk.Properties["TotalTracks"].Value),
                TotalSectors = ConvertToUInt64(disk.Properties["TotalSectors"].Value),
                Partitions = ConvertToUInt32(disk.Properties["Partitions"].Value)
            };
            list.Add(info);

            // Get descriptor
            var descriptor = GetStorageDescriptor(info.DeviceId);
            if (descriptor is null)
            {
                info.SmartType = SmartType.Unsupported;
                info.Smart = SmartUnsupported.Default;
                continue;
            }

            info.BusType = (BusType)descriptor.BusType;
            info.Removable = descriptor.Removable;
            info.PhysicalBlockSize = descriptor.PhysicalBlockSize;

            // Virtual
            if (IsVirtualDisk(info.Model))
            {
                info.SmartType = SmartType.Unsupported;
                info.Smart = SmartUnsupported.Default;
                continue;
            }

            // NVMe
            if (descriptor.BusType is STORAGE_BUS_TYPE.BusTypeNvme)
            {
                info.SmartType = SmartType.Nvme;
                info.Smart = new SmartNvme(OpenDevice(info.DeviceId));
                info.Smart.Update();
                continue;
            }

            // ATA
            if (descriptor.BusType is STORAGE_BUS_TYPE.BusTypeAta or STORAGE_BUS_TYPE.BusTypeSata)
            {
                info.SmartType = SmartType.Generic;
                info.Smart = new SmartGeneric(OpenDevice(info.DeviceId), (byte)info.Index);
                info.Smart.Update();
                continue;
            }

            // USB
            if (descriptor.BusType is STORAGE_BUS_TYPE.BusTypeUsb)
            {
                var smart = new SmartUsb(OpenDevice(info.DeviceId));
                if (smart.Update())
                {
                    info.SmartType = SmartType.Generic;
                    info.Smart = smart;
                    continue;
                }

                smart.Dispose();
            }

            info.SmartType = SmartType.Unsupported;
            info.Smart = SmartUnsupported.Default;
        }

        list.Sort(IndexComparison);

        return list;
    }

    private static int IndexComparison(IDiskInfo x, IDiskInfo y) => (int)x.Index - (int)y.Index;

    private static bool IsVirtualDisk(string model)
    {
        return !string.IsNullOrEmpty(model) && model.StartsWith("Virtual HD", StringComparison.OrdinalIgnoreCase);
    }

    private static uint ConvertToUInt32(object? value)
    {
        if (value is null)
        {
            return 0;
        }

        try
        {
            return Convert.ToUInt32(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return 0;
        }
    }

    private static ulong ConvertToUInt64(object? value)
    {
        if (value is null)
        {
            return 0;
        }

        try
        {
            return Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return 0;
        }
    }

    //------------------------------------------------------------------------
    // Helper
    //------------------------------------------------------------------------

    private static SafeFileHandle OpenDevice(string devicePath) =>
        CreateFile(devicePath, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);

    private sealed record StorageDescriptor(STORAGE_BUS_TYPE BusType, bool Removable, uint PhysicalBlockSize);

    private static unsafe StorageDescriptor? GetStorageDescriptor(string devicePath)
    {
        using var handle = OpenDevice(devicePath);
        if (handle.IsInvalid)
        {
            return null;
        }

        var query = new STORAGE_PROPERTY_QUERY
        {
            PropertyId = STORAGE_PROPERTY_ID.StorageDeviceProperty,
            QueryType = STORAGE_QUERY_TYPE.PropertyStandardQuery
        };
        var header = default(STORAGE_DEVICE_DESCRIPTOR_HEADER);
        if (!DeviceIoControl(handle, IOCTL_STORAGE_QUERY_PROPERTY, ref query, Marshal.SizeOf(query), ref header, Marshal.SizeOf<STORAGE_DEVICE_DESCRIPTOR_HEADER>(), out _, IntPtr.Zero))
        {
            return null;
        }

        var ptr = Marshal.AllocHGlobal((int)header.Size);
        try
        {
            if (!DeviceIoControl(handle, IOCTL_STORAGE_QUERY_PROPERTY, ref query, Marshal.SizeOf(query), ptr, header.Size, out _, IntPtr.Zero))
            {
                return null;
            }

            var descriptor = (STORAGE_DEVICE_DESCRIPTOR*)ptr;
            var busType = descriptor->BusType;
            var removable = descriptor->RemovableMedia;

            // Get physical block size from access alignment descriptor
            var physicalBlockSize = GetPhysicalBlockSize(handle);

            return new StorageDescriptor(busType, removable, physicalBlockSize);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    private static uint GetPhysicalBlockSize(SafeFileHandle handle)
    {
        var query = new STORAGE_PROPERTY_QUERY
        {
            PropertyId = STORAGE_PROPERTY_ID.StorageAccessAlignmentProperty,
            QueryType = STORAGE_QUERY_TYPE.PropertyStandardQuery
        };

        var alignment = default(STORAGE_ACCESS_ALIGNMENT_DESCRIPTOR);
        if (DeviceIoControl(handle, IOCTL_STORAGE_QUERY_PROPERTY, ref query, Marshal.SizeOf(query), ref alignment, Marshal.SizeOf<STORAGE_ACCESS_ALIGNMENT_DESCRIPTOR>(), out _, IntPtr.Zero))
        {
            return alignment.BytesPerPhysicalSector;
        }

        return 0;
    }
}
