
using System.Diagnostics;

var list = new List<PerformanceCounter>();

// Load
//list.AddRange(Create("Processor", "% Processor Time")); // CPU load
//list.AddRange(Create("System", "Processor Queue Length")); // Processor queue length
//list.AddRange(Create("Memory", "Pages/sec")); // Memory pages/sec
//list.AddRange(Create("LogicalDisk", "% Disk Time")); // Disk time
//list.AddRange(Create("PhysicalDisk", "Current Disk Queue Length")); // Disk queue length

// Storage
//list.AddRange(Create("LogicalDisk", "% Free Space")); // Storage free

// System
//list.AddRange(Create("TCPv4", "Connections Established")); // TCPv4
//list.AddRange(Create("TCPv6", "Connections Established")); // TCPv6
//list.AddRange(Create("System", "Processes")); // Process
//list.AddRange(Create("Process", "Thread Count", "_Total")); // Threads
//list.AddRange(Create("Process", "Handle Count", "_Total")); // Handle
//list.AddRange(Create("System", "System Up Time")); // Uptime * 1.1574074074074073e-005 // (1 / 86400.0) days

// HyperV
//list.AddRange(Create("Hyper-V Virtual Machine Health Summary", "Health Ok")); // Ok
//list.AddRange(Create("Hyper-V Virtual Machine Health Summary", "Health Critical")); // Error
//list.AddRange(Create("Hyper-V VM Vid Driver", "VidPartitions")); // Running

// Show
while (true)
{
    Debug.WriteLine("----------");
    foreach (var counter in list)
    {
        Debug.WriteLine($"{counter.CategoryName}/{counter.CounterName}/{counter.InstanceName} = {counter.NextValue()}");
    }

    Thread.Sleep(3000);
}

// Create
static IEnumerable<PerformanceCounter> Create(string category, string counter, string? instance = null)
{
    if (!String.IsNullOrEmpty(instance))
    {

        yield return new PerformanceCounter(category, counter, instance);
    }
    else
    {
        var pcc = new PerformanceCounterCategory(category);
        if (pcc.CategoryType == PerformanceCounterCategoryType.SingleInstance)
        {
            yield return new PerformanceCounter(category, counter);
        }
        else
        {
            var names = pcc.GetInstanceNames();
            Array.Sort(names);
            foreach (var name in names)
            {
                yield return new PerformanceCounter(category, counter, name);
            }
        }
    }
}
