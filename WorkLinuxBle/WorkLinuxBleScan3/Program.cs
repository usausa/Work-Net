namespace WorkLinuxBleScan3;

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

internal sealed class DeviceInfo
{
    public string DevicePath { get; set; } = "";
    public string? Address { get; set; }
    public string? Name { get; set; }
    public string? Alias { get; set; }
    public short? Rssi { get; set; }
    public DateTimeOffset LastEventTime { get; set; }
    public Dictionary<ushort, byte[]> ManufacturerData { get; } = new();
}

internal static class Program
{
    static async Task<int> Main()
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            // ReSharper disable once AccessToDisposedClosure
            cts.Cancel();
        };

        await using var session = await BleScanSession.CreateAsync();
        var devices = new ConcurrentDictionary<string, DeviceInfo>(StringComparer.Ordinal);
        var gate = new object();
        var needsRedraw = false;

        session.DeviceEvent += ev =>
        {
            lock (gate)
            {
                if (ev.Type == BleScanEventType.Lost)
                {
                    devices.TryRemove(ev.DevicePath, out _);
                    needsRedraw = true;
                }
                else
                {
                    var device = devices.GetOrAdd(ev.DevicePath, _ => new DeviceInfo { DevicePath = ev.DevicePath });

                    if (ev.Address is not null) device.Address = ev.Address;
                    if (ev.Name is not null) device.Name = ev.Name;
                    if (ev.Alias is not null) device.Alias = ev.Alias;
                    if (ev.Rssi is not null) device.Rssi = ev.Rssi;
                    device.LastEventTime = ev.Timestamp;

                    device.ManufacturerData.Clear();
                    if (ev.ManufacturerData is not null)
                    {
                        foreach (var md in ev.ManufacturerData)
                        {
                            device.ManufacturerData[md.Key] = md.Value;
                        }
                    }

                    needsRedraw = true;
                }
            }
        };

        await session.StartAsync(cts.Token);

        // 描画ループ
        // ReSharper disable once MethodSupportsCancellation
        var drawTask = Task.Run(async () =>
        {
            // ReSharper disable once AccessToDisposedClosure
            while (!cts.Token.IsCancellationRequested)
            {
                lock (gate)
                {
                    if (needsRedraw)
                    {
                        DrawDashboard(devices);
                        needsRedraw = false;
                    }
                }

                try
                {
                    // ReSharper disable once AccessToDisposedClosure
                    await Task.Delay(100, cts.Token);
                }
                catch
                {
                    // Ignore
                }
            }
        });

        Console.WriteLine("Scanning... Press Enter to stop. (Ctrl+C also works)");
        // ReSharper disable once MethodSupportsCancellation
        // ReSharper disable once ConvertClosureToMethodGroup
        var inputTask = Task.Run(() => Console.ReadLine());
        // ReSharper disable once MethodSupportsCancellation
        await Task.WhenAny(inputTask, Task.Delay(Timeout.Infinite, cts.Token)).ContinueWith(_ => { });

        await cts.CancelAsync();
        await session.StopAsync();

        try
        {
            await drawTask;
        }
        catch
        {
            // Ignore
        }

        return 0;
    }

    private static void DrawDashboard(ConcurrentDictionary<string, DeviceInfo> devices)
    {
        Console.Clear();
        Console.SetCursorPosition(0, 0);

        var now = DateTimeOffset.Now;
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                                              BLE Device Dashboard                                                                     ║");
        Console.WriteLine($"║  Last Update: {FmtTs(now),-20}                                               Device Count: {devices.Count,-3}                                    ║");
        Console.WriteLine("╠═══════════════════╦══════╦══════════════╦══════════════════════════╦══════════════════════════════════════════════════════════════════╣");
        Console.WriteLine("║ Address           ║ RSSI ║ Last Event   ║ Name / Alias             ║ ManufacturerData                                                 ║");
        Console.WriteLine("╠═══════════════════╬══════╬══════════════╬══════════════════════════╬══════════════════════════════════════════════════════════════════╣");

        var sortedDevices = devices.Values
            .OrderByDescending(d => d.LastEventTime)
            .Take(20) // 最大20デバイスまで表示
            .ToList();

        foreach (var device in sortedDevices)
        {
            var address = device.Address ?? "Unknown";
            var rssi = device.Rssi?.ToString() ?? "?";
            var lastEvent = FmtTs(device.LastEventTime);
            var name = device.Name ?? device.Alias ?? "Unknown";
            if (name.Length > 29) name = name.Substring(0, 26) + "...";

            string mdInfo;
            if (device.ManufacturerData.Count > 0)
            {
                var firstMd = device.ManufacturerData.First();
                var hexData = ToHexDump(firstMd.Value);
                if (hexData.Length > 57) hexData = hexData.Substring(0, 54) + "...";
                mdInfo = $"0x{firstMd.Key:X4}: {hexData}";
            }
            else
            {
                mdInfo = "-";
            }

            Console.WriteLine($"║ {address,-17} ║ {rssi,4} ║ {lastEvent,10} ║ {name,-24} ║ {mdInfo,-64} ║");
        }

        // 空行で埋める
        for (int i = sortedDevices.Count; i < 20; i++)
        {
            Console.WriteLine("║                   ║      ║              ║                          ║                                                                  ║");
        }

        Console.WriteLine("╚═══════════════════╩══════╩══════════════╩══════════════════════════╩══════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("Press Enter or Ctrl+C to stop...");
    }

    private static string FmtTs(DateTimeOffset ts) =>
        ts.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

    private static string ToHexDump(byte[] data, int bytesPerLine = 16)
    {
        const string hexChars = "0123456789ABCDEF";
        var sb = new StringBuilder();
        for (int i = 0; i < data.Length; i += bytesPerLine)
        {
            int n = Math.Min(bytesPerLine, data.Length - i);
            for (int j = 0; j < n; j++)
            {
                byte b = data[i + j];
                sb.Append(hexChars[b >> 4]);
                sb.Append(hexChars[b & 0xF]);
                if (j + 1 != n) sb.Append(' ');
            }
            if (i + n < data.Length) sb.Append(' ');
        }
        return sb.ToString();
    }
}
