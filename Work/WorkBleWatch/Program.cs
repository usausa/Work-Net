using System.Globalization;

using HashtagChris.DotNetBlueZ;
using HashtagChris.DotNetBlueZ.Extensions;

while (true)
{
    using var adapter = await BlueZManager.GetAdapterAsync(adapterName: "hci0");

    // TODO per device & task?
    // TODO retry
    IReadOnlyList<Device> devices = await adapter.GetDevicesAsync();
    foreach (var device in devices)
    {
        var prop = await device.GetAllAsync();
        Console.WriteLine($"{prop.Address} {prop.Name} {prop.RSSI}");
        if (String.IsNullOrEmpty(prop.Name) || !prop.Name.StartsWith("BTWATTCH2", true, CultureInfo.InvariantCulture))
        {
            continue;
        }

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

        try
        {
            if (!prop.Connected)
            {
                await device.ConnectAsync();
            }

            var service = await device.GetServiceAsync("6e400001-b5a3-f393-e0a9-e50e24dcca9e");
            var tx = await service.GetCharacteristicAsync("6e400002-b5a3-f393-e0a9-e50e24dcca9e");
            var rx = await service.GetCharacteristicAsync("6e400003-b5a3-f393-e0a9-e50e24dcca9e");
            Console.WriteLine("ok get");

            rx.Value += (sender, eventArgs) =>
            {
                // TODO parse
                Console.WriteLine("notify " + Convert.ToHexString(eventArgs.Value));
                return Task.CompletedTask;
            };

            await rx.StartNotifyAsync();

            // TODO RTC

            await tx.WriteValueAsync(Convert.FromHexString("aa0002a70159"), new Dictionary<string, object>());
            Console.WriteLine("ok write on");

            while (true)
            {
                await tx.WriteValueAsync(Convert.FromHexString("aa000108b3"), new Dictionary<string, object>());
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
            await device.DisconnectAsync();
        }
    }

    Thread.Sleep(1000);
}
