// Program.cs (single file)
// BlueZ BLE scan logger (DISCOVER/LOST/UPDATE) + ManufacturerData dump when received
// using Tmds.DBus.
//
// Adds:
// - Parse ManufacturerData (org.bluez.Device1 ManufacturerData: a{qv})
// - When UPDATE(PropertiesChanged) includes ManufacturerData, print a hex dump on the next line.
//
// Options:
//   -a, --active
//   --debug

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

#region ---- Low level D-Bus interfaces (public) ----

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

[DBusInterface("org.bluez.Device1")]
public interface IDevice1 : IDBusObject
{
    Task ConnectAsync();
    Task DisconnectAsync();
}

[DBusInterface("org.bluez.GattCharacteristic1")]
public interface IGattCharacteristic1 : IDBusObject
{
    Task StartNotifyAsync();
    Task StopNotifyAsync();
    Task WriteValueAsync(byte[] value, IDictionary<string, object> options);
}

[DBusInterface("org.freedesktop.DBus.Properties")]
public interface IProperties : IDBusObject
{
    Task<IDisposable> WatchPropertiesChangedAsync(Action<(string iface, IDictionary<string, object> changed, string[] invalidated)> handler);
}

#endregion

#region ---- Utilities ----

internal static class BleUtil
{
    public static string NormalizeAddress(string input)
    {
        var sb = new StringBuilder(32);
        foreach (var ch in input)
        {
            if (ch == ':' || ch == '-') continue;
            sb.Append(char.ToUpperInvariant(ch));
        }
        var s = sb.ToString();
        if (s.Length == 12)
        {
            return $"{s[0]}{s[1]}:{s[2]}{s[3]}:{s[4]}{s[5]}:{s[6]}{s[7]}:{s[8]}{s[9]}:{s[10]}{s[11]}";
        }
        return input;
    }

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

    public static string ToHexDump(byte[] data, int bytesPerLine = 16)
    {
        const string hexChars = "0123456789ABCDEF";
        var sb = new StringBuilder();
        for (int i = 0; i < data.Length; i += bytesPerLine)
        {
            int n = Math.Min(bytesPerLine, data.Length - i);
            sb.Append("    "); // indent (next line)
            for (int j = 0; j < n; j++)
            {
                byte b = data[i + j];
                sb.Append(hexChars[b >> 4]);
                sb.Append(hexChars[b & 0xF]);
                if (j + 1 != n) sb.Append(' ');
            }
            if (i + n < data.Length) sb.AppendLine();
        }
        return sb.ToString();
    }

    // ManufacturerData: a{qv} (UInt16 -> variant(byte[]))
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
                if (kv.Value is byte[] bytes)
                    res[kv.Key] = bytes;
                else if (kv.Value is IEnumerable<byte> eb)
                    res[kv.Key] = eb.ToArray();
            }
            return res;
        }

        return null;
    }
}

#endregion

#region ---- Windows-like wrappers (restored; not required for scan logger but kept as requested) ----

public sealed class BlueZBluetoothLEDevice : IAsyncDisposable
{
    private readonly Connection _conn;
    private readonly IObjectManager _objMgr;

    private readonly ObjectPath _adapterPath;
    private readonly IAdapter1 _adapter;

    private readonly ObjectPath _devicePath;
    private readonly IDevice1 _deviceProxy;

    private BlueZBluetoothLEDevice(Connection conn, IObjectManager objMgr, ObjectPath adapterPath, IAdapter1 adapter, ObjectPath devicePath)
    {
        _conn = conn;
        _objMgr = objMgr;
        _adapterPath = adapterPath;
        _adapter = adapter;
        _devicePath = devicePath;
        _deviceProxy = _conn.CreateProxy<IDevice1>("org.bluez", _devicePath);
    }

    public string DevicePath => _devicePath.ToString();

    public async Task ConnectAsync() => await _deviceProxy.ConnectAsync();
    public async Task DisconnectAsync() => await _deviceProxy.DisconnectAsync();

    public async Task<IReadOnlyList<BlueZGattDeviceService>> GetGattServicesAsync()
    {
        var objects = await _objMgr.GetManagedObjectsAsync();
        var list = new List<BlueZGattDeviceService>();

        foreach (var kv in objects)
        {
            var path = kv.Key.ToString();
            if (!path.StartsWith(_devicePath.ToString(), StringComparison.Ordinal))
                continue;

            if (!kv.Value.TryGetValue("org.bluez.GattService1", out var props))
                continue;

            var uuid = props.TryGetValue("UUID", out var uuidObj) ? uuidObj as string : null;
            list.Add(new BlueZGattDeviceService(_conn, _objMgr, kv.Key, uuid));
        }
        return list;
    }

