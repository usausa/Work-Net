using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

var watcher = new BluetoothLEAdvertisementWatcher
{
    ScanningMode = BluetoothLEScanningMode.Passive
};

var set = new HashSet<ulong>();

watcher.Received += async(sender, eventArgs) =>
{
    if (!set.Add(eventArgs.BluetoothAddress))
    {
        return;
    }

    var device = await BluetoothLEDevice.FromBluetoothAddressAsync(eventArgs.BluetoothAddress);
    var gatt = device is not null ? await device?.GetGattServicesAsync() : null;

    Console.WriteLine($"{eventArgs.Timestamp:HH:mm:ss.fff} {eventArgs.BluetoothAddress:X12} {eventArgs.RawSignalStrengthInDBm} {device?.Name} {gatt?.Status} {gatt?.Services.Count}");
    Debug.WriteLine($"{eventArgs.Timestamp:HH:mm:ss.fff} {eventArgs.BluetoothAddress:X12} {eventArgs.RawSignalStrengthInDBm} {device?.Name} {gatt?.Status} {gatt?.Services.Count}");
    //if (gatt?.Services.Count > 0)
    //{
    //    for (var i = 0; i < gatt!.Services.Count; i++)
    //    {
    //        Console.WriteLine($"  {gatt.Services[i].Uuid}");
    //    }
    //}
};

watcher.Start();

Console.ReadLine();
