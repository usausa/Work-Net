namespace MacDotNet.SystemInfo;

using static MacDotNet.SystemInfo.NativeMethods;

public sealed class LoadAverageInfo
{
    public DateTime UpdateAt { get; private set; }

    public double Average1 { get; private set; }

    public double Average5 { get; private set; }

    public double Average15 { get; private set; }

    internal LoadAverageInfo()
    {
        Update();
    }

    public unsafe bool Update()
    {
        var loadavg = stackalloc double[3];
        var count = getloadavg(loadavg, 3);
        if (count < 1)
        {
            return false;
        }

        Average1 = count >= 1 ? loadavg[0] : 0;
        Average5 = count >= 2 ? loadavg[1] : 0;
        Average15 = count >= 3 ? loadavg[2] : 0;

        UpdateAt = DateTime.Now;

        return true;
    }
}