    public async ValueTask DisposeAsync()
    {
        try { await DisconnectAsync(); } catch { }
        _conn.Dispose();
    }
}

public sealed class BlueZGattDeviceService
{
    private readonly Connection _conn;
    private readonly IObjectManager _objMgr;

    internal BlueZGattDeviceService(Connection conn, IObjectManager objMgr, ObjectPath servicePath, string? uuid)
    {
        _conn = conn;
        _objMgr = objMgr;
        ObjectPath = servicePath;
        Uuid = Guid.TryParse(uuid, out var g) ? g : Guid.Empty;
    }

    public ObjectPath ObjectPath { get; }
    public Guid Uuid { get; }

    public async Task<IReadOnlyList<BlueZGattCharacteristic>> GetCharacteristicsAsync()
    {
        var objects = await _objMgr.GetManagedObjectsAsync();
        var list = new List<BlueZGattCharacteristic>();

        foreach (var kv in objects)
        {
            var path = kv.Key.ToString();
            if (!path.StartsWith(ObjectPath.ToString(), StringComparison.Ordinal))
                continue;

            if (!kv.Value.TryGetValue("org.bluez.GattCharacteristic1", out var props))
                continue;

            var uuid = props.TryGetValue("UUID", out var uuidObj) ? uuidObj as string : null;
            list.Add(new BlueZGattCharacteristic(_conn, kv.Key, uuid));
        }
        return list;
    }
}

public sealed class BlueZGattCharacteristic : IAsyncDisposable
{
    private readonly Connection _conn;
    private readonly ObjectPath _charPath;
    private readonly IGattCharacteristic1 _ch;
    private readonly IProperties _props;
    private IDisposable? _sub;

    internal BlueZGattCharacteristic(Connection conn, ObjectPath charPath, string? uuid)
    {
        _conn = conn;
        _charPath = charPath;
        Uuid = Guid.TryParse(uuid, out var g) ? g : Guid.Empty;

        _ch = _conn.CreateProxy<IGattCharacteristic1>("org.bluez", _charPath);
        _props = _conn.CreateProxy<IProperties>("org.bluez", _charPath);
    }

    public Guid Uuid { get; }
    public event EventHandler<byte[]>? ValueChanged;

    public async Task StartNotifyAsync()
    {
        await _ch.StartNotifyAsync();

        _sub?.Dispose();
        _sub = await _props.WatchPropertiesChangedAsync(ev =>
        {
            if (!string.Equals(ev.iface, "org.bluez.GattCharacteristic1", StringComparison.Ordinal))
                return;

            if (!ev.changed.TryGetValue("Value", out var v) || v is null)
                return;

            byte[]? bytes = v as byte[];
            if (bytes is null && v is IEnumerable<byte> eb) bytes = eb.ToArray();
            if (bytes is null) return;

            ValueChanged?.Invoke(this, bytes);
        });
    }

    public async Task StopNotifyAsync()
    {
        _sub?.Dispose();
        _sub = null;
        try { await _ch.StopNotifyAsync(); } catch { }
    }

    public Task WriteValueAsync(byte[] data) => _ch.WriteValueAsync(data, new Dictionary<string, object>());

    public async ValueTask DisposeAsync() => await StopNotifyAsync();
}

#endregion

#region ---- Scan session (DISCOVER/LOST/UPDATE) ----

public enum BlueZDeviceEventType
{
    Discover,
    Update,
    Lost
}

public sealed class BlueZDeviceEvent
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
    public BlueZDeviceEventType Type { get; init; }
    public string Source { get; init; } = ""; // InitialDump / InterfacesAdded / PropertiesChanged / InterfacesRemoved

    public string DevicePath { get; init; } = "";
    public IReadOnlyCollection<string> Keys { get; init; } = Array.Empty<string>();

    public string? Address { get; init; }
    public string? Name { get; init; }
    public string? Alias { get; init; }
    public short? Rssi { get; init; }

    // Only set when present in PropertiesChanged (or if you choose to parse from full props)
    public IReadOnlyDictionary<ushort, byte[]>? ManufacturerData { get; init; }
    public bool HasManufacturerDataUpdate { get; init; }
}

