using HidSharp;

var devices = DeviceList.Local.GetHidDevices().ToList();
foreach (var device in devices)
{
    Console.WriteLine($"VID:{device.VendorID:X4} PID:{device.ProductID:X4} : {device.GetProductName()}");
    Console.WriteLine($"  DevicePath: {device.DevicePath}");
    Console.WriteLine($"  MaxInputReportLength: {device.GetMaxInputReportLength()}");
    Console.WriteLine($"  MaxOutputReportLength: {device.GetMaxOutputReportLength()}");
    Console.WriteLine($"  MaxFeatureReportLength: {device.GetMaxFeatureReportLength()}");

    try
    {
        var desc = device.GetReportDescriptor();

        Console.WriteLine("  Reports");
        foreach (var report in desc.Reports)
        {
            Console.WriteLine($"    {report.ReportID} {report.ReportType}");
        }

        foreach (var item in desc.DeviceItems)
        {
            Console.WriteLine($"  CollectionType: {item.CollectionType}");

            Console.WriteLine("  Usage");
            foreach (var usage in item.Usages.GetAllValues())
            {
                // Generic Desktop (0x01) の Joystick(0x04) / GamePad(0x05)
                var page = (ushort)(usage >> 16);
                var id = (ushort)(usage & 0xFFFF);
                Console.WriteLine($"    {page:X4}:{id:X4}");
            }
        }

    }
    catch
    {
        Console.WriteLine("  Failed get detail.");
    }
}

Console.ReadLine();

// Usage Page 0x01: Generic Desktop
//   0x02 = Mouse
//   0x04 = Joystick
//   0x06 = Keyboard
//   0x30 = X軸
//   0x31 = Y軸
//   0x38 = Wheel

// Usage Page 0x09: Button
//   0x01 = Button 1
//   0x02 = Button 2
//    ...

// Usage Page 0x08: LED
//   0x01 = Num Lock
//   0x02 = Caps Lock
//   0x03 = Scroll Lock
