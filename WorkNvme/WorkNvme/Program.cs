using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace SmartReader
{
    public static class NativeMethods
    {
        public const int O_RDONLY = 0x0000;
        public const int O_NONBLOCK = 0x0800;
        public const int NVME_MAJOR = 259;
        public const int SG_DXFER_FROM_DEV = -3;
        public const uint SG_INFO_OK_MASK = 0x1;
        public const uint SG_INFO_OK = 0x0;

        [DllImport("libc", SetLastError = true)]
        public static extern int open([MarshalAs(UnmanagedType.LPUTF8Str)] string pathname, int flags);

        [DllImport("libc", SetLastError = true)]
        public static extern int close(int fd);

        [DllImport("libc", SetLastError = true)]
        public static extern int stat([MarshalAs(UnmanagedType.LPUTF8Str)] string pathname, out stat_t statbuf);

        [DllImport("libc", SetLastError = true)]
        public static extern int ioctl(int fd, ulong request, ref sg_io_hdr_t data);

        [DllImport("libc", SetLastError = true)]
        public static extern int ioctl(int fd, ulong request, ref nvme_admin_cmd data);

        public const ulong SG_IO = 0x2285;
        public const ulong NVME_IOCTL_ADMIN_CMD = 0xC0484E41;

        [StructLayout(LayoutKind.Sequential)]
        public struct stat_t
        {
            public ulong st_dev;
            public ulong st_ino;
            public ulong st_nlink;
            public uint st_mode;
            public uint st_uid;
            public uint st_gid;
            public int __pad0;
            public ulong st_rdev;
            public long st_size;
            public long st_blksize;
            public long st_blocks;
            public long st_atime;
            public long st_atime_nsec;
            public long st_mtime;
            public long st_mtime_nsec;
            public long st_ctime;
            public long st_ctime_nsec;
            public long __unused0;
            public long __unused1;
            public long __unused2;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct sg_io_hdr_t
        {
            public int interface_id;
            public int dxfer_direction;
            public byte cmd_len;
            public byte mx_sb_len;
            public ushort iovec_count;
            public uint dxfer_len;
            public void* dxferp;
            public byte* cmdp;
            public byte* sbp;
            public uint timeout;
            public uint flags;
            public int pack_id;
            public void* usr_ptr;
            public byte status;
            public byte masked_status;
            public byte msg_status;
            public byte sb_len_wr;
            public ushort host_status;
            public ushort driver_status;
            public int resid;
            public uint duration;
            public uint info;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct nvme_admin_cmd
        {
            public byte opcode;
            public byte flags;
            public ushort rsvd1;
            public uint nsid;
            public uint cdw2;
            public uint cdw3;
            public ulong metadata;
            public ulong addr;
            public uint metadata_len;
            public uint data_len;
            public uint cdw10;
            public uint cdw11;
            public uint cdw12;
            public uint cdw13;
            public uint cdw14;
            public uint cdw15;
            public uint timeout_ms;
            public uint result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct nvme_smart_log
        {
            public byte critical_warning;
            public fixed byte temperature[2];
            public byte avail_spare;
            public byte spare_thresh;
            public byte percent_used;
            public fixed byte rsvd6[26];
            public fixed byte data_units_read[16];
            public fixed byte data_units_written[16];
            public fixed byte host_reads[16];
            public fixed byte host_writes[16];
            public fixed byte ctrl_busy_time[16];
            public fixed byte power_cycles[16];
            public fixed byte power_on_hours[16];
            public fixed byte unsafe_shutdowns[16];
            public fixed byte media_errors[16];
            public fixed byte num_err_log_entries[16];
            public uint warning_temp_time;
            public uint critical_comp_time;
            public fixed ushort temp_sensor[8];
            public uint thm_temp1_trans_count;
            public uint thm_temp2_trans_count;
            public uint thm_temp1_total_time;
            public uint thm_temp2_total_time;
            public fixed byte rsvd232[280];
        }

        public static uint major(ulong dev)
        {
            return (uint)((dev >> 8) & 0xfff);
        }

        public static bool S_ISBLK(uint mode)
        {
            return (mode & 0xF000) == 0x6000;
        }
    }

    // ========== データモデル ==========

    public enum DeviceType
    {
        Unknown,
        SATA,
        NVMe
    }

    public class SataSmartAttribute
    {
        public byte Id { get; set; }
        public ushort Flags { get; set; }
        public byte Current { get; set; }
        public byte Worst { get; set; }
        public byte[] RawBytes { get; set; } = default!;
        public ulong RawValue { get; set; }
    }

    public class SataSmartData
    {
        public DeviceType DeviceType => DeviceType.SATA;
        public List<SataSmartAttribute> Attributes { get; set; } = new List<SataSmartAttribute>();
    }

    public class NvmeSmartData
    {
        public DeviceType DeviceType => DeviceType.NVMe;
        public byte CriticalWarning { get; set; }
        public int? TemperatureCelsius { get; set; }
        public byte AvailableSparePercent { get; set; }
        public byte SpareThresholdPercent { get; set; }
        public byte PercentageUsed { get; set; }
        public ulong DataUnitsRead { get; set; }
        public double DataUnitsReadGB { get; set; }
        public ulong DataUnitsWritten { get; set; }
        public double DataUnitsWrittenGB { get; set; }
        public ulong PowerCycles { get; set; }
        public ulong PowerOnHours { get; set; }
        public ulong UnsafeShutdowns { get; set; }
        public ulong MediaErrors { get; set; }
    }

    public class SmartReadException : Exception
    {
        public SmartReadException(string message) : base(message) { }
        public SmartReadException(string message, Exception inner) : base(message, inner) { }
    }

    // ========== ライブラリ本体 ==========

    public unsafe class SmartLibrary
    {
        // ========== 共通ユーティリティ ==========

        private static DeviceType DetectDeviceType(string path)
        {
            if (NativeMethods.stat(path, out var st) == 0 && NativeMethods.S_ISBLK(st.st_mode))
            {
                if (NativeMethods.major(st.st_rdev) == NativeMethods.NVME_MAJOR)
                {
                    return DeviceType.NVMe;
                }
                return DeviceType.SATA;
            }
            return DeviceType.Unknown;
        }

        private static ushort Le16(byte* p)
        {
            return (ushort)(p[0] | (p[1] << 8));
        }

        private static ulong Raw48ToU64(byte* raw)
        {
            ulong v = 0;
            for (int i = 5; i >= 0; --i)
                v = (v << 8) | raw[i];
            return v;
        }

        // ========== SATA/ATA 用 ==========

        private static int DoSgIo(int fd, byte* cdb, int cdbLen,
                                   void* dxferp, uint dxferLen, int dxferDir,
                                   byte* sense, uint senseLen,
                                   int timeoutMs, out string errorDetail)
        {
            errorDetail = null!;

            var io = new NativeMethods.sg_io_hdr_t
            {
                interface_id = 'S',
                cmdp = cdb,
                cmd_len = (byte)cdbLen,
                dxferp = dxferp,
                dxfer_len = dxferLen,
                dxfer_direction = dxferDir,
                sbp = sense,
                mx_sb_len = (byte)senseLen,
                timeout = (uint)timeoutMs
            };

            if (NativeMethods.ioctl(fd, NativeMethods.SG_IO, ref io) < 0)
            {
                int e = Marshal.GetLastWin32Error();
                errorDetail = $"SG_IO ioctl failed with errno {e}";
                return -e;
            }

            bool ok = ((io.info & NativeMethods.SG_INFO_OK_MASK) == NativeMethods.SG_INFO_OK) &&
                     (io.status == 0) &&
                     (io.host_status == 0) &&
                     (io.driver_status == 0);

            if (!ok)
            {
                errorDetail = $"SG_IO failed: status=0x{io.status:x} host=0x{io.host_status:x} driver=0x{io.driver_status:x}";
                return -5; // -EIO
            }

            return 0;
        }

        private static int SmartReadDataPt12(int fd, byte* data512, out string errorDetail)
        {
            byte* cdb = stackalloc byte[12];
            for (int i = 0; i < 12; i++) cdb[i] = 0;

            cdb[0] = 0xA1;
            cdb[1] = (4 << 1);
            cdb[2] = 0x0E;
            cdb[3] = 0xD0;
            cdb[4] = 0x01;
            cdb[5] = 0x00;
            cdb[6] = 0x4F;
            cdb[7] = 0xC2;
            cdb[8] = 0x00;
            cdb[9] = 0xB0;

            byte* sense = stackalloc byte[64];
            for (int i = 0; i < 64; i++) sense[i] = 0;

            return DoSgIo(fd, cdb, 12, data512, 512, NativeMethods.SG_DXFER_FROM_DEV,
                         sense, 64, 10000, out errorDetail);
        }

        private static int SmartReadDataPt16(int fd, byte* data512, out string errorDetail)
        {
            byte* cdb = stackalloc byte[16];
            for (int i = 0; i < 16; i++) cdb[i] = 0;

            cdb[0] = 0x85;
            cdb[1] = (4 << 1);
            cdb[2] = 0x0E;
            cdb[4] = 0xD0;
            cdb[6] = 0x01;
            cdb[8] = 0x00;
            cdb[10] = 0x4F;
            cdb[12] = 0xC2;
            cdb[14] = 0xB0;

            byte* sense = stackalloc byte[64];
            for (int i = 0; i < 64; i++) sense[i] = 0;

            return DoSgIo(fd, cdb, 16, data512, 512, NativeMethods.SG_DXFER_FROM_DEV,
                         sense, 64, 10000, out errorDetail);
        }

        private static SataSmartData ParseSataSmart(byte* smart)
        {
            var result = new SataSmartData();

            const int tableOff = 2;
            const int entrySz = 12;
            const int n = 30;

            for (int i = 0; i < n; i++)
            {
                byte* e = smart + tableOff + i * entrySz;
                byte id = e[0];
                if (id == 0x00 || id == 0xff) continue;

                ushort flags = Le16(e + 1);
                byte cur = e[3];
                byte wor = e[4];
                byte* raw = e + 5;

                var attr = new SataSmartAttribute
                {
                    Id = id,
                    Flags = flags,
                    Current = cur,
                    Worst = wor,
                    RawBytes = new byte[6],
                    RawValue = Raw48ToU64(raw)
                };

                for (int j = 0; j < 6; j++)
                {
                    attr.RawBytes[j] = raw[j];
                }

                result.Attributes.Add(attr);
            }

            return result;
        }

        private static SataSmartData GetSataSmart(int fd)
        {
            byte* data512 = stackalloc byte[512];

            int rc = SmartReadDataPt12(fd, data512, out string error12);
            if (rc == 0)
            {
                return ParseSataSmart(data512);
            }

            rc = SmartReadDataPt16(fd, data512, out string error16);
            if (rc != 0)
            {
                throw new SmartReadException($"Failed to read SATA SMART data. PT12: {error12}, PT16: {error16}");
            }

            return ParseSataSmart(data512);
        }

        // ========== NVMe 用 ==========

        private static ulong Le128ToU64(byte* p)
        {
            ulong v = 0;
            for (int i = 7; i >= 0; i--)
            {
                v = (v << 8) | p[i];
            }
            return v;
        }

        private static NvmeSmartData GetNvmeSmart(int fd)
        {
            var smartLog = new NativeMethods.nvme_smart_log();
            var cmd = new NativeMethods.nvme_admin_cmd();

            cmd.opcode = 0x02;
            cmd.nsid = 0xFFFFFFFF;
            cmd.addr = (ulong)(&smartLog);
            cmd.data_len = (uint)sizeof(NativeMethods.nvme_smart_log);
            cmd.cdw10 = 0x02 | ((uint)(sizeof(NativeMethods.nvme_smart_log) / 4 - 1) << 16);

            if (NativeMethods.ioctl(fd, NativeMethods.NVME_IOCTL_ADMIN_CMD, ref cmd) < 0)
            {
                int e = Marshal.GetLastWin32Error();
                throw new SmartReadException($"NVME_IOCTL_ADMIN_CMD failed with errno {e}");
            }

            var result = new NvmeSmartData
            {
                CriticalWarning = smartLog.critical_warning,
                AvailableSparePercent = smartLog.avail_spare,
                SpareThresholdPercent = smartLog.spare_thresh,
                PercentageUsed = smartLog.percent_used,
                DataUnitsRead = Le128ToU64(smartLog.data_units_read),
                DataUnitsWritten = Le128ToU64(smartLog.data_units_written),
                PowerCycles = Le128ToU64(smartLog.power_cycles),
                PowerOnHours = Le128ToU64(smartLog.power_on_hours),
                UnsafeShutdowns = Le128ToU64(smartLog.unsafe_shutdowns),
                MediaErrors = Le128ToU64(smartLog.media_errors)
            };

            result.DataUnitsReadGB = result.DataUnitsRead * 0.0005;
            result.DataUnitsWrittenGB = result.DataUnitsWritten * 0.0005;

            ushort tempK = Le16(smartLog.temperature);
            if (tempK > 0)
            {
                result.TemperatureCelsius = tempK - 273;
            }

            return result;
        }

        // ========== 公開API ==========

        public static DeviceType GetDeviceType(string devicePath)
        {
            return DetectDeviceType(devicePath);
        }

        public static SataSmartData ReadSataSmart(string devicePath)
        {
            int fd = NativeMethods.open(devicePath, NativeMethods.O_RDONLY | NativeMethods.O_NONBLOCK);
            if (fd < 0)
            {
                throw new SmartReadException($"Failed to open device: {devicePath}");
            }

            try
            {
                return GetSataSmart(fd);
            }
            finally
            {
                NativeMethods.close(fd);
            }
        }

        public static NvmeSmartData ReadNvmeSmart(string devicePath)
        {
            int fd = NativeMethods.open(devicePath, NativeMethods.O_RDONLY);
            if (fd < 0)
            {
                throw new SmartReadException($"Failed to open device: {devicePath}");
            }

            try
            {
                return GetNvmeSmart(fd);
            }
            finally
            {
                NativeMethods.close(fd);
            }
        }

        public static object ReadSmart(string devicePath)
        {
            var deviceType = GetDeviceType(devicePath);

            switch (deviceType)
            {
                case DeviceType.NVMe:
                    return ReadNvmeSmart(devicePath);
                case DeviceType.SATA:
                    return ReadSataSmart(devicePath);
                default:
                    throw new SmartReadException($"Unknown or unsupported device type: {devicePath}");
            }
        }
    }

    // ========== サンプルプログラム ==========

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("usage: SmartReader /dev/sdX_or_/dev/nvmeXnY");
                Environment.Exit(2);
            }

            string path = args[0];

            try
            {
                var deviceType = SmartLibrary.GetDeviceType(path);
                Console.WriteLine($"Detected: {deviceType} device\n");

                var smartData = SmartLibrary.ReadSmart(path);

                if (smartData is NvmeSmartData nvme)
                {
                    PrintNvmeSmart(nvme);
                }
                else if (smartData is SataSmartData sata)
                {
                    PrintSataSmart(sata);
                }
            }
            catch (SmartReadException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static void PrintNvmeSmart(NvmeSmartData smart)
        {
            Console.WriteLine("=== NVMe SMART/Health Information ===\n");

            if (smart.TemperatureCelsius.HasValue)
            {
                Console.WriteLine($"Temperature:                {smart.TemperatureCelsius.Value}°C");
            }

            Console.WriteLine($"Available Spare:            {smart.AvailableSparePercent}%");
            Console.WriteLine($"Percentage Used:            {smart.PercentageUsed}%");
            Console.WriteLine($"Data Units Read:            {smart.DataUnitsRead} ({smart.DataUnitsReadGB:F2} GB)");
            Console.WriteLine($"Data Units Written:         {smart.DataUnitsWritten} ({smart.DataUnitsWrittenGB:F2} GB)");
            Console.WriteLine($"Power Cycles:               {smart.PowerCycles}");
            Console.WriteLine($"Power On Hours:             {smart.PowerOnHours}");
            Console.WriteLine($"Unsafe Shutdowns:           {smart.UnsafeShutdowns}");
            Console.WriteLine($"Media Errors:               {smart.MediaErrors}");
        }

        static void PrintSataSmart(SataSmartData smart)
        {
            Console.WriteLine("=== SATA SMART Attributes ===\n");
            Console.WriteLine("ID  FLAG   CUR WOR  RAW(HEX 6B)          RAW(U64)");
            Console.WriteLine("---------------------------------------------------------");

            foreach (var attr in smart.Attributes)
            {
                Console.WriteLine($"{attr.Id,3}  0x{attr.Flags:x4}  {attr.Current,3} {attr.Worst,3}  " +
                                $"{attr.RawBytes[0]:x2} {attr.RawBytes[1]:x2} {attr.RawBytes[2]:x2} " +
                                $"{attr.RawBytes[3]:x2} {attr.RawBytes[4]:x2} {attr.RawBytes[5]:x2}  " +
                                $"{attr.RawValue,10}");
            }
        }
    }
}