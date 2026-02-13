namespace WorkProcess;

using System.Runtime.InteropServices;

using static WorkProcess.NativeMethods;

// .NET標準のProcess (System.Diagnostics.Process) との違い:
// - System.Diagnostics.Processは内部的にsysctl(KERN_PROC)を使用しており、
//   libprocのproc_listpids/proc_pidinfoとは異なるカーネルインターフェースを使用
// - libprocはMach taskレベルの詳細情報 (ページフォルト数、コンテキストスイッチ数、
//   Mach/UNIXシステムコール数、コピーオンライトフォルト数) を直接取得可能
// - System.Diagnostics.Processではスレッド数は取得できるが、
//   実行中のスレッド数 (pti_numrunning) は取得できない
// - proc_pidpathでプロセスのフルパスを直接取得可能
//   (.NETではProcess.MainModule?.FileName経由だがmacOSでは制限あり)
// - libprocはPID列挙が軽量 (Process.GetProcesses()はプロセス毎にProcessオブジェクトを生成)
// - proc_taskinfo経由でページイン数やシステムコール統計にアクセス可能
//   (System.Diagnostics.Processには対応するプロパティがない)

internal static class Program
{
    public static void Main()
    {
        var processes = ProcessInfoProvider.GetProcesses();
        if (processes.Length == 0)
        {
            Console.WriteLine("No processes found.");
            return;
        }

        // サマリー
        var totalThreads = 0;
        foreach (var p in processes)
        {
            totalThreads += p.ThreadCount;
        }

        Console.WriteLine($"Total Processes: {processes.Length}");
        Console.WriteLine($"Total Threads:   {totalThreads}");
        Console.WriteLine();

        // プロセス一覧
        Console.WriteLine($"{"PID",6} {"PPID",6} {"THR",4} {"RSS",10} {"VSIZE",10} {"UID",6} {"NAME"}");
        Console.WriteLine(new string('-', 70));

        foreach (var p in processes)
        {
            Console.WriteLine(
                $"{p.Pid,6} {p.ParentPid,6} {p.ThreadCount,4} {FormatBytes(p.ResidentSize),10} " +
                $"{FormatBytes(p.VirtualSize),10} {p.Uid,6} {p.Name}");
        }
    }

    private static string FormatBytes(ulong bytes) => bytes switch
    {
        >= 1UL << 30 => $"{bytes / (double)(1UL << 30):F1} GiB",
        >= 1UL << 20 => $"{bytes / (double)(1UL << 20):F1} MiB",
        >= 1UL << 10 => $"{bytes / (double)(1UL << 10):F1} KiB",
        _ => $"{bytes} B"
    };
}

// プロセス情報
internal sealed record ProcessEntry
{
    // プロセスID
    public required int Pid { get; init; }

    // 親プロセスID
    public required int ParentPid { get; init; }

    // プロセス名
    public required string Name { get; init; }

    // プロセスのフルパス (取得できない場合は空文字)
    // .NETのProcess.MainModule?.FileNameより直接的に取得可能
    public string Path { get; init; } = string.Empty;

    // ユーザーID
    public required uint Uid { get; init; }

    // グループID
    public required uint Gid { get; init; }

    // Nice値
    public required int Nice { get; init; }

    // 開いているファイル数
    public required uint OpenFiles { get; init; }

    // プロセス開始時刻
    public required DateTimeOffset StartTime { get; init; }

    // スレッド数
    public required int ThreadCount { get; init; }

    // 実行中のスレッド数 (.NETのProcessでは取得不可)
    public required int RunningThreadCount { get; init; }

    // 仮想メモリサイズ(バイト)
    public required ulong VirtualSize { get; init; }

    // 物理メモリ常駐サイズ(バイト)
    public required ulong ResidentSize { get; init; }

    // ユーザーCPU時間(Mach ticks)
    public required ulong TotalUserTime { get; init; }

    // システムCPU時間(Mach ticks)
    public required ulong TotalSystemTime { get; init; }

    // ページフォルト数
    public required int Faults { get; init; }

    // ページイン数 (.NETのProcessでは取得不可)
    public required int PageIns { get; init; }

    // コピーオンライトフォルト数 (.NETのProcessでは取得不可)
    public required int CowFaults { get; init; }

    // コンテキストスイッチ数 (.NETのProcessでは取得不可)
    public required int ContextSwitches { get; init; }

    // Machシステムコール数 (.NETのProcessでは取得不可)
    public required int SyscallsMach { get; init; }

    // UNIXシステムコール数 (.NETのProcessでは取得不可)
    public required int SyscallsUnix { get; init; }

    // タスクプライオリティ
    public required int Priority { get; init; }
}

