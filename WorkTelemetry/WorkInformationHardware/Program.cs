namespace WorkInformationHardware;

using System.Diagnostics;

using LibreHardwareMonitor.Hardware;

internal static class Program
{
    public static void Main()
    {
        var computer = new Computer
        {
            IsBatteryEnabled = true,
            IsControllerEnabled = true,
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsNetworkEnabled = true,
            IsPsuEnabled = true,
            IsStorageEnabled = true
        };
        computer.Open();

        computer.Accept(new UpdateVisitor());


        foreach (var hardware in computer.Hardware)
        {
            ShowHardware(hardware, 0);
            //Console.WriteLine($"{hardware.HardwareType}: {hardware.Name}");

            //foreach (var subHardware in hardware.SubHardware)
            //{
            //    Console.WriteLine($"\t{subHardware.HardwareType}: {subHardware.Name}");

            //    foreach (var sensor in subHardware.Sensors)
            //    {
            //        Console.WriteLine($"\t\t{sensor.SensorType}: {sensor.Name}, value: {sensor.Value}");
            //    }
            //}

            //foreach (var sensor in hardware.Sensors)
            //{
            //    Console.WriteLine($"\t{sensor.SensorType}: {sensor.Name}, value: {sensor.Value}");
            //}
        }

        computer.Close();

        // TODO timer ?
        // Hardware.HardwareType,sensor.SensorTypeでカウンター決定、sensor.Nameでタグやサブ、Valueが値
        // or Identifierで？ 複数GPU？
        // ソースを見ての判定が必要か...

        //// Update
        //foreach (var hardware in computer.Hardware)
        //{
        //    Update(hardware);
        //}

        //foreach (var hardware in computer.Hardware)
        //{
        //    Console.WriteLine($"{hardware.HardwareType}");
        //    foreach (var sensor in EnumerableSensor(hardware))
        //    {
        //        Console.WriteLine($"{sensor.Hardware.HardwareType} : {sensor.SensorType} : {sensor.Hardware.Name} : {sensor.Name} : {sensor.Identifier} : {sensor.Value}");
        //    }
    }

    private static void ShowHardware(IHardware hardware, int indent)
    {
        WriteIndent(indent);
        Console.WriteLine($"{hardware.HardwareType}: {hardware.Name} : {hardware.Identifier}");

        foreach (var subHardware in hardware.SubHardware)
        {
            ShowHardware(subHardware, indent + 1);
        }

        foreach (var sensor in hardware.Sensors)
        {
            WriteIndent(indent + 1);
            Console.WriteLine($"{sensor.SensorType}: {sensor.Name} : {sensor.Identifier} : {sensor.Value}");
        }
    }

    private static void WriteIndent(int indent)
    {
        for (var i = 0; i < indent; i++)
        {
            Console.Write("  ");
        }
    }


    //private static void Update(IHardware hardware)
    //{
    //    hardware.Update();
    //    foreach (var subHardware in hardware.SubHardware)
    //    {
    //        Update(subHardware);
    //    }
    //}

    //public static IEnumerable<ISensor> EnumerableSensor(IHardware hardware)
    //{
    //    foreach (var sensor in hardware.Sensors)
    //    {
    //        yield return sensor;
    //    }

    //    foreach (var subHardware in hardware.SubHardware)
    //    {
    //        foreach (var sensor in EnumerableSensor(subHardware))
    //        {
    //            yield return sensor;
    //        }
    //    }
    //}
}

public class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (var subHardware in hardware.SubHardware) subHardware.Accept(this);
    }

    public void VisitSensor(ISensor sensor) { }

    public void VisitParameter(IParameter parameter) { }
}

//private static string MakeFieldName(ISensor sensor)
//if (sensor.Hardware.HardwareType == HardwareType.Network)
//    return sensor.Identifier.ToString()![1..].Replace('/', '_')
//        .Replace("-", string.Empty, StringComparison.Ordinal)
//        .Replace("{", string.Empty, StringComparison.Ordinal)
//        .Replace("}", string.Empty, StringComparison.Ordinal);

//return sensor.Identifier.ToString()![1..].Replace('/', '_');


//switch (sensor.Name)
//private readonly ISensor physicalMemoryUsed;  "Memory Used"
//private readonly ISensor physicalMemoryAvailable; "Memory Available"
//private readonly ISensor virtualMemoryUsed; "Virtual Memory Used"


//internal sealed class SensorRepository
//private readonly Computer computer;
//public object Sync => computer;

//private readonly long expire;
//private long lastTick;

//public void Update()
//{
//    var timestamp = Stopwatch.GetTimestamp();
//    if ((timestamp - lastTick) < expire)
//    {
//        return;
//    }

//    foreach (var hardware in computer.Hardware)
//    {
//        Update(hardware);
//    }

//    lastTick = timestamp;
//}

