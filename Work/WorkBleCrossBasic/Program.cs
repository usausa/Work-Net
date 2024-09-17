namespace WorkBleCrossBasic;

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
        var scan = await Bluetooth.RequestLEScanAsync();

        Console.ReadLine();
    }
}
