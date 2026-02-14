namespace MacDotNet.SystemInfo;

public sealed class KernelInfo
{
    public string OsType { get; }

    public string OsRelease { get; }

    public string OsVersion { get; }

    public string? OsProductVersion { get; }

    public int OsRevision { get; }

    public string KernelVersion { get; }

    public string Uuid { get; }

    public DateTimeOffset BootTime { get; }

    public int MaxProc { get; }

    public int MaxFiles { get; }

    public int MaxFilesPerProc { get; }

    public int ArgMax { get; }

    public int SecureLevel { get; }

    private unsafe KernelInfo()
    {
        var bootTime = DateTimeOffset.MinValue;
        NativeMethods.timeval_boot tv;
        var len = (nint)sizeof(NativeMethods.timeval_boot);
        if (NativeMethods.sysctlbyname("kern.boottime", &tv, ref len, IntPtr.Zero, 0) == 0)
        {
            bootTime = DateTimeOffset.FromUnixTimeSeconds(tv.tv_sec);
        }

        OsType = Helper.GetSysctlString("kern.ostype") ?? string.Empty;
        OsRelease = Helper.GetSysctlString("kern.osrelease") ?? string.Empty;
        OsVersion = Helper.GetSysctlString("kern.osversion") ?? string.Empty;
        OsProductVersion = Helper.GetSysctlString("kern.osproductversion");
        OsRevision = Helper.GetSysctlInt("kern.osrevision");
        KernelVersion = Helper.GetSysctlString("kern.version") ?? string.Empty;
        Uuid = Helper.GetSysctlString("kern.uuid") ?? string.Empty;
        BootTime = bootTime;
        MaxProc = Helper.GetSysctlInt("kern.maxproc");
        MaxFiles = Helper.GetSysctlInt("kern.maxfiles");
        MaxFilesPerProc = Helper.GetSysctlInt("kern.maxfilesperproc");
        ArgMax = Helper.GetSysctlInt("kern.argmax");
        SecureLevel = Helper.GetSysctlInt("kern.securelevel");
    }

    public static KernelInfo Create() => new();
}
