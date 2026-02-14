namespace MacDotNet.SystemInfo;

using System.Runtime.InteropServices;

using static MacDotNet.SystemInfo.NativeMethods;

public sealed record ProcessEntry
{
    public required int Pid { get; init; }

    public required int ParentPid { get; init; }

    public required string Name { get; init; }

    public string Path { get; init; } = string.Empty;

    public required uint Uid { get; init; }

    public required uint Gid { get; init; }

    public required int Nice { get; init; }

    public required uint OpenFiles { get; init; }

    public required DateTimeOffset StartTime { get; init; }

    public required int ThreadCount { get; init; }

    public required int RunningThreadCount { get; init; }

    public required ulong VirtualSize { get; init; }

    public required ulong ResidentSize { get; init; }

    public required ulong TotalUserTime { get; init; }

    public required ulong TotalSystemTime { get; init; }

    public required int Faults { get; init; }

    public required int PageIns { get; init; }

    public required int CowFaults { get; init; }

    public required int ContextSwitches { get; init; }

    public required int SyscallsMach { get; init; }

    public required int SyscallsUnix { get; init; }

    public required int Priority { get; init; }
}

public static class ProcessInfo
{
    public static unsafe ProcessEntry[] GetProcesses()
    {
        var bufferSize = proc_listpids(PROC_ALL_PIDS, 0, null, 0);
        if (bufferSize <= 0)
        {
            return [];
        }

        var pidCount = bufferSize / sizeof(int);
        var pids = new int[pidCount];

        fixed (int* pidPtr = pids)
        {
            var actualSize = proc_listpids(PROC_ALL_PIDS, 0, pidPtr, bufferSize);
            if (actualSize <= 0)
            {
                return [];
            }

            var actualCount = Math.Min(actualSize / sizeof(int), pidCount);
            var result = new List<ProcessEntry>();

            var pathBuffer = stackalloc byte[(int)PROC_PIDPATHINFO_MAXSIZE];

            for (var i = 0; i < actualCount; i++)
            {
                var pid = pids[i];
                if (pid == 0)
                {
                    continue;
                }

                proc_bsdinfo bsdInfo;
                var bsdSize = proc_pidinfo(pid, PROC_PIDTBSDINFO, 0, &bsdInfo, sizeof(proc_bsdinfo));
                if (bsdSize < sizeof(proc_bsdinfo))
                {
                    continue;
                }

                proc_taskinfo taskInfo;
                var taskSize = proc_pidinfo(pid, PROC_PIDTASKINFO, 0, &taskInfo, sizeof(proc_taskinfo));
                var hasTaskInfo = taskSize >= sizeof(proc_taskinfo);

                var pathLen = proc_pidpath(pid, pathBuffer, PROC_PIDPATHINFO_MAXSIZE);
                var path = pathLen > 0
                    ? Marshal.PtrToStringUTF8((nint)pathBuffer) ?? string.Empty
                    : string.Empty;

                var name = Marshal.PtrToStringUTF8((nint)bsdInfo.pbi_name);
                if (string.IsNullOrEmpty(name))
                {
                    name = Marshal.PtrToStringUTF8((nint)bsdInfo.pbi_comm) ?? string.Empty;
                }

                var startTime = DateTimeOffset.FromUnixTimeSeconds((long)bsdInfo.pbi_start_tvsec)
                    .AddTicks((long)bsdInfo.pbi_start_tvusec * 10);

                result.Add(new ProcessEntry
                {
                    Pid = pid,
                    ParentPid = (int)bsdInfo.pbi_ppid,
                    Name = name,
                    Path = path,
                    Uid = bsdInfo.pbi_uid,
                    Gid = bsdInfo.pbi_gid,
                    Nice = bsdInfo.pbi_nice,
                    OpenFiles = bsdInfo.pbi_nfiles,
                    StartTime = startTime,
                    ThreadCount = hasTaskInfo ? taskInfo.pti_threadnum : 0,
                    RunningThreadCount = hasTaskInfo ? taskInfo.pti_numrunning : 0,
                    VirtualSize = hasTaskInfo ? taskInfo.pti_virtual_size : 0,
                    ResidentSize = hasTaskInfo ? taskInfo.pti_resident_size : 0,
                    TotalUserTime = hasTaskInfo ? taskInfo.pti_total_user : 0,
                    TotalSystemTime = hasTaskInfo ? taskInfo.pti_total_system : 0,
                    Faults = hasTaskInfo ? taskInfo.pti_faults : 0,
                    PageIns = hasTaskInfo ? taskInfo.pti_pageins : 0,
                    CowFaults = hasTaskInfo ? taskInfo.pti_cow_faults : 0,
                    ContextSwitches = hasTaskInfo ? taskInfo.pti_csw : 0,
                    SyscallsMach = hasTaskInfo ? taskInfo.pti_syscalls_mach : 0,
                    SyscallsUnix = hasTaskInfo ? taskInfo.pti_syscalls_unix : 0,
                    Priority = hasTaskInfo ? taskInfo.pti_priority : 0,
                });
            }

            result.Sort((a, b) => a.Pid.CompareTo(b.Pid));
            return [.. result];
        }
    }
}
