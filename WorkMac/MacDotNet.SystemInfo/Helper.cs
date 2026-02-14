namespace MacDotNet.SystemInfo;

using System.Runtime.InteropServices;

using static MacDotNet.SystemInfo.NativeMethods;

internal static class Helper
{
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

    public static unsafe ulong GetSysctlUlong(string name)
    {
        ulong value;
        var len = (nint)sizeof(ulong);
        return sysctlbyname(name, &value, ref len, IntPtr.Zero, 0) == 0 ? value : 0;
    }

    public static unsafe string? GetSysctlString(string name)
    {
        var len = (nint)0;
        if (sysctlbyname(name, null, ref len, IntPtr.Zero, 0) != 0 || len <= 0)
        {
            return null;
        }

        if (len > 1024)
        {
            return null;
        }

        var allocatedSize = len;
        var buffer = stackalloc byte[(int)allocatedSize];
        return sysctlbyname(name, buffer, ref len, IntPtr.Zero, 0) == 0
            ? Marshal.PtrToStringUTF8((nint)buffer)
            : null;
    }
}
