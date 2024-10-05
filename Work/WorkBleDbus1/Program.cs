using HashtagChris.DotNetBlueZ;
using HashtagChris.DotNetBlueZ.Extensions;

using Tmds.DBus;

using var adapter = await BlueZManager.GetAdapterAsync(adapterName: "hci0");

await adapter.WatchPropertiesAsync(Handler);
void Handler(PropertyChanges obj)
{
    Console.WriteLine("Property changed");
    foreach (var (key, value) in obj.Changed)
    {
        Console.WriteLine($"  {key}: {value}");
    }
}

adapter.DeviceFound += AdapterOnDeviceFound;
async Task AdapterOnDeviceFound(Adapter sender, DeviceFoundEventArgs args)
{
    var device = args.Device;
    var props = await device.GetAllAsync();
    Console.WriteLine($"DeviceFound {props.Address}");
    if (props.ManufacturerData is not null)
    {
        Dump(props);
    }
}

await adapter.StartDiscoveryAsync();

while (true)
{
    IReadOnlyList<Device> devices = await adapter.GetDevicesAsync();
    foreach (var device in devices)
    {
        var props = await device.GetAllAsync();
        //Console.WriteLine(props.Address);

        if (props.ManufacturerData is not null)
        {
            Dump(props);
        }
    }

    Thread.Sleep(5000);
}

void Dump(Device1Properties props)
{
    foreach (var (key, value) in props.ManufacturerData)
    {
        var buffer = (byte[])value;
        //Console.WriteLine($"  {key:X4}: {Convert.ToHexString(buffer)}");
        if (key == 0x0969)
        {
            if (buffer.Length >= 11)
            {
                var temperature = ((float)(buffer[8] & 0x0f) / 10 + (buffer[9] & 0x7f)) * ((buffer[9] & 0x80) > 0 ? 1 : -1);
                var humidity = buffer[10] & 0x7f;
                Console.WriteLine($"{props.Address} {props.RSSI} {Convert.ToHexString(buffer.AsSpan(0, 6))} Temp={temperature:F1}C, Hum={humidity}%");
            }
        }
    }
}
