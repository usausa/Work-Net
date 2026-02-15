namespace MacDotNet.SystemInfo;

using System.Runtime.InteropServices;

// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
#pragma warning disable CA2101
#pragma warning disable CA5392
#pragma warning disable CS8981
internal static class NativeMethods
{
    //------------------------------------------------------------------------
    // Constants
    //------------------------------------------------------------------------

    // 成功コード (mach/kern_return.h)
    public const int KERN_SUCCESS = 0;

    // host_statistics64のflavor (mach/host_info.h)
    public const int HOST_VM_INFO64 = 4;

    // vm_statistics64のサイズ (natural_t単位)
    public const int HOST_VM_INFO64_COUNT = 40;

    // host_processor_info flavor (mach/processor_info.h)
    public const int PROCESSOR_CPU_LOAD_INFO = 2;

    // CPU state indices (mach/machine.h)
    public const int CPU_STATE_USER = 0;
    public const int CPU_STATE_SYSTEM = 1;
    public const int CPU_STATE_IDLE = 2;
    public const int CPU_STATE_NICE = 3;
    public const int CPU_STATE_MAX = 4;

    // CFStringEncoding (CFString.h)
    public const uint kCFStringEncodingUTF8 = 0x08000100;

    // CFNumberType (CFNumber.h)
    public const int kCFNumberSInt32Type = 3;
    public const int kCFNumberSInt64Type = 4;

    // IOPowerSources dictionary keys (IOPSKeys.h)
    public const string kIOPSNameKey = "Name";
    public const string kIOPSTypeKey = "Type";
    public const string kIOPSTransportTypeKey = "Transport Type";
    public const string kIOPSHardwareSerialNumberKey = "Hardware Serial Number";
    public const string kIOPSIsPresentKey = "Is Present";
    public const string kIOPSPowerSourceStateKey = "Power Source State";
    public const string kIOPSIsChargingKey = "Is Charging";
    public const string kIOPSIsChargedKey = "Is Charged";
    public const string kIOPSCurrentCapacityKey = "Current Capacity";
    public const string kIOPSMaxCapacityKey = "Max Capacity";
    public const string kIOPSTimeToEmptyKey = "Time to Empty";
    public const string kIOPSTimeToFullChargeKey = "Time to Full Charge";
    public const string kIOPSBatteryHealthKey = "BatteryHealth";
    public const string kIOPSBatteryHealthConditionKey = "BatteryHealthCondition";
    public const string kIOPSDesignCycleCountKey = "DesignCycleCount9C";
    public const string kIOPSACPowerValue = "AC Power";

    // getfsstat モード (sys/mount.h)
    public const int MNT_WAIT = 1;
    public const int MNT_NOWAIT = 2;

    // マウントフラグ (sys/mount.h)
    public const uint MNT_RDONLY = 0x00000001;
    public const uint MNT_LOCAL = 0x00001000;

    // アドレスファミリー定数 (sys/socket.h)
    public const byte AF_INET = 2;
    public const byte AF_LINK = 18;
    public const byte AF_INET6 = 30;

    // インターフェースフラグ定数 (net/if.h)
    public const uint IFF_UP = 0x1;
    public const uint IFF_BROADCAST = 0x2;
    public const uint IFF_LOOPBACK = 0x8;
    public const uint IFF_POINTOPOINT = 0x10;
    public const uint IFF_RUNNING = 0x40;
    public const uint IFF_MULTICAST = 0x8000;

    // インターフェース種別定数 (net/if_types.h)
    public const byte IFT_ETHER = 0x06;
    public const byte IFT_LOOP = 0x18;
    public const byte IFT_GIF = 0x37;
    public const byte IFT_STF = 0x39;
    public const byte IFT_IEEE80211 = 0x47;
    public const byte IFT_BRIDGE = 0xD1;
    public const byte IFT_CELLULAR = 0xFF;

    // inet_ntop用バッファサイズ定数 (netinet/in.h)
    public const uint INET_ADDRSTRLEN = 16;
    public const uint INET6_ADDRSTRLEN = 46;

