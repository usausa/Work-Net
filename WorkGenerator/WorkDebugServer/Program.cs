using System.Net;
using System.Net.Sockets;
using System.Text;

var client = new UdpClient(12345);

while (true)
{
    var endPoint = new IPEndPoint(IPAddress.Any, 0);
    var data = client.Receive(ref endPoint);
    Console.WriteLine(Encoding.UTF8.GetString(data));
}
