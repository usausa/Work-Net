namespace WorkBleCrossBasic;

using System;
using System.Diagnostics;

using InTheHand.Bluetooth;

internal class Program
{
    public static async Task Main(string[] args)
    {
        Bluetooth.AdvertisementReceived += (_, @event) =>
        {
            if (String.IsNullOrEmpty(@event.Name))
            {
                return;
            }

            Debug.WriteLine($"{@event.Uuids.Length} {@event.Name} {@event.Rssi} {@event.TxPower}");
        };

        // NG on Linux
        try
        {
            var scan = await Bluetooth.RequestLEScanAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        Console.ReadLine();
    }
}
