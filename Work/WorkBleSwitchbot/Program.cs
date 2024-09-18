using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Devices.Bluetooth.Advertisement;

var watcher = new BluetoothLEAdvertisementWatcher
{
    ScanningMode = BluetoothLEScanningMode.Passive
};

// https://github.com/OpenWonderLabs/SwitchBotAPI-BLE/blob/latest/devicetypes/meter.md#outdoor-temperaturehumidity-sensor
watcher.Received += (_, eventArgs) =>
{
    foreach (var data in eventArgs.Advertisement.ManufacturerData.Where(static x => x.CompanyId == 0x0969))
    {
        var buffer = data.Data.ToArray();
        if (buffer.Length >= 11)
        {
            var temperature = ((float)(buffer[8] & 0x0f) / 10 + (buffer[9] & 0x7f)) * ((buffer[9] & 0x80) > 0 ? 1 : -1);
            var humidity = buffer[10] & 0x7f;
            Debug.WriteLine($"{eventArgs.Timestamp:HH:mm:ss.fff} {eventArgs.RawSignalStrengthInDBm} {Convert.ToHexString(buffer.AsSpan(0, 6))} Temp={temperature:F1}C, Hum={humidity}%");
        }
    }
};

watcher.Start();

Console.ReadLine();
