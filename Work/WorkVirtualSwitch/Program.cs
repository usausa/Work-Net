using System.Diagnostics;

while (true)
{
    var pcc = new PerformanceCounterCategory("Hyper-V Virtual Switch");
    foreach (var instance in pcc.GetInstanceNames())
    {
        using var counter1 = new PerformanceCounter("Hyper-V Virtual Switch", "Bytes Received/sec", instance);
        using var counter2 = new PerformanceCounter("Hyper-V Virtual Switch", "Bytes Sent/sec", instance);
        counter1.NextValue();
        counter2.NextValue();
        Debug.WriteLine($"{instance} R : {counter1.NextValue()}");
        Debug.WriteLine($"{instance} S : {counter2.NextValue()}");
    }

    Thread.Sleep(1000);
}
