using System.Diagnostics;

// [Process]
var process = Process.GetCurrentProcess();

process.Refresh();

Debug.WriteLine($"Uptime: {(DateTime.Now - process.StartTime).TotalMilliseconds}"); // * (start_time_seconds) -
Debug.WriteLine($"CPU Time: {process.TotalProcessorTime.TotalSeconds}"); // cpu_seconds_total -

Debug.WriteLine($"User Time: {process.UserProcessorTime.TotalSeconds}"); // - process.cpu.time[user]
Debug.WriteLine($"System Time: {process.PrivilegedProcessorTime.TotalSeconds}"); // - process.cpu.time[system]

Debug.WriteLine($"Handles: {process.HandleCount}"); // * open_handles [MEMO] OpenTelemetryにこれがない！
Debug.WriteLine($"Threads: {process.Threads.Count}"); // num_threads process.threads

Debug.WriteLine($"Working Set: {process.WorkingSet64}"); // working_set_bytes process.memory.usage
Debug.WriteLine($"Virtual Memory: {process.VirtualMemorySize64}"); // virtual_memory_bytes process.memory.virtual
Debug.WriteLine($"Private Memory: {process.PrivateMemorySize64}"); // private_memory_bytes -

// [GC]
Debug.WriteLine($"TotalMemory: {GC.GetTotalMemory(false)}"); // (total_memory_bytes) -

for (var gen = 0; gen <= GC.MaxGeneration; gen++)
{
    Debug.WriteLine($"Collection Count[{gen}]: {GC.CollectionCount(gen)}"); // collection_count process.runtime.dotnet.gc.collections.count
}

// ...

// [Environment]
Debug.WriteLine($"Version: {Environment.Version}"); // x
Debug.WriteLine($"Current Directory: {Environment.CurrentDirectory}"); // x

Debug.WriteLine($"Processor Count: {Environment.ProcessorCount}"); // process.cpu.count

// Other... .net version ?
// TODO
