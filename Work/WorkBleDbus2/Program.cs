using System.Runtime.InteropServices.ComTypes;
using HashtagChris.DotNetBlueZ;
using HashtagChris.DotNetBlueZ.Extensions;

using var adapter = await BlueZManager.GetAdapterAsync(adapterName: "hci0");
adapter.DeviceFound += async (sender, args) =>
{
    var device = args.Device;
    var props = await device.GetAllAsync();

    Console.WriteLine($"Device found: {props.Address} {props.Name}");

    var watcher = await device.WatchPropertiesAsync(changes =>
    {
        foreach (var (key, value) in changes.Changed)
        {
            Console.WriteLine($"Changed: {props.Name} {key} {value}");
        }
    });
};

//using var _ = adapter.WatchDevicesAddedAsync(async device =>
//{
//    var props = await device.GetAllAsync();

//    Console.WriteLine($"Device added: {props.Address} {props.Name} {props.RSSI}");
//    await adapter.RemoveDeviceAsync(device.ObjectPath);
//});

var devices = await adapter.GetDevicesAsync();
//foreach (var device in devices)
//{
//    var props = await device.GetAllAsync();
//    Console.WriteLine($"Remove {props.Name}");
//    await adapter.RemoveDeviceAsync(device.ObjectPath);
//}

await adapter.StartDiscoveryAsync();

Console.ReadLine();