    // proc_listpids type (sys/proc_info.h)
    public const uint PROC_ALL_PIDS = 1;

    // proc_pidinfo flavor (sys/proc_info.h)
    public const int PROC_PIDTBSDINFO = 3;
    public const int PROC_PIDTASKINFO = 4;

    // proc_pidpath buffer size (sys/proc_info.h)
    public const uint PROC_PIDPATHINFO_MAXSIZE = 4096;

    // SMC selector (IOConnectCallStructMethodのselector引数)
    public const uint KERNEL_INDEX_SMC = 2;

    // SMCコマンド
    public const byte SMC_CMD_READ_BYTES = 5;
    public const byte SMC_CMD_READ_KEYINFO = 9;
    public const byte SMC_CMD_READ_INDEX = 8;

    // SMCデータ型定数 (4文字をビッグエンディアンuint32にエンコード)
    public const uint DATA_TYPE_FLT = 0x666C7420;   // "flt "
    public const uint DATA_TYPE_SP78 = 0x73703738;  // "sp78"
    public const uint DATA_TYPE_FPE2 = 0x66706532;  // "fpe2"
    public const uint DATA_TYPE_IOFT = 0x696F6674;  // "ioft"
    public const uint DATA_TYPE_UI8 = 0x75693820;   // "ui8 "
    public const uint DATA_TYPE_UI16 = 0x75693136;  // "ui16"
    public const uint DATA_TYPE_UI32 = 0x75693332;  // "ui32"

