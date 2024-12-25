using System.Diagnostics;
using System.Management;

using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SerialPort");
foreach (var obj in searcher.Get())
{
    foreach (var property in obj.Properties)
    {
        Debug.WriteLine($"{property.Name} : {property.Value}");
    }

    Debug.WriteLine("====================================");
}
