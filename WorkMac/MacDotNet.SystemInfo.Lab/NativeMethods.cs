namespace MacDotNet.SystemInfo.Lab;

using System.Runtime.InteropServices;

#pragma warning disable CA1051
#pragma warning disable CA1707
#pragma warning disable CA2101
#pragma warning disable SYSLIB1054

// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable IdentifierTypo

internal static partial class NativeMethods
{
    // ========================================
    // Constants
    // ========================================

    public const int KERN_SUCCESS = 0;

    // Host Info
    public const int HOST_BASIC_INFO = 1;
    public const int HOST_CPU_LOAD_INFO = 3;
    public const int HOST_VM_INFO64 = 4;

    // Processor Info
    public const int PROCESSOR_CPU_LOAD_INFO = 2;
    public const int CPU_STATE_MAX = 4;
    public const int CPU_STATE_USER = 0;
    public const int CPU_STATE_SYSTEM = 1;
    public const int CPU_STATE_IDLE = 2;
    public const int CPU_STATE_NICE = 3;

    // CFNumber types
    public const int kCFNumberSInt32Type = 3;
    public const int kCFNumberSInt64Type = 4;
    public const int kCFNumberFloat64Type = 6;

    // CFString encoding
    public const uint kCFStringEncodingUTF8 = 0x08000100;

    // SMC
    public const int KERNEL_INDEX_SMC = 2;
    public const byte SMC_CMD_READ_BYTES = 5;
    public const byte SMC_CMD_READ_INDEX = 8;
    public const byte SMC_CMD_READ_KEYINFO = 9;

    // IOKit
    public const string kIOBlockStorageDeviceClass = "IOBlockStorageDevice";

    // Network
    public const int AF_INET = 2;
    public const int AF_INET6 = 30;
    public const int AF_LINK = 18;
    public const uint INET_ADDRSTRLEN = 16;
    public const uint INET6_ADDRSTRLEN = 46;
    public const int NI_MAXHOST = 1025;
    public const int NI_NUMERICHOST = 0x02;

    // ========================================
    // Mach / Host
    // ========================================

    [LibraryImport("libSystem.dylib")]
    public static partial uint mach_host_self();

    [LibraryImport("libSystem.dylib")]
    public static partial uint mach_task_self();

    [LibraryImport("libSystem.dylib")]
    public static partial int host_page_size(uint host, out nuint pageSize);

    [LibraryImport("libSystem.dylib")]
    public static unsafe partial int host_statistics64(uint host, int flavor, void* hostInfo, ref int count);

    [LibraryImport("libSystem.dylib")]
    public static unsafe partial int host_processor_info(uint host, int flavor, out uint processorCount, out int* processorInfo, out int processorInfoCount);

    [LibraryImport("libSystem.dylib")]
    public static unsafe partial int vm_deallocate(uint task, nint address, nuint size);

    // ========================================
    // Sysctl
    // ========================================

    [DllImport("libSystem.dylib", CharSet = CharSet.Ansi)]
    public static extern unsafe int sysctlbyname(string name, void* oldp, ref nint oldlenp, IntPtr newp, nint newlen);

    // ========================================
    // CoreFoundation
    // ========================================

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static partial void CFRelease(nint cf);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static partial nint CFRetain(nint cf);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation", CharSet = CharSet.Ansi)]
    public static extern nint CFStringCreateWithCString(nint alloc, string cStr, uint encoding);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static partial nint CFStringGetCStringPtr(nint theString, uint encoding);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static partial nint CFStringGetLength(nint theString);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool CFStringGetCString(nint theString, byte* buffer, nint bufferSize, uint encoding);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static partial nint CFDictionaryGetValue(nint theDict, nint key);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static partial nint CFDictionaryCreateMutableCopy(nint allocator, nint capacity, nint theDict);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CFNumberGetValue(nint number, int theType, out int valuePtr);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CFNumberGetValue(nint number, int theType, ref long valuePtr);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CFNumberGetValue(nint number, int theType, ref double valuePtr);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CFBooleanGetValue(nint boolean);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static partial nuint CFGetTypeID(nint cf);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static partial nuint CFStringGetTypeID();

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static partial nuint CFNumberGetTypeID();

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static partial nuint CFDictionaryGetTypeID();

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static partial nuint CFDataGetTypeID();

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static partial nuint CFArrayGetTypeID();

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static partial nint CFArrayGetCount(nint theArray);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static partial nint CFArrayGetValueAtIndex(nint theArray, nint idx);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static partial nint CFDataGetLength(nint theData);

