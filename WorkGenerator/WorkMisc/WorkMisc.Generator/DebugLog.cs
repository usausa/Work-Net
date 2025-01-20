namespace WorkMisc.Generator;

using System.Net.Sockets;
using System.Net;
using System.Text;

public static class DebugLog
{
    public static void Log(string message)
    {
        using var udp = new UdpClient();
        var ep = new IPEndPoint(IPAddress.Loopback, 12345);
        var bytes = Encoding.UTF8.GetBytes(message);
        udp.Send(bytes, bytes.Length, ep);
    }
}
