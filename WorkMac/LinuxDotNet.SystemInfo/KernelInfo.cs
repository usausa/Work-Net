namespace LinuxDotNet.SystemInfo;

public sealed class KernelInfo
{
    public DateTime UpdateAt { get; private set; }

    public string OsType { get; private set; } = string.Empty;

    public string OsRelease { get; private set; } = string.Empty;

    public string KernelVersion { get; private set; } = string.Empty;

    public string? OsProductVersion { get; private set; }

    public string? OsName { get; private set; }

    public string? OsPrettyName { get; private set; }

    public string? OsId { get; private set; }

    public string Hostname { get; private set; } = string.Empty;

    public DateTimeOffset BootTime { get; private set; }

    public int MaxProc { get; private set; }

    public int MaxFiles { get; private set; }

    public int MaxFilesPerProc { get; private set; }

    internal KernelInfo()
    {
        Update();
    }

    public bool Update()
    {
        OsType = ReadProcFile("sys/kernel/ostype");
        OsRelease = ReadProcFile("sys/kernel/osrelease");
        KernelVersion = ReadProcFile("sys/kernel/version");
        Hostname = ReadProcFile("sys/kernel/hostname");

        ParseOsRelease();

        MaxProc = ReadProcFileAsInt32("sys/kernel/pid_max");
        MaxFiles = ReadProcFileAsInt32("sys/fs/file-max");
        MaxFilesPerProc = ReadProcFileAsInt32("sys/fs/nr_open");

        ParseBootTime();

        UpdateAt = DateTime.Now;

        return true;
    }

    private void ParseOsRelease()
    {
        const string path = "/etc/os-release";
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            using var reader = new StreamReader(path);
            while (reader.ReadLine() is { } line)
            {
                var eqIndex = line.IndexOf('=');
                if (eqIndex < 0)
                {
                    continue;
                }

                var key = line[..eqIndex];
                var value = line[(eqIndex + 1)..].Trim('"');

                switch (key)
                {
                    case "VERSION_ID":
                        OsProductVersion = value;
                        break;
                    case "NAME":
                        OsName = value;
                        break;
                    case "PRETTY_NAME":
                        OsPrettyName = value;
                        break;
                    case "ID":
                        OsId = value;
                        break;
                }
            }
        }
        catch
        {
            // Ignore
        }
    }

    private void ParseBootTime()
    {
        try
        {
            using var reader = new StreamReader("/proc/stat");
            while (reader.ReadLine() is { } line)
            {
                if (line.StartsWith("btime ", StringComparison.Ordinal))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && Int64.TryParse(parts[1], out var btime))
                    {
                        BootTime = DateTimeOffset.FromUnixTimeSeconds(btime);
                        return;
                    }
                }
            }
        }
        catch
        {
            // Ignore
        }

        BootTime = DateTimeOffset.MinValue;
    }

    private static string ReadProcFile(string relativePath)
    {
        var path = $"/proc/{relativePath}";
        if (File.Exists(path))
        {
            try
            {
                return File.ReadAllText(path).Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        return string.Empty;
    }

    private static int ReadProcFileAsInt32(string relativePath)
    {
        var value = ReadProcFile(relativePath);
        return Int32.TryParse(value, out var result) ? result : 0;
    }
}
