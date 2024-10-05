using HashtagChris.DotNetBlueZ;
using HashtagChris.DotNetBlueZ.Extensions;

using var adapter = await BlueZManager.GetAdapterAsync(adapterName: "hci0");

await adapter.StartDiscoveryAsync();

while (true)
{

    var device = await adapter.GetDeviceAsync("CF:34:30:37:44:1E");
    if (device is null)
    {
        Console.WriteLine("*");
        Thread.Sleep(1000);
        continue;
    }

    var prop = await device.GetAllAsync();
    Console.WriteLine($"Address: {prop.Address}");
    Console.WriteLine($"AddressType: {prop.AddressType}");
    Console.WriteLine($"Name: {prop.Name}");
    Console.WriteLine($"Alias: {prop.Alias}");
    Console.WriteLine($"Class: {prop.Class}");
    Console.WriteLine($"Appearance: {prop.Appearance}");
    Console.WriteLine($"Icon: {prop.Icon}");
    Console.WriteLine($"Paired: {prop.Paired}");
    Console.WriteLine($"Trusted: {prop.Trusted}");
    Console.WriteLine($"Blocked: {prop.Blocked}");
    Console.WriteLine($"LegacyPairing: {prop.LegacyPairing}");
    Console.WriteLine($"Connected: {prop.Connected}");
    Console.WriteLine($"RSSI: {prop.RSSI}");
    Console.WriteLine($"TxPower: {prop.TxPower}");

    var connected = false;
    try
    {
        if (!prop.Connected)
        {
            await device.ConnectAsync();
        }

        connected = true;

        Console.WriteLine("--");
        var services = await device.GetServicesAsync();
        foreach (var s in services)
        {
            Console.WriteLine($"{(await s.GetAllAsync()).UUID}");
            var chars = await s.GetCharacteristicsAsync();
            foreach (var c in chars)
            {
                Console.WriteLine($"  {(await c.GetAllAsync()).UUID}");
            }
        }
        Console.WriteLine("--");

        var service = await device.GetServiceAsync("cba20d00-224d-11e6-9fb8-0002a5d5c51b");
        var tx = await service.GetCharacteristicAsync("cba20002-224d-11e6-9fb8-0002a5d5c51b");
        var rx = await service.GetCharacteristicAsync("cba20003-224d-11e6-9fb8-0002a5d5c51b");
        if ((tx is null) || (rx is null))
        {
            Console.WriteLine("ng get");
            continue;
        }

        // TODO not fired ?
        rx.Value += (sender, eventArgs) =>
        {
            // TODO parse
            Console.WriteLine("notify " + Convert.ToHexString(eventArgs.Value));
            return Task.CompletedTask;
        };

        await rx.StartNotifyAsync();

        while (true)
        {
            await tx.WriteValueAsync(Convert.FromHexString("570f31"), new Dictionary<string, object>());
            Console.WriteLine("ok write measure");

            await Task.Delay(5000);
        }

        //await rx.StopNotifyAsync();
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
    finally
    {
        if (connected)
        {
            await device.DisconnectAsync();
        }
    }

    Thread.Sleep(1000);
}