    [LibraryImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    public static partial nint CFDataGetBytePtr(nint theData);

    // ========================================
    // IOKit
    // ========================================

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit", CharSet = CharSet.Ansi)]
    public static extern nint IOServiceMatching(string name);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial uint IOServiceGetMatchingService(uint mainPort, nint matching);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial int IOServiceGetMatchingServices(uint mainPort, nint matching, ref nint existing);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial int IOServiceOpen(uint service, uint owningTask, uint type, out uint connect);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial int IOServiceClose(uint connect);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial int IOObjectRelease(uint obj);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial uint IOIteratorNext(nint iterator);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial nint IORegistryEntryCreateCFProperty(uint entry, nint key, nint allocator, uint options);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial int IORegistryEntryCreateCFProperties(uint entry, out nint properties, nint allocator, uint options);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial int IORegistryEntryGetParentEntry(uint entry, nint plane, out uint parent);

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit", CharSet = CharSet.Ansi)]
    public static extern int IOObjectConformsTo(uint obj, string className);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static unsafe partial int IOConnectCallStructMethod(uint connection, uint selector, void* inputStruct, nuint inputStructCnt, void* outputStruct, nuint* outputStructCnt);

    // ========================================
    // IOPowerSources
    // ========================================

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial nint IOPSCopyPowerSourcesInfo();

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial nint IOPSCopyPowerSourcesList(nint blob);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial nint IOPSGetPowerSourceDescription(nint blob, nint ps);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial nint IOPSCopyExternalPowerAdapterDetails();

    // IOPowerSources Keys
    public const string kIOPSPowerAdapterWattsKey = "Watts";

    // ========================================
    // IOReport (Apple Silicon Power)
    // ========================================

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit", CharSet = CharSet.Ansi)]
    public static extern nint IOReportCopyChannelsInGroup(string? group, string? subgroup, ulong a, ulong b, ulong c);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial int IOReportMergeChannels(nint a, nint b, nint c);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial nint IOReportCreateSubscription(nint a, nint channels, out nint b, ulong c, nint d);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial nint IOReportCreateSamples(nint subscription, nint channels, nint a);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial nint IOReportChannelGetGroup(nint channel);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial nint IOReportChannelGetChannelName(nint channel);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial nint IOReportChannelGetUnitLabel(nint channel);

    [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static partial long IOReportSimpleGetIntegerValue(nint channel, int idx);

    // ========================================
    // SystemConfiguration
    // ========================================

    [LibraryImport("/System/Library/Frameworks/SystemConfiguration.framework/SystemConfiguration")]
    public static partial nint SCDynamicStoreCopyValue(nint store, nint key);

    [LibraryImport("/System/Library/Frameworks/SystemConfiguration.framework/SystemConfiguration")]
    public static partial nint SCNetworkInterfaceCopyAll();

    [LibraryImport("/System/Library/Frameworks/SystemConfiguration.framework/SystemConfiguration")]
    public static partial nint SCNetworkInterfaceGetBSDName(nint iface);

    [LibraryImport("/System/Library/Frameworks/SystemConfiguration.framework/SystemConfiguration")]
    public static partial nint SCNetworkInterfaceGetInterfaceType(nint iface);

    [LibraryImport("/System/Library/Frameworks/SystemConfiguration.framework/SystemConfiguration")]
    public static partial nint SCNetworkInterfaceGetLocalizedDisplayName(nint iface);

    [LibraryImport("/System/Library/Frameworks/SystemConfiguration.framework/SystemConfiguration")]
    public static partial nint SCNetworkInterfaceGetHardwareAddressString(nint iface);

    [LibraryImport("/System/Library/Frameworks/SystemConfiguration.framework/SystemConfiguration")]
    public static partial nint CFNetworkCopySystemProxySettings();

    // SCNetworkInterfaceType constants (as strings)
    public const string kSCNetworkInterfaceTypeEthernet = "Ethernet";
    public const string kSCNetworkInterfaceTypeIEEE80211 = "IEEE80211";
    public const string kSCNetworkInterfaceTypeBluetooth = "Bluetooth";

    // ========================================
    // CoreWLAN (WiFi)
    // ========================================

    // Note: CoreWLAN is an Objective-C framework, requires ObjC runtime interop
    // For simplicity, using system_profiler or networksetup commands

    // ========================================
    // Network
    // ========================================

    [LibraryImport("libSystem.dylib")]
    public static partial int getifaddrs(out nint ifap);

    [LibraryImport("libSystem.dylib")]
    public static partial void freeifaddrs(nint ifap);

    [LibraryImport("libSystem.dylib")]
    public static unsafe partial nint inet_ntop(int af, void* src, byte* dst, uint size);

    [LibraryImport("libSystem.dylib")]
    public static unsafe partial int getnameinfo(sockaddr* sa, uint salen, byte* host, uint hostlen, byte* serv, uint servlen, int flags);

    // ========================================
    // Structs
    // ========================================

    [StructLayout(LayoutKind.Sequential)]
    public struct host_cpu_load_info
    {
        public uint cpu_ticks_user;
        public uint cpu_ticks_system;
        public uint cpu_ticks_idle;
        public uint cpu_ticks_nice;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct vm_statistics64
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

    public const int HOST_VM_INFO64_COUNT = 38;

    [StructLayout(LayoutKind.Sequential)]
    public struct sockaddr
    {
        public byte sa_len;
        public byte sa_family;
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
        public long ifi_lastchange_sec;
        public int ifi_lastchange_usec;
    }

    // SMC Structures
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SMCKeyData_vers_t
    {
        public fixed byte major[1];
        public fixed byte minor[1];
        public fixed byte build[1];
        public fixed byte reserved[1];
        public ushort release;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SMCKeyData_pLimitData_t
    {
        public ushort version;
        public ushort length;
        public uint cpuPLimit;
        public uint gpuPLimit;
        public uint memPLimit;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SMCKeyData_keyInfo_t
    {
        public uint dataSize;
        public uint dataType;
        public byte dataAttributes;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SMCKeyData_t
    {
        public uint key;
        public SMCKeyData_vers_t vers;
        public SMCKeyData_pLimitData_t pLimitData;
        public SMCKeyData_keyInfo_t keyInfo;
        public byte result;
        public byte status;
        public byte data8;
        public uint data32;
        public fixed byte bytes[32];
    }

    // SMC Data Types
    public static readonly uint DATA_TYPE_FLT = KeyToUInt32("flt ");
    public static readonly uint DATA_TYPE_SP78 = KeyToUInt32("sp78");
    public static readonly uint DATA_TYPE_FPE2 = KeyToUInt32("fpe2");
    public static readonly uint DATA_TYPE_IOFT = KeyToUInt32("ioft");
    public static readonly uint DATA_TYPE_UI8 = KeyToUInt32("ui8 ");
    public static readonly uint DATA_TYPE_UI16 = KeyToUInt32("ui16");
    public static readonly uint DATA_TYPE_UI32 = KeyToUInt32("ui32");

    public static uint KeyToUInt32(string key) =>
        ((uint)key[0] << 24) | ((uint)key[1] << 16) | ((uint)key[2] << 8) | key[3];

    public static string UInt32ToKey(uint key) => new(
    [
        (char)((key >> 24) & 0xFF),
        (char)((key >> 16) & 0xFF),
        (char)((key >> 8) & 0xFF),
        (char)(key & 0xFF),
    ]);

    // ========================================
    // Helper Methods
    // ========================================

    public static unsafe string? GetSysctlString(string name)
    {
        var len = (nint)0;
        if (sysctlbyname(name, null, ref len, IntPtr.Zero, 0) != 0 || len <= 0)
        {
            return null;
        }

        var buffer = stackalloc byte[(int)len];
        return sysctlbyname(name, buffer, ref len, IntPtr.Zero, 0) == 0
            ? Marshal.PtrToStringUTF8((nint)buffer)
            : null;
    }

    public static unsafe int GetSysctlInt(string name)
    {
        int value;
        var len = (nint)sizeof(int);
        return sysctlbyname(name, &value, ref len, IntPtr.Zero, 0) == 0 ? value : 0;
    }

    public static unsafe long GetSysctlLong(string name)
    {
        long value;
        var len = (nint)sizeof(long);
        return sysctlbyname(name, &value, ref len, IntPtr.Zero, 0) == 0 ? value : 0;
    }

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
