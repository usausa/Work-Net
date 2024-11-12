using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

var ping = new Ping();
for (var i = 0; i < 10; i++)
{
    var result = await ping.SendPingAsync(IPAddress.Parse("192.168.100.5"), 5);
    Debug.WriteLine($"{result.Status} : {result.RoundtripTime}");
    await Task.Delay(1000);
}
