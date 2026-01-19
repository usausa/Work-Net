using HidLibrary;

var devices = HidDevices.Enumerate();

foreach (var device in devices)
{
    device.OpenDevice();

    Console.WriteLine("==================");
    Console.WriteLine($"製品名: {device.Description}");
    Console.WriteLine($"VendorID: 0x{device.Attributes.VendorId:X4}");
    Console.WriteLine($"ProductID: 0x{device.Attributes.ProductId:X4}");
    Console.WriteLine($"Version: {device.Attributes.Version}");
    Console.WriteLine($"デバイスパス: {device.DevicePath}");
    Console.WriteLine($"最大入力レポート長: {device.Capabilities.InputReportByteLength}");
    Console.WriteLine($"最大出力レポート長: {device.Capabilities.OutputReportByteLength}");

    device.CloseDevice();
}

Console.ReadLine();
