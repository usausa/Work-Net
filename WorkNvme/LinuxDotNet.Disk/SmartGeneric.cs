namespace LinuxDotNet.Disk;

using System.Runtime.InteropServices;

using static LinuxDotNet.Disk.NativeMethods;

internal sealed class SmartGeneric : ISmartGeneric, IDisposable
{
    private const int SmartDataSize = 512;
    private const int MaxAttributes = 30;
    private const int TableOffset = 2;
    private const int EntrySize = 12;

    private readonly string devicePath;
    private byte[]? smartData;

    public bool LastUpdate { get; private set; }

    public SmartGeneric(string devicePath)
    {
        this.devicePath = devicePath;
        smartData = new byte[SmartDataSize];
    }

    public void Dispose()
    {
        smartData = null;
    }

    public unsafe bool Update()
    {
        if (smartData is null)
        {
            LastUpdate = false;
            return false;
        }

        Array.Clear(smartData, 0, smartData.Length);

        var fd = open(devicePath, O_RDONLY | O_NONBLOCK);
        if (fd < 0)
        {
            LastUpdate = false;
            return false;
        }

        try
        {
            fixed (byte* dataPtr = smartData)
            {
                // Try PT12 first
                if (TrySmartReadPt12(fd, dataPtr))
                {
                    LastUpdate = true;
                    return true;
                }

                // Try PT16 as fallback
                Array.Clear(smartData, 0, smartData.Length);
                if (TrySmartReadPt16(fd, dataPtr))
                {
                    LastUpdate = true;
                    return true;
                }
            }
        }
        finally
        {
            _ = close(fd);
        }

        LastUpdate = false;
        return false;
    }

    private static unsafe bool TrySmartReadPt12(int fd, byte* data)
    {
        var cdb = stackalloc byte[12];
        cdb[0] = 0xA1;      // ATA PASS-THROUGH(12)
        cdb[1] = 4 << 1;    // protocol = 4 (PIO Data-In)
        cdb[2] = 0x0E;      // off_line=0, ck_cond=0, t_dir=1, byte_block=1, t_length=10
        cdb[3] = 0xD0;      // features (SMART_READ_DATA)
        cdb[4] = 0x01;      // sector_count
        cdb[5] = 0x00;      // lba_low
        cdb[6] = 0x4F;      // lba_mid (SMART signature)
        cdb[7] = 0xC2;      // lba_high (SMART signature)
        cdb[8] = 0x00;      // device
        cdb[9] = 0xB0;      // command (SMART)
        cdb[10] = 0x00;
        cdb[11] = 0x00;

        var sense = stackalloc byte[64];
        return DoSgIo(fd, cdb, 12, data, SmartDataSize, sense, 64);
    }

    private static unsafe bool TrySmartReadPt16(int fd, byte* data)
    {
        var cdb = stackalloc byte[16];
        cdb[0] = 0x85;      // ATA PASS-THROUGH(16)
        cdb[1] = 4 << 1;    // protocol = 4 (PIO Data-In)
        cdb[2] = 0x0E;      // off_line=0, ck_cond=0, t_dir=1, byte_block=1, t_length=10
        cdb[3] = 0x00;
        cdb[4] = 0xD0;      // features (SMART_READ_DATA)
        cdb[5] = 0x00;
        cdb[6] = 0x01;      // sector_count
        cdb[7] = 0x00;
        cdb[8] = 0x00;      // lba_low
        cdb[9] = 0x00;
        cdb[10] = 0x4F;     // lba_mid (SMART signature)
        cdb[11] = 0x00;
        cdb[12] = 0xC2;     // lba_high (SMART signature)
        cdb[13] = 0x00;     // device
        cdb[14] = 0xB0;     // command (SMART)
        cdb[15] = 0x00;

        var sense = stackalloc byte[64];
        return DoSgIo(fd, cdb, 16, data, SmartDataSize, sense, 64);
    }

    private static unsafe bool DoSgIo(int fd, byte* cdb, int cdbLen, byte* data, int dataLen, byte* sense, int senseLen)
    {
        var io = new sg_io_hdr_t
        {
            interface_id = 'S',
            cmdp = cdb,
            cmd_len = (byte)cdbLen,
            dxferp = data,
            dxfer_len = (uint)dataLen,
            dxfer_direction = SG_DXFER_FROM_DEV,
            sbp = sense,
            mx_sb_len = (byte)senseLen,
            timeout = 10000
        };

        if (ioctl(fd, SG_IO, &io) < 0)
        {
            return false;
        }

        var ok = ((io.info & SG_INFO_OK_MASK) == SG_INFO_OK) &&
                 (io.status == 0) &&
                 (io.host_status == 0) &&
                 (io.driver_status == 0);

        return ok;
    }

    public IReadOnlyList<SmartId> GetSupportedIds()
    {
        var list = new List<SmartId>();

        if (smartData is null)
        {
            return list;
        }

        for (var i = 0; i < MaxAttributes; i++)
        {
            var offset = TableOffset + (i * EntrySize);
            var id = smartData[offset];
            if (id != 0 && id != 0xff)
            {
                list.Add((SmartId)id);
            }
        }

        return list;
    }

    public SmartAttribute? GetAttribute(SmartId id)
    {
        if (smartData is null)
        {
            return null;
        }

        var target = (byte)id;
        for (var i = 0; i < MaxAttributes; i++)
        {
            var offset = TableOffset + (i * EntrySize);
            if (smartData[offset] == target)
            {
                var rawOffset = offset + 5;
                return new SmartAttribute
                {
                    Id = smartData[offset],
                    Flags = (short)(smartData[offset + 1] | (smartData[offset + 2] << 8)),
                    CurrentValue = smartData[offset + 3],
                    WorstValue = smartData[offset + 4],
                    RawValue = Raw48ToU64(smartData, rawOffset)
                };
            }
        }

        return null;
    }

    private static ulong Raw48ToU64(byte[] data, int offset)
    {
        ulong v = 0;
        for (var i = 5; i >= 0; i--)
        {
            v = (v << 8) | data[offset + i];
        }
        return v;
    }
}