    //------------------------------------------------------------------------
    // Struct
    //------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct timeval
    {
        public long tv_sec;
        public long tv_usec;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct timeval_boot
    {
        public long tv_sec;
        public int tv_usec;
        private readonly int _padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct vm_statistics64
    {
        public uint free_count;
        public uint active_count;
        public uint inactive_count;
        public uint wire_count;
        public ulong zero_fill_count;
        public ulong reactivations;
        public ulong pageins;
        public ulong pageouts;
        public ulong faults;
        public ulong cow_faults;
        public ulong lookups;
        public ulong hits;
        public ulong purges;
        public uint purgeable_count;
        public uint speculative_count;
        public ulong decompressions;
        public ulong compressions;
        public ulong swapins;
        public ulong swapouts;
        public uint compressor_page_count;
        public uint throttled_count;
        public uint external_page_count;
        public uint internal_page_count;
        public ulong total_uncompressed_pages_in_compressor;
        public ulong swapped_count;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct xsw_usage
    {
        public ulong xsu_total;
        public ulong xsu_avail;
        public ulong xsu_used;
        public int xsu_pagesize;
        public int xsu_encrypted;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct fsid_t
    {
        public int val0;
        public int val1;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct statfs
    {
        public uint f_bsize;
        public int f_iosize;
        public ulong f_blocks;
        public ulong f_bfree;
        public ulong f_bavail;
        public ulong f_files;
        public ulong f_ffree;
        public fsid_t f_fsid;
        public uint f_owner;
        public uint f_type;
        public uint f_flags;
        public uint f_fssubtype;
        public fixed byte f_fstypename[16];
        public fixed byte f_mntonname[1024];
        public fixed byte f_mntfromname[1024];
        public uint f_flags_ext;
        public fixed uint f_reserved[7];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ifaddrs
    {
        public nint ifa_next;
        public nint ifa_name;
        public uint ifa_flags;
        public nint ifa_addr;
        public nint ifa_netmask;
        public nint ifa_dstaddr;
        public nint ifa_data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct sockaddr
    {
        public byte sa_len;
        public byte sa_family;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct sockaddr_dl
    {
        public byte sdl_len;
        public byte sdl_family;
        public ushort sdl_index;
        public byte sdl_type;
        public byte sdl_nlen;
        public byte sdl_alen;
        public byte sdl_slen;

        public const int DataOffset = 8;
    }

    public static class sockaddr_in
    {
        public const int AddrOffset = 4;
    }

    public static class sockaddr_in6
    {
        public const int AddrOffset = 8;
        public const int ScopeIdOffset = 24;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct if_data
    {
        public byte ifi_type;
        public byte ifi_typelen;
        public byte ifi_physical;
        public byte ifi_addrlen;
        public byte ifi_hdrlen;
        public byte ifi_recvquota;
        public byte ifi_xmitquota;
        public byte ifi_unused1;
        public uint ifi_mtu;
        public uint ifi_metric;
        public uint ifi_baudrate;
        public uint ifi_ipackets;
        public uint ifi_ierrors;
        public uint ifi_opackets;
        public uint ifi_oerrors;
        public uint ifi_collisions;
        public uint ifi_ibytes;
        public uint ifi_obytes;
        public uint ifi_imcasts;
        public uint ifi_omcasts;
        public uint ifi_iqdrops;
        public uint ifi_noproto;
        public uint ifi_recvtiming;
        public uint ifi_xmittiming;
        public long ifi_lastchange_tv_sec;
        public int ifi_lastchange_tv_usec;
        private int ifi_lastchange_pad;
        public uint ifi_unused2;
        public uint ifi_hwassist;
        public uint ifi_reserved1;
        public uint ifi_reserved2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct proc_bsdinfo
    {
        public uint pbi_flags;
        public uint pbi_status;
        public uint pbi_xstatus;
        public uint pbi_pid;
        public uint pbi_ppid;
        public uint pbi_uid;
        public uint pbi_gid;
        public uint pbi_ruid;
        public uint pbi_rgid;
        public uint pbi_svuid;
        public uint pbi_svgid;
        public uint rfu_1;
        public fixed byte pbi_comm[16];
        public fixed byte pbi_name[32];
        public uint pbi_nfiles;
        public uint pbi_pgid;
        public uint pbi_pjobc;
        public uint e_tdev;
        public uint e_tpgid;
        public int pbi_nice;
        public ulong pbi_start_tvsec;
        public ulong pbi_start_tvusec;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct proc_taskinfo
    {
        public ulong pti_virtual_size;
        public ulong pti_resident_size;
        public ulong pti_total_user;
        public ulong pti_total_system;
        public ulong pti_threads_user;
        public ulong pti_threads_system;
        public int pti_policy;
        public int pti_faults;
        public int pti_pageins;
        public int pti_cow_faults;
        public int pti_messages_sent;
        public int pti_messages_received;
        public int pti_syscalls_mach;
        public int pti_syscalls_unix;
        public int pti_csw;
        public int pti_threadnum;
        public int pti_numrunning;
        public int pti_priority;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SMCKeyData_keyInfo_t
    {
        public uint dataSize;
        public uint dataType;
        public byte dataAttributes;
    }

    [StructLayout(LayoutKind.Explicit, Size = 80)]
    internal unsafe struct SMCKeyData_t
    {
        [FieldOffset(0)]
        public uint key;

        [FieldOffset(28)]
        public SMCKeyData_keyInfo_t keyInfo;

        [FieldOffset(40)]
        public byte result;

        [FieldOffset(41)]
        public byte status;

        [FieldOffset(42)]
        public byte data8;

        [FieldOffset(44)]
        public uint data32;

        [FieldOffset(48)]
        public fixed byte bytes[32];
    }

    //------------------------------------------------------------------------
    // Mach
    //------------------------------------------------------------------------

    [DllImport("libSystem.dylib")]
    public static extern uint mach_host_self();

    [DllImport("libSystem.dylib")]
    public static extern uint task_self_trap();

    [DllImport("libSystem.dylib")]
    public static extern int host_processor_info(uint host, int flavor, out int processorCount, out nint processorInfo, out int processorInfoCnt);

    [DllImport("libSystem.dylib")]
    public static extern int vm_deallocate(uint targetTask, nint address, nint size);

    [DllImport("libSystem.dylib")]
    public static extern unsafe int host_statistics64(uint host_priv, int flavor, vm_statistics64* host_info_out, ref int host_info_outCnt);

    [DllImport("libSystem.dylib")]
    public static extern int host_page_size(uint host, out nuint page_size);

    //------------------------------------------------------------------------
    // libc
    //------------------------------------------------------------------------

    [DllImport("libc")]
    public static extern int sysctlbyname([MarshalAs(UnmanagedType.LPStr)] string name, ref timeval oldp, ref int oldlen, IntPtr newp, int newlen);

    [DllImport("libc")]
    public static extern unsafe int sysctlbyname([MarshalAs(UnmanagedType.LPUTF8Str)] string name, void* oldp, ref nint oldlenp, nint newp, nint newlen);

    [DllImport("libc")]
    public static extern unsafe int getloadavg(double* loadavg, int nelem);

    [DllImport("libc")]
    public static extern unsafe int getfsstat(statfs* buf, int bufsize, int mode);

    [DllImport("libc")]
    public static extern int getifaddrs(out nint ifap);

    [DllImport("libc")]
    public static extern void freeifaddrs(nint ifa);

    [DllImport("libc")]
    public static extern unsafe nint inet_ntop(int af, void* src, byte* dst, uint size);

    //------------------------------------------------------------------------
    // libproc
    //------------------------------------------------------------------------

    [DllImport("libproc")]
    public static extern unsafe int proc_listpids(uint type, uint typeinfo, int* buffer, int buffersize);

    [DllImport("libproc")]
    public static extern unsafe int proc_pidinfo(int pid, int flavor, ulong arg, void* buffer, int buffersize);

    [DllImport("libproc")]
    public static extern unsafe int proc_pidpath(int pid, byte* buffer, uint buffersize);

    //------------------------------------------------------------------------
    // CoreFoundation
    //------------------------------------------------------------------------

    private const string CoreFoundationLib = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [DllImport(CoreFoundationLib)]
    public static extern void CFRelease(nint cf);

    [DllImport(CoreFoundationLib)]
    public static extern long CFArrayGetCount(nint theArray);

    [DllImport(CoreFoundationLib)]
    public static extern nint CFArrayGetValueAtIndex(nint theArray, long idx);

    [DllImport(CoreFoundationLib)]
    public static extern nint CFStringCreateWithCString(nint alloc, [MarshalAs(UnmanagedType.LPUTF8Str)] string cStr, uint encoding);

    [DllImport(CoreFoundationLib)]
    public static extern nint CFStringGetCStringPtr(nint theString, uint encoding);

    [DllImport(CoreFoundationLib)]
    public static extern nint CFStringGetLength(nint theString);

    [DllImport(CoreFoundationLib)]
    public static extern unsafe bool CFStringGetCString(nint theString, byte* buffer, nint bufferSize, uint encoding);

    [DllImport(CoreFoundationLib)]
    public static extern nint CFDictionaryGetValue(nint theDict, nint key);

    [DllImport(CoreFoundationLib)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool CFNumberGetValue(nint number, int theType, out int valuePtr);

    [DllImport(CoreFoundationLib)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool CFNumberGetValue(nint number, int theType, ref long valuePtr);

    [DllImport(CoreFoundationLib)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool CFBooleanGetValue(nint boolean);

    [DllImport(CoreFoundationLib)]
    public static extern nuint CFGetTypeID(nint cf);

    [DllImport(CoreFoundationLib)]
    public static extern nuint CFStringGetTypeID();

    [DllImport(CoreFoundationLib)]
    public static extern nuint CFNumberGetTypeID();

    [DllImport(CoreFoundationLib)]
    public static extern nuint CFDictionaryGetTypeID();

    [DllImport(CoreFoundationLib)]
    public static extern nuint CFDataGetTypeID();

    [DllImport(CoreFoundationLib)]
    public static extern nint CFDataGetLength(nint theData);

    [DllImport(CoreFoundationLib)]
    public static extern nint CFDataGetBytePtr(nint theData);

    //------------------------------------------------------------------------
    // IOKit
    //------------------------------------------------------------------------

    private const string IOKitLib = "/System/Library/Frameworks/IOKit.framework/IOKit";

    [DllImport(IOKitLib)]
    public static extern nint IOPSCopyPowerSourcesInfo();

    [DllImport(IOKitLib)]
    public static extern nint IOPSCopyPowerSourcesList(nint blob);

    [DllImport(IOKitLib)]
    public static extern nint IOPSGetPowerSourceDescription(nint blob, nint ps);

    [DllImport(IOKitLib)]
    public static extern int IOServiceGetMatchingServices(uint mainPort, nint matching, ref nint existing);

    [DllImport(IOKitLib)]
    public static extern uint IOServiceGetMatchingService(uint mainPort, nint matching);

    [DllImport(IOKitLib)]
    public static extern uint IOIteratorNext(nint iterator);

    [DllImport(IOKitLib)]
    public static extern int IOObjectRelease(nint @object);

    [DllImport(IOKitLib)]
    public static extern int IOObjectRelease(uint @object);

    [DllImport(IOKitLib)]
    public static extern nint IOServiceMatching([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(IOKitLib)]
    public static extern nint IORegistryEntryCreateCFProperty(uint entry, nint key, nint allocator, uint options);

    [DllImport(IOKitLib)]
    public static extern int IOServiceOpen(uint service, uint owningTask, uint type, out uint connect);

    [DllImport(IOKitLib)]
    public static extern int IOServiceClose(uint connect);

    [DllImport(IOKitLib)]
    public static extern unsafe int IOConnectCallStructMethod(uint connection, uint selector, void* inputStruct, nuint inputStructCnt, void* outputStruct, nuint* outputStructCnt);

    [DllImport(IOKitLib)]
    public static extern int IORegistryEntryCreateCFProperties(uint entry, out nint properties, nint allocator, uint options);

    [DllImport(IOKitLib)]
    public static extern nint IOPSCopyExternalPowerAdapterDetails();

    [DllImport(IOKitLib)]
    public static extern nint IOReportCopyChannelsInGroup([MarshalAs(UnmanagedType.LPUTF8Str)] string? group, [MarshalAs(UnmanagedType.LPUTF8Str)] string? subgroup, ulong a, ulong b, ulong c);

    [DllImport(IOKitLib)]
    public static extern nint IOReportCreateSubscription(nint a, nint channels, out nint b, ulong c, nint d);

    [DllImport(IOKitLib)]
    public static extern nint IOReportCreateSamples(nint subscription, nint channels, nint a);

    [DllImport(IOKitLib)]
    public static extern nint IOReportChannelGetGroup(nint channel);

    [DllImport(IOKitLib)]
    public static extern nint IOReportChannelGetChannelName(nint channel);

    [DllImport(IOKitLib)]
    public static extern nint IOReportChannelGetUnitLabel(nint channel);

    [DllImport(IOKitLib)]
    public static extern long IOReportSimpleGetIntegerValue(nint channel, int idx);

    [DllImport(CoreFoundationLib)]
    public static extern nuint CFArrayGetTypeID();

    [DllImport(CoreFoundationLib)]
    public static extern nint CFDictionaryCreateMutableCopy(nint allocator, nint capacity, nint theDict);

    [DllImport(CoreFoundationLib)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool CFNumberGetValue(nint number, int theType, ref double valuePtr);

    // Additional IOPowerSources Keys
    public const string kIOPSPowerAdapterWattsKey = "Watts";

    // CFNumberType for double
    public const int kCFNumberFloat64Type = 6;

    //------------------------------------------------------------------------
    // Helper
    //------------------------------------------------------------------------

    public static unsafe string? CfStringToManaged(nint cfString)
    {
        if (cfString == nint.Zero)
        {
            return null;
        }

        var ptr = CFStringGetCStringPtr(cfString, kCFStringEncodingUTF8);
        if (ptr != nint.Zero)
        {
            return Marshal.PtrToStringUTF8(ptr);
        }

        var length = CFStringGetLength(cfString);
        if (length <= 0)
        {
            return string.Empty;
        }

        var bufSize = (length * 4) + 1;
        var buf = stackalloc byte[(int)bufSize];
        return CFStringGetCString(cfString, buf, bufSize, kCFStringEncodingUTF8)
            ? Marshal.PtrToStringUTF8((nint)buf)
            : null;
    }
}
