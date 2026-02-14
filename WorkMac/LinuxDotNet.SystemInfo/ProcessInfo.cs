namespace LinuxDotNet.SystemInfo;

using System.Runtime.InteropServices;

public sealed record ProcessEntry
{
    public required int Pid { get; init; }

    public required int ParentPid { get; init; }

    public required string Name { get; init; }

    public string Path { get; init; } = string.Empty;

    public string CommandLine { get; init; } = string.Empty;

    public required uint Uid { get; init; }

    public required uint Gid { get; init; }

    public required int Nice { get; init; }

    public required uint OpenFiles { get; init; }

    public required DateTimeOffset StartTime { get; init; }

    public required int ThreadCount { get; init; }

    public required char State { get; init; }

    public string StateName => State switch
    {
        'R' => "Running",
        'S' => "Sleeping",
        'D' => "Disk Sleep",
        'Z' => "Zombie",
        'T' => "Stopped",
        't' => "Tracing Stop",
        'X' or 'x' => "Dead",
        'K' => "Wakekill",
        'W' => "Waking",
        'P' => "Parked",
        'I' => "Idle",
        _ => "Unknown",
    };

    public required ulong VirtualSize { get; init; }

    public required ulong ResidentSize { get; init; }

    public required ulong SharedSize { get; init; }

    public required ulong UserTime { get; init; }

    public required ulong SystemTime { get; init; }

    public required long MinorFaults { get; init; }

    public required long MajorFaults { get; init; }

    public required int Priority { get; init; }
}

public static class ProcessInfo
{
    private static readonly long ClkTck;
    private static readonly long BootTimeSec;
    private static readonly long PageSize;

    static ProcessInfo()
    {
        ClkTck = GetClkTck();
        BootTimeSec = GetBootTime();
        PageSize = Environment.SystemPageSize;
    }

    public static ProcessEntry[] GetProcesses()
    {
        var result = new List<ProcessEntry>();

        foreach (var dir in Directory.EnumerateDirectories("/proc"))
        {
            var pidStr = Path.GetFileName(dir);
            if (!Int32.TryParse(pidStr, out var pid))
            {
                continue;
            }

            var entry = GetProcess(pid);
            if (entry is not null)
            {
                result.Add(entry);
            }
        }

        result.Sort((a, b) => a.Pid.CompareTo(b.Pid));
        return [.. result];
    }

    public static ProcessEntry? GetProcess(int pid)
    {
        var procPath = $"/proc/{pid}";

        if (!Directory.Exists(procPath))
        {
            return null;
        }

        var statPath = Path.Combine(procPath, "stat");
        if (!File.Exists(statPath))
        {
            return null;
        }

        try
        {
            var statContent = File.ReadAllText(statPath);
            var parsed = ParseStat(statContent);
            if (parsed is null)
            {
                return null;
            }

            var statusInfo = ParseStatus(Path.Combine(procPath, "status"));

            var exePath = string.Empty;
            try
            {
                var exeLink = Path.Combine(procPath, "exe");
                if (File.Exists(exeLink))
                {
                    exePath = Path.GetFullPath(exeLink);
                }
            }
            catch
            {
                // Permission denied
            }

            var cmdLine = string.Empty;
            try
            {
                var cmdLinePath = Path.Combine(procPath, "cmdline");
                if (File.Exists(cmdLinePath))
                {
                    cmdLine = File.ReadAllText(cmdLinePath).Replace('\0', ' ').Trim();
                }
            }
            catch
            {
                // Permission denied
            }

            uint openFiles = 0;
            try
            {
                var fdPath = Path.Combine(procPath, "fd");
                if (Directory.Exists(fdPath))
                {
                    openFiles = (uint)Directory.GetFiles(fdPath).Length;
                }
            }
            catch
            {
                // Permission denied
            }

            var startTimeTicks = parsed.StartTime;
            var startTimeSec = BootTimeSec + (long)(startTimeTicks / (ulong)ClkTck);
            var startTime = DateTimeOffset.FromUnixTimeSeconds(startTimeSec);

            return new ProcessEntry
            {
                Pid = pid,
                ParentPid = parsed.Ppid,
                Name = parsed.Comm,
                Path = exePath,
                CommandLine = cmdLine,
                Uid = statusInfo.Uid,
                Gid = statusInfo.Gid,
                Nice = parsed.Nice,
                OpenFiles = openFiles,
                StartTime = startTime,
                ThreadCount = statusInfo.Threads,
                State = parsed.State,
                VirtualSize = parsed.Vsize,
                ResidentSize = (ulong)parsed.Rss * (ulong)PageSize,
                SharedSize = statusInfo.RssShared,
                UserTime = parsed.Utime,
                SystemTime = parsed.Stime,
                MinorFaults = parsed.Minflt,
                MajorFaults = parsed.Majflt,
                Priority = parsed.Priority,
            };
        }
        catch
        {
            return null;
        }
    }