// プロセス情報取得
internal static class ProcessInfoProvider
{
    public static unsafe ProcessEntry[] GetProcesses()
    {
        // 1回目: バッファサイズ取得
        var bufferSize = proc_listpids(PROC_ALL_PIDS, 0, null, 0);
        if (bufferSize <= 0)
        {
            return [];
        }

        // PIDバッファ確保
        var pidCount = bufferSize / sizeof(int);
        var pids = new int[pidCount];

        fixed (int* pidPtr = pids)
        {
            // 2回目: PID一覧取得
            var actualSize = proc_listpids(PROC_ALL_PIDS, 0, pidPtr, bufferSize);
            if (actualSize <= 0)
            {
                return [];
            }

            // バッファサイズ以下に制限 (2回目の呼び出しで件数が増えた場合の安全対策)
            var actualCount = Math.Min(actualSize / sizeof(int), pidCount);
            var result = new List<ProcessEntry>();

            // パスバッファを事前に1回だけ確保 (ループ内でstackallocしない)
            var pathBuffer = stackalloc byte[(int)PROC_PIDPATHINFO_MAXSIZE];

            for (var i = 0; i < actualCount; i++)
            {
                var pid = pids[i];
                if (pid == 0)
                {
                    continue;
                }

                // BSD info取得 (権限不足等で失敗する場合はスキップ)
                proc_bsdinfo bsdInfo;
                var bsdSize = proc_pidinfo(pid, PROC_PIDTBSDINFO, 0, &bsdInfo, sizeof(proc_bsdinfo));
                if (bsdSize < sizeof(proc_bsdinfo))
                {
                    continue;
                }

                // Task info取得 (失敗してもBSD infoのみで登録する)
                proc_taskinfo taskInfo;
                var taskSize = proc_pidinfo(pid, PROC_PIDTASKINFO, 0, &taskInfo, sizeof(proc_taskinfo));
                var hasTaskInfo = taskSize >= sizeof(proc_taskinfo);

                // パス取得
                var pathLen = proc_pidpath(pid, pathBuffer, PROC_PIDPATHINFO_MAXSIZE);
                var path = pathLen > 0
                    ? Marshal.PtrToStringUTF8((nint)pathBuffer) ?? string.Empty
                    : string.Empty;

                // 名前取得 (pbi_nameを優先、空ならpbi_commを使用)
                var name = Marshal.PtrToStringUTF8((nint)bsdInfo.pbi_name);
                if (string.IsNullOrEmpty(name))
                {
                    name = Marshal.PtrToStringUTF8((nint)bsdInfo.pbi_comm) ?? string.Empty;
                }

                var startTime = DateTimeOffset.FromUnixTimeSeconds((long)bsdInfo.pbi_start_tvsec)
                    .AddTicks((long)bsdInfo.pbi_start_tvusec * 10); // 1μs = 10 ticks

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

// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
#pragma warning disable CA2101
#pragma warning disable CA5392
#pragma warning disable CS8981
internal static class NativeMethods
{
    // proc_listpids type (sys/proc_info.h)
    public const uint PROC_ALL_PIDS = 1;

    // proc_pidinfo flavor (sys/proc_info.h)
    public const int PROC_PIDTBSDINFO = 3;
    public const int PROC_PIDTASKINFO = 4;

    // proc_pidpath buffer size (sys/proc_info.h)
    public const uint PROC_PIDPATHINFO_MAXSIZE = 4096; // 4 * MAXPATHLEN

    // MAXCOMLEN (sys/param.h)
    public const int MAXCOMLEN = 16;

    //------------------------------------------------------------------------
    // Struct
    //------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct proc_bsdinfo
    {
        public uint pbi_flags;
        public uint pbi_status;
        public uint pbi_xstatus;
        public uint pbi_pid;
        public uint pbi_ppid;
        public uint pbi_uid;               // uid_t
        public uint pbi_gid;               // gid_t
        public uint pbi_ruid;              // real uid
        public uint pbi_rgid;              // real gid
        public uint pbi_svuid;             // saved uid
        public uint pbi_svgid;             // saved gid
        public uint rfu_1;                 // reserved
        public fixed byte pbi_comm[16];    // MAXCOMLEN
        public fixed byte pbi_name[32];    // 2 * MAXCOMLEN
        public uint pbi_nfiles;
        public uint pbi_pgid;
        public uint pbi_pjobc;
        public uint e_tdev;
        public uint e_tpgid;
        public int pbi_nice;
        public ulong pbi_start_tvsec;      // start time (seconds)
        public ulong pbi_start_tvusec;     // start time (microseconds)
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct proc_taskinfo
    {
        public ulong pti_virtual_size;      // virtual memory size (bytes)
        public ulong pti_resident_size;     // resident memory size (bytes)
        public ulong pti_total_user;        // total user time (mach ticks)
        public ulong pti_total_system;      // total system time (mach ticks)
        public ulong pti_threads_user;      // existing threads user time
        public ulong pti_threads_system;    // existing threads system time
        public int pti_policy;              // default policy for new threads
        public int pti_faults;              // page faults
        public int pti_pageins;             // pageins
        public int pti_cow_faults;          // copy-on-write faults
        public int pti_messages_sent;       // mach messages sent
        public int pti_messages_received;   // mach messages received
        public int pti_syscalls_mach;       // mach system calls
        public int pti_syscalls_unix;       // unix system calls
        public int pti_csw;                 // context switches
        public int pti_threadnum;           // number of threads
        public int pti_numrunning;          // number of running threads
        public int pti_priority;            // task priority
    }

    //------------------------------------------------------------------------
    // P/Invoke (libproc)
    //------------------------------------------------------------------------

    [DllImport("libproc")]
    public static extern unsafe int proc_listpids(uint type, uint typeinfo, int* buffer, int buffersize);

    [DllImport("libproc")]
    public static extern unsafe int proc_pidinfo(int pid, int flavor, ulong arg, void* buffer, int buffersize);

    [DllImport("libproc")]
    public static extern unsafe int proc_pidpath(int pid, byte* buffer, uint buffersize);
}
