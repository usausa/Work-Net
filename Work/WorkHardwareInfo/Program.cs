using Hardware.Info;

using System.Net.NetworkInformation;

// TODO Not work?

var hardwareInfo = new HardwareInfo();

Console.WriteLine("----");
Console.WriteLine(hardwareInfo.OperatingSystem);

Console.WriteLine("----");
Console.WriteLine(hardwareInfo.MemoryStatus);

Console.WriteLine("----");
foreach (var hardware in hardwareInfo.BatteryList)
    Console.WriteLine(hardware);

Console.WriteLine("----");
foreach (var hardware in hardwareInfo.BiosList)
    Console.WriteLine(hardware);

Console.WriteLine("----");
foreach (var hardware in hardwareInfo.ComputerSystemList)
    Console.WriteLine(hardware);

Console.WriteLine("----");
foreach (var cpu in hardwareInfo.CpuList)
{
    Console.WriteLine(cpu);
    foreach (var cpuCore in cpu.CpuCoreList)
        Console.WriteLine(cpuCore);
}

Console.WriteLine("----");
foreach (var drive in hardwareInfo.DriveList)
{
    Console.WriteLine(drive);
    foreach (var partition in drive.PartitionList)
    {
        Console.WriteLine(partition);
        foreach (var volume in partition.VolumeList)
            Console.WriteLine(volume);
    }
}

Console.WriteLine("----Keyboard");
foreach (var hardware in hardwareInfo.KeyboardList)
    Console.WriteLine(hardware);

Console.WriteLine("----Memory");
foreach (var hardware in hardwareInfo.MemoryList)
    Console.WriteLine(hardware);

Console.WriteLine("----Monitor");
foreach (var hardware in hardwareInfo.MonitorList)
    Console.WriteLine(hardware);

Console.WriteLine("----Motherboard");
foreach (var hardware in hardwareInfo.MotherboardList)
    Console.WriteLine(hardware);

Console.WriteLine("----Mouse");
foreach (var hardware in hardwareInfo.MouseList)
    Console.WriteLine(hardware);

Console.WriteLine("----NetworkAdapter");
foreach (var hardware in hardwareInfo.NetworkAdapterList)
    Console.WriteLine(hardware);

Console.WriteLine("----Printer");
foreach (var hardware in hardwareInfo.PrinterList)
    Console.WriteLine(hardware);

Console.WriteLine("----SoundDevice");
foreach (var hardware in hardwareInfo.SoundDeviceList)
    Console.WriteLine(hardware);

Console.WriteLine("----VideoController");
foreach (var hardware in hardwareInfo.VideoControllerList)
    Console.WriteLine(hardware);

Console.WriteLine("----Network");
foreach (var address in HardwareInfo.GetLocalIPv4Addresses(NetworkInterfaceType.Ethernet, OperationalStatus.Up))
    Console.WriteLine(address);
Console.WriteLine("----");
foreach (var address in HardwareInfo.GetLocalIPv4Addresses(NetworkInterfaceType.Wireless80211))
    Console.WriteLine(address);
Console.WriteLine("----");
foreach (var address in HardwareInfo.GetLocalIPv4Addresses(OperationalStatus.Up))
    Console.WriteLine(address);
Console.WriteLine("----");
foreach (var address in HardwareInfo.GetLocalIPv4Addresses())
    Console.WriteLine(address);

Console.ReadLine();
