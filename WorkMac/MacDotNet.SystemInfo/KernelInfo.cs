namespace MacDotNet.SystemInfo;

public sealed class KernelInfo
{
    public DateTime UpdateAt { get; private set; }

    public string OsType { get; private set; } = string.Empty;

    public string OsRelease { get; private set; } = string.Empty;

    public string OsVersion { get; private set; } = string.Empty;

    public string? OsProductVersion { get; private set; }

    public int OsRevision { get; private set; }

    public string KernelVersion { get; private set; } = string.Empty;

    public string Hostname { get; private set; } = string.Empty;

    public string Uuid { get; private set; } = string.Empty;

    public DateTimeOffset BootTime { get; private set; }

    public int MaxProc { get; private set; }

    public int MaxFiles { get; private set; }

    public int MaxFilesPerProc { get; private set; }

    public int ArgMax { get; private set; }

    public int SecureLevel { get; private set; }

    internal KernelInfo()
    {
        Update();
    }

    public unsafe bool Update()
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
        Hostname = Helper.GetSysctlString("kern.hostname") ?? string.Empty;
        Uuid = Helper.GetSysctlString("kern.uuid") ?? string.Empty;
        BootTime = bootTime;
        MaxProc = Helper.GetSysctlInt("kern.maxproc");
        MaxFiles = Helper.GetSysctlInt("kern.maxfiles");
        MaxFilesPerProc = Helper.GetSysctlInt("kern.maxfilesperproc");
        ArgMax = Helper.GetSysctlInt("kern.argmax");
        SecureLevel = Helper.GetSysctlInt("kern.securelevel");

        UpdateAt = DateTime.Now;

        return true;
    }
}