public sealed class BlueZScanSession : IAsyncDisposable
{
    private readonly Connection _conn;
    private readonly IObjectManager _objMgr;
    private readonly ObjectPath _adapterPath;
    private readonly IAdapter1 _adapter;

    private IDisposable? _addedSub;
    private IDisposable? _removedSub;

    private readonly ConcurrentDictionary<ObjectPath, IDisposable> _propsSubs = new();

    public event Action<string>? Debug;
    public event Action<BlueZDeviceEvent>? DeviceEvent;

    private BlueZScanSession(Connection conn, IObjectManager objMgr, ObjectPath adapterPath, IAdapter1 adapter)
    {
        _conn = conn;
        _objMgr = objMgr;
        _adapterPath = adapterPath;
        _adapter = adapter;
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

        var adapter = conn.CreateProxy<IAdapter1>("org.bluez", adapterPath);
        return new BlueZScanSession(conn, objMgr, adapterPath, adapter);
    }

    public async Task StartAsync(bool active, CancellationToken ct)
    {
        Debug?.Invoke($"[DBG] Using adapter: {_adapterPath}");
        if (active)
            Debug?.Invoke("[DBG] --active specified (note: no 1:1 mapping on BlueZ)");

        _addedSub = await _objMgr.WatchInterfacesAddedAsync(ev =>
        {
            try
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
            }
            catch (Exception ex)
            {
                Debug?.Invoke("[DBG] InterfacesAdded exception: " + ex.Message);
            }
        });

        _removedSub = await _objMgr.WatchInterfacesRemovedAsync(ev =>
        {
            try
            {
                if (_propsSubs.TryRemove(ev.objectPath, out var sub))
                    sub.Dispose();

                Emit(new BlueZDeviceEvent
                {
                    Timestamp = DateTimeOffset.Now,
                    Type = BlueZDeviceEventType.Lost,
                    Source = "InterfacesRemoved",
                    DevicePath = ev.objectPath.ToString(),
                    Keys = ev.interfaces.ToArray()
                });
            }
            catch (Exception ex)
            {
                Debug?.Invoke("[DBG] InterfacesRemoved exception: " + ex.Message);
            }
        });

        // InitialDump (one-time)
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

        try
        {
            Debug?.Invoke("[DBG] Calling StartDiscovery...");
            await _adapter.StartDiscoveryAsync();
            Debug?.Invoke("[DBG] StartDiscovery OK");
        }
        catch (DBusException ex)
        {
            Debug?.Invoke($"[DBG] StartDiscovery DBusException: {ex.ErrorName} {ex.Message}");
            if (ex.ErrorName?.Contains("InProgress", StringComparison.OrdinalIgnoreCase) != true)
                throw;
        }

        _ = ct;
    }

    public async Task StopAsync()
    {
        try { await _adapter.StopDiscoveryAsync(); } catch { }

        _addedSub?.Dispose(); _addedSub = null;
        _removedSub?.Dispose(); _removedSub = null;

        foreach (var kv in _propsSubs)
            kv.Value.Dispose();
        _propsSubs.Clear();
    }

    private async Task EnsureDevicePropsSubscriptionAsync(ObjectPath devicePath)
    {
        if (_propsSubs.ContainsKey(devicePath))
            return;

        var propsProxy = _conn.CreateProxy<IProperties>("org.bluez", devicePath);
        var sub = await propsProxy.WatchPropertiesChangedAsync(ev =>
        {
            try
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
                    ManufacturerData = md,
                    HasManufacturerDataUpdate = hasMd
                });
            }
            catch (Exception ex)
            {
                Debug?.Invoke("[DBG] PropertiesChanged exception: " + ex.Message);
            }
        });

        if (!_propsSubs.TryAdd(devicePath, sub))
            sub.Dispose();
        else
            Debug?.Invoke($"[DBG] Subscribed PropertiesChanged for {devicePath}");
    }

    private void Emit(BlueZDeviceEvent e) => DeviceEvent?.Invoke(e);

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _conn.Dispose();
    }
}

#endregion

#region ---- Program: log output ----

internal sealed class Options
{
    public bool Active { get; private set; }
    public bool Debug { get; private set; }

