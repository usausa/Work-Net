using System.Linq;
using System;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Devices.Bluetooth.Advertisement;

var watcher = new BluetoothLEAdvertisementWatcher();
watcher.Received += OnWatcherReceived;
watcher.Start();

Console.ReadLine();

void OnWatcherReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
{
    foreach (var md in args.Advertisement.ManufacturerData.Where(static x => x.CompanyId == 0x0B60))
    {
        var buffer = md.Data.ToArray();

        if (buffer.Length >= 8)
        {
            var relay = buffer[0] != 0x00;
            var voltage = (double)((buffer[2] << 8) + buffer[1]) / 10;
            var current = (double)((buffer[4] << 8) + buffer[3]) / 1000;
            var power = (double)((buffer[7] << 16) + (buffer[6] << 8) + buffer[5]) / 1000;

            Console.WriteLine($"{DateTime.Now:HH:mm:ss} address={args.BluetoothAddress:X12}, rssi={args.RawSignalStrengthInDBm} : {Convert.ToHexString(buffer)} : relay={relay}, voltage={voltage}V, current={current}A, power={power}W");
        }
    }
}
