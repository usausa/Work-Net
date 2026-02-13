namespace MacDotNet.SystemInfo;

#pragma warning disable CA1024
public static class PlatformProvider
{
    public static UptimeInfo GetUptime() => new();
}
