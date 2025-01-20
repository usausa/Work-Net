namespace Develop;

using System.Net;
using System.Net.Sockets;
using System.Text;

internal static class Program
{
    public static void Main()
    {
        using var udp = new UdpClient();
        var ep = new IPEndPoint(IPAddress.Loopback, 12345);
        var message = Encoding.UTF8.GetBytes("Hello, World!");
        udp.Send(message, message.Length, ep);
    }
}
