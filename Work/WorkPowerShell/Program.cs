using System.Management.Automation;

using var powerShell = PowerShell.Create();
powerShell.AddScript("Get-VM");
var results = powerShell.Invoke();

foreach (var result in results)
{
    Console.WriteLine("----");
    foreach (var pi in result.Properties)
    {
        Console.WriteLine($"{pi.Name} : {pi.Value}");
        //if (pi.Name == "HardDrives")
        //{
        //    Console.ReadLine();
        //}
        //if (pi.Name == "NetworkAdapters")
        //{
        //    Console.ReadLine();
        //}
    }
}

var results2 = powerShell.Invoke();

Console.ReadLine();
