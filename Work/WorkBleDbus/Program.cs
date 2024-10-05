using HashtagChris.DotNetBlueZ;
using HashtagChris.DotNetBlueZ.Extensions;

while (true)
{
    var adapter = await BlueZManager.GetAdapterAsync(adapterName: "hci0");

    // TODO retry
    IReadOnlyList<Device> devices = await adapter.GetDevicesAsync();
    foreach (var device in devices)
    {
        var prop = await device.GetAllAsync();
        Console.WriteLine($"{prop.Address} {prop.Name} {prop.RSSI}");
        if (!prop.Name.StartsWith("BTWATTCH2"))
        {
            continue;
        }

        try
        {
            await device.ConnectAsync();

            //var paired = await device.GetPairedAsync();
            //var rssi = await device.GetRSSIAsync();
            //Console.WriteLine($"{paired} {rssi}");

            var service = await device.GetServiceAsync("6e400001-b5a3-f393-e0a9-e50e24dcca9e");
            var tx = await service.GetCharacteristicAsync("6e400002-b5a3-f393-e0a9-e50e24dcca9e");
            var rx = await service.GetCharacteristicAsync("6e400003-b5a3-f393-e0a9-e50e24dcca9e");
            Console.WriteLine("ok get");

            rx.Value += (sender, eventArgs) =>
            {
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

                Thread.Sleep(5000);
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
