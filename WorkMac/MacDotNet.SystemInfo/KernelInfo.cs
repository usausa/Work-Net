namespace MacDotNet.SystemInfo;

using static MacDotNet.SystemInfo.NativeMethods;

public sealed class KernelInfo
{
    public string OsType { get; }

    public string OsRelease { get; }

    public string OsVersion { get; }

    public string? OsProductVersion { get; }

    public int OsRevision { get; }

    public string KernelVersion { get; }

    public string Uuid { get; }

    public int MaxProc { get; }

    public int MaxFiles { get; }

    public int MaxFilesPerProc { get; }

    public int ArgMax { get; }

    public int SecureLevel { get; }

    public DateTimeOffset BootTime { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    private unsafe KernelInfo()
    {
        var bootTime = DateTimeOffset.MinValue;
        NativeMethods.timeval_boot tv;
        var len = (IntPtr)sizeof(NativeMethods.timeval_boot);
        if (NativeMethods.sysctlbyname("kern.boottime", &tv, ref len, IntPtr.Zero, 0) == 0)
        {
            bootTime = DateTimeOffset.FromUnixTimeSeconds(tv.tv_sec);
        }
        BootTime = bootTime;

        OsType = GetSystemControlString("kern.ostype") ?? string.Empty;
        OsRelease = GetSystemControlString("kern.osrelease") ?? string.Empty;
        OsVersion = GetSystemControlString("kern.osversion") ?? string.Empty;
        OsProductVersion = GetSystemControlString("kern.osproductversion");
        OsRevision = GetSystemControlInt32("kern.osrevision");
        KernelVersion = GetSystemControlString("kern.version") ?? string.Empty;
        Uuid = GetSystemControlString("kern.uuid") ?? string.Empty;
        MaxProc = GetSystemControlInt32("kern.maxproc");
        MaxFiles = GetSystemControlInt32("kern.maxfiles");
        MaxFilesPerProc = GetSystemControlInt32("kern.maxfilesperproc");
        ArgMax = GetSystemControlInt32("kern.argmax");
        SecureLevel = GetSystemControlInt32("kern.securelevel");
    }

    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    public static KernelInfo Create() => new();
}
