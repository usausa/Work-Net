using System.Diagnostics;
using ManagedNativeWifi;

while (true)
{
    var watch = Stopwatch.StartNew();
    foreach (var net in NativeWifi.EnumerateBssNetworks())
    {
        Console.WriteLine($"{net.Bssid} {net.Ssid} {net.BssType} {net.Band} {net.Channel} {net.SignalStrength}");
    }
    Console.WriteLine($"--{watch.ElapsedMilliseconds}");

    await Task.Delay(1000);
}