    public static Options Parse(string[] args)
    {
        var o = new Options();
        foreach (var a in args)
        {
            switch (a)
            {
                case "--active":
                case "-a":
                    o.Active = true; break;
                case "--debug":
                    o.Debug = true; break;
            }
        }
        return o;
    }
}

internal sealed class DeviceIdentityCache
{
    public string? Address;
    public string? Name;
    public string? Alias;
    public short? Rssi;
}

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        var opt = Options.Parse(args);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

        await using var session = await BlueZScanSession.CreateAsync();

        var cache = new ConcurrentDictionary<string, DeviceIdentityCache>(StringComparer.Ordinal);
        var gate = new object();

        if (opt.Debug)
        {
            session.Debug += msg =>
            {
                lock (gate)
                {
                    Console.Error.WriteLine(msg);
                    Console.Error.Flush();
                }
            };
        }

        session.DeviceEvent += ev =>
        {
            if (ev.Type != BlueZDeviceEventType.Lost)
            {
                var c = cache.GetOrAdd(ev.DevicePath, _ => new DeviceIdentityCache());
                MergeCache(c, ev);
            }

            lock (gate)
            {
                switch (ev.Type)
                {
                    case BlueZDeviceEventType.Discover:
                        PrintLine(ev, cache, "DISCOVER");
                        break;

                    case BlueZDeviceEventType.Update:
                        PrintLine(ev, cache, "UPDATE");

                        // Added requirement: dump ManufacturerData on the next line when received
                        if (ev.Source == "PropertiesChanged" && ev.HasManufacturerDataUpdate)
                        {
                            if (ev.ManufacturerData is null || ev.ManufacturerData.Count == 0)
                            {
                                Console.WriteLine("    ManufacturerData: (empty)");
                            }
                            else
                            {
                                foreach (var md in ev.ManufacturerData)
                                {
                                    Console.WriteLine($"    ManufacturerData CompanyId=0x{md.Key:X4} Len={md.Value.Length}");
                                    Console.WriteLine(BleUtil.ToHexDump(md.Value));
                                }
                            }
                        }
                        break;

                    case BlueZDeviceEventType.Lost:
                        PrintLost(ev, cache);
                        break;
                }

                Console.Out.Flush();
            }
        };

        await session.StartAsync(opt.Active, cts.Token);

        Console.WriteLine("Scanning... Press Enter to stop. (Ctrl+C also works)");
        Console.Out.Flush();

        var inputTask = Task.Run(() => Console.ReadLine());
        await Task.WhenAny(inputTask, Task.Delay(Timeout.Infinite, cts.Token)).ContinueWith(_ => { });
        cts.Cancel();

        await session.StopAsync();
        return 0;
    }

    private static void PrintLine(BlueZDeviceEvent ev, ConcurrentDictionary<string, DeviceIdentityCache> cache, string tag)
    {
        cache.TryGetValue(ev.DevicePath, out var c);

        var ts = BleUtil.FmtTs(ev.Timestamp);
        var addr = ev.Address ?? c?.Address ?? "(NoAddr)";
        var name = ev.Name ?? ev.Alias ?? c?.Name ?? c?.Alias ?? "(Unknown)";
        var rssi = (ev.Rssi ?? c?.Rssi)?.ToString(CultureInfo.InvariantCulture) ?? "?";

        var extra = ev.Source == "PropertiesChanged"
            ? $" keys=[{string.Join(",", ev.Keys)}]"
            : "";

        Console.WriteLine($"{ts} [{addr}] RSSI:{rssi} {name} <{tag}/{ev.Source}{extra}>");
    }

    private static void PrintLost(BlueZDeviceEvent ev, ConcurrentDictionary<string, DeviceIdentityCache> cache)
    {
        cache.TryRemove(ev.DevicePath, out var c);

        var ts = BleUtil.FmtTs(ev.Timestamp);
        var addr = c?.Address ?? "(NoAddr)";
        var name = c?.Name ?? c?.Alias ?? "(Unknown)";

        Console.WriteLine($"{ts} [{addr}] {name} <LOST/{ev.Source} ifaces=[{string.Join(",", ev.Keys)}]>");
    }

    private static void MergeCache(DeviceIdentityCache c, BlueZDeviceEvent e)
    {
        if (e.Address is not null) c.Address = e.Address;
        if (e.Name is not null) c.Name = e.Name;
        if (e.Alias is not null) c.Alias = e.Alias;
        if (e.Rssi is not null) c.Rssi = e.Rssi;
    }
}

#endregion
