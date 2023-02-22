namespace WorkQuicBenchmarkClient;

using System.Diagnostics;
using System.Net.Quic;
using System.Net.Security;
using System.Net;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class Program
{
    private static async Task Main(string[] args)
    {
        var watch = Stopwatch.StartNew();

        var tasks = new List<Task>();
        for (var i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(RunQuicClientAsync));
        }

        await Task.WhenAll(tasks);

        async Task RunQuicClientAsync()
        {
            try
            {
                await using var connection = await QuicConnection.ConnectAsync(new QuicClientConnectionOptions
                {
                    DefaultCloseErrorCode = 0,
                    DefaultStreamErrorCode = 0,
                    RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 5001),
                    ClientAuthenticationOptions = new SslClientAuthenticationOptions
                    {
                        ApplicationProtocols = new List<SslApplicationProtocol> { new("h3") },
                        RemoteCertificateValidationCallback = (_, _, _, _) => true
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        Console.WriteLine($"Elapsed={watch.ElapsedMilliseconds}");
    }
}
