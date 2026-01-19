using LibUsbDotNet;
using LibUsbDotNet.Descriptors;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;

// 全USBデバイスを列挙
var all = UsbDevice.AllDevices;

Console.WriteLine($"USB device count: {all.Count}");
Console.WriteLine();

foreach (UsbRegistry reg in all)
{
    // レジストリ情報（列挙時点で分かる範囲）
    var vid = reg.Vid;
    var pid = reg.Pid;

    Console.WriteLine($"VID:PID = {vid:X4}:{pid:X4}");
    Console.WriteLine($"DevicePath: {reg.DevicePath}");
    Console.WriteLine($"Name: {reg.Name}");

    // デバイスをオープンできたら、Descriptorや文字列を取得
    if (reg.Open(out var device))
    {
        try
        {
            // デバイスディスクリプタ
            var info = device.Info;
            var dd = info.Descriptor;

            Console.WriteLine($"USB Spec: {dd.BcdUsb:X4}");
            Console.WriteLine($"Class/Sub/Proto: {dd.Class}/{dd.SubClass:X2}/{dd.Protocol:X2}");
            Console.WriteLine($"MaxPacketSize0: {dd.MaxPacketSize0}");
            Console.WriteLine($"Configurations: {dd.ConfigurationCount}");

            // 文字列（取得できない場合は空や例外になることがあります）
            var manufacturer = SafeGet(() => info.ManufacturerString);
            var product = SafeGet(() => info.ProductString);
            var serial = SafeGet(() => info.SerialString);

            if (!string.IsNullOrWhiteSpace(manufacturer))
                Console.WriteLine($"Manufacturer: {manufacturer}");
            if (!string.IsNullOrWhiteSpace(product))
                Console.WriteLine($"Product: {product}");
            if (!string.IsNullOrWhiteSpace(serial))
                Console.WriteLine($"Serial: {serial}");

            // コンフィギュレーション、インターフェース、エンドポイント情報の詳細表示
            // UsbDeviceInfo.ToString()を使用すると、すべての詳細情報が表示される
            Console.WriteLine();
            Console.WriteLine("=== Detailed Device Information ===");
            Console.WriteLine(info.ToString());
        }
        finally
        {
            device.Close();
        }
    }
    else
    {
        Console.WriteLine("Open: failed (driver/permission or device in use)");
    }

    Console.WriteLine(new string('=', 80));
    Console.WriteLine();
}

// LibUsbDotNet の内部リソース解放（推奨）
UsbDevice.Exit();

Console.ReadKey();

static string SafeGet(Func<string> getter)
{
    try { return getter() ?? ""; }
    catch { return ""; }
}
