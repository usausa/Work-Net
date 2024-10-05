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
            //if (String.IsNullOrEmpty(@event.Name))
            //{
            //    return;
            //}

            //Debug.WriteLine($"{@event.Uuids.Length} {@event.Name} {@event.Rssi} {@event.TxPower}");

            if (@event.ManufacturerData.TryGetValue(0x0969, out var buffer))
            {
                if (buffer.Length >= 11)
                {
                    var temperature = ((float)(buffer[8] & 0x0f) / 10 + (buffer[9] & 0x7f)) * ((buffer[9] & 0x80) > 0 ? 1 : -1);
                    var humidity = buffer[10] & 0x7f;
                    Debug.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {@event.Rssi} {Convert.ToHexString(buffer.AsSpan(0, 6))} Temp={temperature:F1}C, Hum={humidity}%");
                }
            }
        };

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
