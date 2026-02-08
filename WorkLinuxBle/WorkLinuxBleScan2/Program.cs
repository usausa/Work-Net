using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

#region ---- D-Bus interfaces ----
[DBusInterface("org.freedesktop.DBus.ObjectManager")]
public interface IObjectManager : IDBusObject
{
    Task<IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>>> GetManagedObjectsAsync();
    Task<IDisposable> WatchInterfacesAddedAsync(Action<(ObjectPath objectPath, IDictionary<string, IDictionary<string, object>> interfaces)> handler);
    Task<IDisposable> WatchInterfacesRemovedAsync(Action<(ObjectPath objectPath, string[] interfaces)> handler);
}
[DBusInterface("org.bluez.Adapter1")]
public interface IAdapter1 : IDBusObject
{
    Task StartDiscoveryAsync();
    Task StopDiscoveryAsync();
}
[DBusInterface("org.freedesktop.DBus.Properties")]
public interface IProperties : IDBusObject
{
    Task<IDisposable> WatchPropertiesChangedAsync(Action<(string iface, IDictionary<string, object> changed, string[] invalidated)> handler);
}
#endregion

#region ---- Utils ----
internal static class BleUtil
{
    public static string FmtTs(DateTimeOffset ts) =>
        ts.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
    public static short? TryGetInt16(IDictionary<string, object> props, string key)
    {
        if (!props.TryGetValue(key, out var v) || v is null) return null;
        return v switch
        {
            short s => s,
            int i => (short)i,
            long l => (short)l,
            _ => null
        };
    }
    public static bool? TryGetBool(IDictionary<string, object> props, string key)
        => props.TryGetValue(key, out var v) && v is bool b ? b : null;
    public static IReadOnlyDictionary<ushort, byte[]>? TryGetManufacturerData(IDictionary<string, object> props)
    {
        if (!props.TryGetValue("ManufacturerData", out var v) || v is null)
            return null;
        if (v is IDictionary<ushort, byte[]> direct)
            return new Dictionary<ushort, byte[]>(direct);
        if (v is IDictionary<ushort, object> dictObj)
        {
            var res = new Dictionary<ushort, byte[]>();
            foreach (var kv in dictObj)
            {
                if (kv.Value is byte[] bytes) res[kv.Key] = bytes;
                else if (kv.Value is IEnumerable<byte> eb) res[kv.Key] = eb.ToArray();
            }
            return res;
        }
        return null;
    }
    public static string ToHexDump(byte[] data, int bytesPerLine = 16)
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
#endregion

#region ---- Scan session with keep-alive ----
public enum BlueZDeviceEventType { Discover, Update, Lost }
public sealed class BlueZDeviceEvent
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
    public BlueZDeviceEventType Type { get; init; }
    public string Source { get; init; } = "";
    public string DevicePath { get; init; } = "";
    public IReadOnlyCollection<string> Keys { get; init; } = Array.Empty<string>();
    public string? Address { get; init; }
    public string? Name { get; init; }
    public string? Alias { get; init; }
    public short? Rssi { get; init; }
    public bool HasManufacturerDataUpdate { get; init; }
    public IReadOnlyDictionary<ushort, byte[]>? ManufacturerData { get; init; }
}
public sealed class BlueZScanSession : IAsyncDisposable
{
    private readonly Connection _conn;
    private readonly IObjectManager _objMgr;
    private readonly ObjectPath _adapterPath;
    private readonly IAdapter1 _adapter;
    private readonly IProperties _adapterProps;
    private IDisposable? _addedSub;
    private IDisposable? _removedSub;
    private IDisposable? _adapterPropsSub;
    private readonly ConcurrentDictionary<ObjectPath, IDisposable> _devicePropsSubs = new();
    private CancellationTokenSource? _keepAliveCts;
    private Task? _keepAliveTask;
    private volatile bool _discovering;
    public event Action<string>? Debug;
    public event Action<BlueZDeviceEvent>? DeviceEvent;
    private BlueZScanSession(Connection conn, IObjectManager objMgr, ObjectPath adapterPath)
    {
        _conn = conn;
        _objMgr = objMgr;
        _adapterPath = adapterPath;
        _adapter = conn.CreateProxy<IAdapter1>("org.bluez", _adapterPath);
        _adapterProps = conn.CreateProxy<IProperties>("org.bluez", _adapterPath);
    }
    public static async Task<BlueZScanSession> CreateAsync()
    {
        var conn = new Connection(Address.System);
        await conn.ConnectAsync();
        var objMgr = conn.CreateProxy<IObjectManager>("org.bluez", new ObjectPath("/"));
        var objects = await objMgr.GetManagedObjectsAsync();
        var adapterPath = objects.Keys.FirstOrDefault(p => objects[p].ContainsKey("org.bluez.Adapter1"));
        if (adapterPath == default)
            throw new InvalidOperationException("Bluetooth adapter (org.bluez.Adapter1) not found.");
        return new BlueZScanSession(conn, objMgr, adapterPath);
    }
    public async Task StartAsync(CancellationToken ct)
    {
        Debug?.Invoke($"[DBG] Using adapter: {_adapterPath}");
        _adapterPropsSub = await _adapterProps.WatchPropertiesChangedAsync(ev =>
        {
            if (!string.Equals(ev.iface, "org.bluez.Adapter1", StringComparison.Ordinal))
                return;
            if (ev.changed.TryGetValue("Discovering", out var v) && v is bool b)
            {
                _discovering = b;
                Debug?.Invoke($"[DBG] Adapter Discovering changed: {_discovering}");
            }
        });
        _addedSub = await _objMgr.WatchInterfacesAddedAsync(ev =>
        {
            if (!ev.interfaces.TryGetValue("org.bluez.Device1", out var props))
                return;
            Emit(new BlueZDeviceEvent
            {
                Timestamp = DateTimeOffset.Now,
                Type = BlueZDeviceEventType.Discover,
                Source = "InterfacesAdded",
                DevicePath = ev.objectPath.ToString(),
                Keys = props.Keys.ToArray(),
                Address = props.TryGetValue("Address", out var a) ? a as string : null,
                Name = props.TryGetValue("Name", out var n) ? n as string : null,
                Alias = props.TryGetValue("Alias", out var al) ? al as string : null,
                Rssi = BleUtil.TryGetInt16(props, "RSSI")
            });
            _ = EnsureDevicePropsSubscriptionAsync(ev.objectPath);
        });
        _removedSub = await _objMgr.WatchInterfacesRemovedAsync(ev =>
        {
            if (_devicePropsSubs.TryRemove(ev.objectPath, out var sub))
                sub.Dispose();
            Emit(new BlueZDeviceEvent
            {
                Timestamp = DateTimeOffset.Now,
                Type = BlueZDeviceEventType.Lost,
                Source = "InterfacesRemoved",
                DevicePath = ev.objectPath.ToString(),
                Keys = ev.interfaces.ToArray()
            });
        });
        var objects = await _objMgr.GetManagedObjectsAsync();
        foreach (var kv in objects)
        {
            if (!kv.Value.TryGetValue("org.bluez.Device1", out var props))
                continue;
            Emit(new BlueZDeviceEvent
            {
                Timestamp = DateTimeOffset.Now,
                Type = BlueZDeviceEventType.Discover,
                Source = "InitialDump",
                DevicePath = kv.Key.ToString(),
                Keys = props.Keys.ToArray(),
                Address = props.TryGetValue("Address", out var a) ? a as string : null,
                Name = props.TryGetValue("Name", out var n) ? n as string : null,
                Alias = props.TryGetValue("Alias", out var al) ? al as string : null,
                Rssi = BleUtil.TryGetInt16(props, "RSSI")
            });
            _ = EnsureDevicePropsSubscriptionAsync(kv.Key);
        }
        await StartDiscoverySafeAsync();
        _keepAliveCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _keepAliveTask = Task.Run(() => KeepAliveLoopAsync(_keepAliveCts.Token));
    }
    private async Task KeepAliveLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!_discovering)
                {
                    Debug?.Invoke("[DBG] KeepAlive: discovering=false -> StartDiscovery");
                    await StartDiscoverySafeAsync();
                }
            }
            catch (Exception ex)
            {
                Debug?.Invoke("[DBG] KeepAlive exception: " + ex.Message);
            }
            try { await Task.Delay(1000, ct); } catch { }
        }
    }
    private async Task StartDiscoverySafeAsync()
    {
        try
        {
            Debug?.Invoke("[DBG] Calling StartDiscovery...");
            await _adapter.StartDiscoveryAsync();
            Debug?.Invoke("[DBG] StartDiscovery OK");
            _discovering = true;
        }
        catch (DBusException ex) when (ex.ErrorName?.Contains("InProgress", StringComparison.OrdinalIgnoreCase) == true)
        {
            Debug?.Invoke("[DBG] StartDiscovery: already in progress");
            _discovering = true;
        }
    }
    private async Task EnsureDevicePropsSubscriptionAsync(ObjectPath devicePath)
    {
        if (_devicePropsSubs.ContainsKey(devicePath))
            return;
        var propsProxy = _conn.CreateProxy<IProperties>("org.bluez", devicePath);
        var sub = await propsProxy.WatchPropertiesChangedAsync(ev =>
        {
            if (!string.Equals(ev.iface, "org.bluez.Device1", StringComparison.Ordinal))
                return;
            if (ev.changed.Count == 0)
                return;
            var hasMd = ev.changed.ContainsKey("ManufacturerData");
            var md = hasMd ? BleUtil.TryGetManufacturerData(ev.changed) : null;
            Emit(new BlueZDeviceEvent
            {
                Timestamp = DateTimeOffset.Now,
                Type = BlueZDeviceEventType.Update,
                Source = "PropertiesChanged",
                DevicePath = devicePath.ToString(),
                Keys = ev.changed.Keys.ToArray(),
                Address = ev.changed.TryGetValue("Address", out var a) ? a as string : null,
                Name = ev.changed.TryGetValue("Name", out var n) ? n as string : null,
                Alias = ev.changed.TryGetValue("Alias", out var al) ? al as string : null,
                Rssi = BleUtil.TryGetInt16(ev.changed, "RSSI"),
                HasManufacturerDataUpdate = hasMd,
                ManufacturerData = md
            });
        });
        if (!_devicePropsSubs.TryAdd(devicePath, sub))
            sub.Dispose();
    }
    public async Task StopAsync()
    {
        _keepAliveCts?.Cancel();
        if (_keepAliveTask is not null)
        {
            try { await _keepAliveTask; } catch { }
        }
        _keepAliveTask = null;
        _keepAliveCts?.Dispose();
        _keepAliveCts = null;
        try
        {
            Debug?.Invoke("[DBG] Calling StopDiscovery...");
            await _adapter.StopDiscoveryAsync();
            Debug?.Invoke("[DBG] StopDiscovery OK");
        }
        catch (Exception ex)
        {
            Debug?.Invoke("[DBG] StopDiscovery error: " + ex.Message);
        }
        _adapterPropsSub?.Dispose(); _adapterPropsSub = null;
        _addedSub?.Dispose(); _addedSub = null;
        _removedSub?.Dispose(); _removedSub = null;
        foreach (var kv in _devicePropsSubs)
            kv.Value.Dispose();
        _devicePropsSubs.Clear();
    }
    private void Emit(BlueZDeviceEvent e) => DeviceEvent?.Invoke(e);
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _conn.Dispose();
    }
}
#endregion

