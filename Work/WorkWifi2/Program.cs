using ManagedNativeWifi;

while (true)
{
    foreach (var net in NativeWifi.EnumerateBssNetworks())
    {
        Console.WriteLine($"{net.Ssid} {net.BssType} {net.Band} {net.Channel} {net.SignalStrength}");
    }

    await Task.Delay(1000);
}
