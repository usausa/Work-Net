using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

var meshService = new Guid("72c90001-57a9-4d40-b746-534e22ec9f9e");
var writeCharacteristic = new Guid("72c90002-57a9-4d40-b746-534e22ec9f9e");

var tcs = new TaskCompletionSource<BluetoothLEDevice?>();

// Watcher
var watcher = new BluetoothLEAdvertisementWatcher
{
    ScanningMode = BluetoothLEScanningMode.Active
};

watcher.Received += ReceivedHandler;

watcher.Start();

async void ReceivedHandler(BluetoothLEAdvertisementWatcher source, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
{
    var device = await BluetoothLEDevice.FromBluetoothAddressAsync(eventArgs.BluetoothAddress);
    if ((device is not null) && (device.Name.Contains("MESH-100LE")))
    {
        tcs.TrySetResult(device);
    }
}

// Discover
var device = await tcs.Task;

watcher.Stop();
watcher.Received -= ReceivedHandler;

if (device is null)
{
    return;
}

var gattResult = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
if (gattResult.Status != GattCommunicationStatus.Success)
{
    return;
}

var service = gattResult.Services.FirstOrDefault(x => x.Uuid == meshService);
if (service is null)
{
    return;
}

var accessResult = await service.RequestAccessAsync();
if (accessResult != DeviceAccessStatus.Allowed)
{
    return;
}

var characteristicResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
if (characteristicResult.Status!= GattCommunicationStatus.Success)
{
    return;
}

var characteristic = characteristicResult.Characteristics.FirstOrDefault(x => x.Uuid == writeCharacteristic);
if (characteristic is null)
{
    return;
}

var writer = new DataWriter();
writer.WriteBytes(new byte[]
{
    0x00, // ID
    0x02, //
    0x01, // Enable
    0x03 // チェックサム
});
var buffer1 = writer.DetachBuffer();

var writeResult = await characteristic.WriteValueWithResultAsync(buffer1);
Debug.WriteLine(writeResult);

writer.WriteBytes(new byte[]
{
    0x01, // ID
    0x00, // LED店頭
    0x0F, // R
    0x00,
    0x3F, // G
    0x00,
    0x00, // B
    0xFF, 0xFF, // 点灯時間
    0x88, 0x13, // 点灯サイクル(5000ms)
    0x64, 0x00, // 消灯サイクル(100ms)
    0x01, // 点灯パターン
    0x4D // チェックサム
});
buffer1 = writer.DetachBuffer();

writeResult = await characteristic.WriteValueWithResultAsync(buffer1);
Debug.WriteLine(writeResult);

Console.ReadLine();
