namespace MacDotNet.SystemInfo;

using System.Runtime.InteropServices;

using static MacDotNet.SystemInfo.NativeMethods;

public sealed class Uptime
{
    public DateTime UpdateAt { get; private set; }

    public TimeSpan Elapsed { get; private set; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal Uptime()
    {
        Update();
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    // ReSharper disable StringLiteralTypo
    public bool Update()
    {
        var time = new timeval { tv_sec = 0, tv_usec = 0 };
        var size = Marshal.SizeOf<timeval>();
        if (sysctlbyname("kern.boottime", ref time, ref size, IntPtr.Zero, 0) != 0)
        {
            return false;
        }

        var boot = DateTimeOffset.FromUnixTimeMilliseconds((time.tv_sec * 1000) + (time.tv_usec / 1000));
        Elapsed = DateTimeOffset.Now - boot;

        UpdateAt = DateTime.Now;

        return true;
    }
    // ReSharper restore StringLiteralTypo
}