    private sealed class StatInfo
    {
        public required string Comm { get; init; }
        public required char State { get; init; }
        public required int Ppid { get; init; }
        public required int Priority { get; init; }
        public required int Nice { get; init; }
        public required ulong Utime { get; init; }
        public required ulong Stime { get; init; }
        public required long Minflt { get; init; }
        public required long Majflt { get; init; }
        public required ulong Vsize { get; init; }
        public required long Rss { get; init; }
        public required ulong StartTime { get; init; }
    }

    private static StatInfo? ParseStat(string content)
    {
        var commStart = content.IndexOf('(');
        var commEnd = content.LastIndexOf(')');
        if (commStart < 0 || commEnd < 0 || commEnd <= commStart)
        {
            return null;
        }

        var comm = content.Substring(commStart + 1, commEnd - commStart - 1);
        var rest = content[(commEnd + 2)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (rest.Length < 20)
        {
            return null;
        }

        return new StatInfo
        {
            Comm = comm,
            State = rest[0].Length > 0 ? rest[0][0] : '?',
            Ppid = Int32.TryParse(rest[1], out var ppid) ? ppid : 0,
            Priority = Int32.TryParse(rest[15], out var prio) ? prio : 0,
            Nice = Int32.TryParse(rest[16], out var nice) ? nice : 0,
            Utime = UInt64.TryParse(rest[11], out var utime) ? utime : 0,
            Stime = UInt64.TryParse(rest[12], out var stime) ? stime : 0,
            Minflt = Int64.TryParse(rest[7], out var minflt) ? minflt : 0,
            Majflt = Int64.TryParse(rest[9], out var majflt) ? majflt : 0,
            Vsize = UInt64.TryParse(rest[20], out var vsize) ? vsize : 0,
            Rss = Int64.TryParse(rest[21], out var rss) ? rss : 0,
            StartTime = UInt64.TryParse(rest[19], out var starttime) ? starttime : 0,
        };
    }

    private sealed class StatusInfo
    {
        public uint Uid { get; set; }
        public uint Gid { get; set; }
        public int Threads { get; set; }
        public ulong RssShared { get; set; }
    }

    private static StatusInfo ParseStatus(string path)
    {
        var info = new StatusInfo();

        if (!File.Exists(path))
        {
            return info;
        }

        try
        {
            using var reader = new StreamReader(path);
            while (reader.ReadLine() is { } line)
            {
                var span = line.AsSpan();

                if (span.StartsWith("Uid:"))
                {
                    var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && UInt32.TryParse(parts[1], out var uid))
                    {
                        info.Uid = uid;
                    }
                }
                else if (span.StartsWith("Gid:"))
                {
                    var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && UInt32.TryParse(parts[1], out var gid))
                    {
                        info.Gid = gid;
                    }
                }
                else if (span.StartsWith("Threads:"))
                {
                    var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && Int32.TryParse(parts[1], out var threads))
                    {
                        info.Threads = threads;
                    }
                }
                else if (span.StartsWith("RssShmem:"))
                {
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && UInt64.TryParse(parts[1], out var rssShared))
                    {
                        info.RssShared = rssShared * 1024;
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return info;
    }

    private static long GetClkTck()
    {
        try
        {
            return sysconf(2); // _SC_CLK_TCK = 2
        }
        catch
        {
            return 100; // Default value
        }
    }

    private static long GetBootTime()
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
                        return btime;
                    }
                }
            }
        }
        catch
        {
            // Ignore
        }

        return 0;
    }

    [DllImport("libc", SetLastError = true)]
    private static extern long sysconf(int name);
}