#region ---- Program: Dashboard Display ----
internal sealed class Options
{
    public bool Debug { get; private set; }
    public static Options Parse(string[] args)
    {
        var o = new Options();
        foreach (var a in args)
        {
            if (a == "--debug") o.Debug = true;
        }
        return o;
    }
}

internal sealed class DeviceInfo
{
    public string DevicePath { get; set; } = "";
    public string? Address { get; set; }
    public string? Name { get; set; }
    public string? Alias { get; set; }
    public short? Rssi { get; set; }
    public DateTimeOffset LastEventTime { get; set; }
    public Dictionary<ushort, byte[]> ManufacturerData { get; set; } = new();
}

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        var opt = Options.Parse(args);
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

        await using var session = await BlueZScanSession.CreateAsync();
        var devices = new ConcurrentDictionary<string, DeviceInfo>(StringComparer.Ordinal);
        var gate = new object();
        var needsRedraw = false;

        if (opt.Debug)
        {
            session.Debug += msg =>
            {
                // デバッグメッセージは標準エラー出力へ
                lock (gate)
                {
                    Console.Error.WriteLine(msg);
                    Console.Error.Flush();
                }
            };
        }

        session.DeviceEvent += ev =>
        {
            lock (gate)
            {
                if (ev.Type == BlueZDeviceEventType.Lost)
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

                    if (ev.HasManufacturerDataUpdate && ev.ManufacturerData is not null)
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
        var drawTask = Task.Run(async () =>
        {
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
                try { await Task.Delay(100, cts.Token); } catch { }
            }
        });

        Console.WriteLine("Scanning... Press Enter to stop. (Ctrl+C also works)");
        var inputTask = Task.Run(() => Console.ReadLine());
        await Task.WhenAny(inputTask, Task.Delay(Timeout.Infinite, cts.Token)).ContinueWith(_ => { });

        cts.Cancel();
        await session.StopAsync();

        try { await drawTask; } catch { }

        return 0;
    }

    private static void DrawDashboard(ConcurrentDictionary<string, DeviceInfo> devices)
    {
        Console.Clear();
        Console.SetCursorPosition(0, 0);

        var now = DateTimeOffset.Now;
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                                              BLE Device Dashboard                                                                     ║");
        Console.WriteLine($"║  Last Update: {BleUtil.FmtTs(now),-20}                                               Device Count: {devices.Count,-3}                                    ║");
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
            var lastEvent = BleUtil.FmtTs(device.LastEventTime);
            var name = device.Name ?? device.Alias ?? "Unknown";
            if (name.Length > 29) name = name.Substring(0, 26) + "...";

            var mdInfo = "";
            if (device.ManufacturerData.Count > 0)
            {
                var firstMd = device.ManufacturerData.First();
                var hexData = BleUtil.ToHexDump(firstMd.Value);
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
}
#endregion
