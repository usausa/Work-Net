// Program.cs (single file)
// BlueZ BLE: Windows-like device/service/characteristic wrappers + scan logger (DISCOVER/LOST/UPDATE)
// using Tmds.DBus.
//
// What this provides:
// - Wrapper classes (restored):
//   * BlueZBluetoothLEDevice
//   * BlueZGattDeviceService
//   * BlueZGattCharacteristic
// - Scanner that raises events similar to your debug program:
//   * Discovered (DISCOVER): Device1 object appeared (InterfacesAdded) or already cached at startup
//   * Lost      (LOST):      Device object removed (InterfacesRemoved)
//   * Updated   (UPDATE):    Device1 PropertiesChanged (only when changed keys are non-empty)
// - Program prints logs for DISCOVER/LOST/UPDATE.
// - No periodic polling. Updates are only when BlueZ emits PropertiesChanged (same as last request).
//
// Options:
//   -a, --active   : accepted (note: no 1:1 mapping to Windows active scan)
//   --debug        : prints internal debug lines to stderr
//
// Notes:
// - BlueZ D-Bus is not a per-advertisement event stream; UPDATE depends on BlueZ emitting Device1 PropertiesChanged.
// - PropertiesChanged often contains partial properties (no Address/Name), so Program caches identity by device path.
//
// Build:
//   dotnet new console -n WorkLinuxBleScan
//   dotnet add package Tmds.DBus
//   dotnet run -- --debug
//
// Run (published):
//   ./WorkLinuxBleScan --debug

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

// Device connect/disconnect for completeness (not used by scan logger directly)
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

    public static bool? TryGetBool(IDictionary<string, object> props, string key)
        => props.TryGetValue(key, out var v) && v is bool b ? b : null;
}

#endregion

#region ---- Windows-like wrappers (restored) ----

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

    public static async Task<BlueZBluetoothLEDevice> FromAddressAsync(string address, CancellationToken ct)
    {
        // NOTE: This method is preserved; not used by scan logger directly.
        var target = BleUtil.NormalizeAddress(address);

        var conn = new Connection(Address.System);
        await conn.ConnectAsync();

        var objMgr = conn.CreateProxy<IObjectManager>("org.bluez", new ObjectPath("/"));
        var objects = await objMgr.GetManagedObjectsAsync();

        var adapterPath = objects.Keys.FirstOrDefault(p => objects[p].ContainsKey("org.bluez.Adapter1"));
        if (adapterPath == default)
            throw new InvalidOperationException("Bluetooth adapter (org.bluez.Adapter1) not found.");

        var adapter = conn.CreateProxy<IAdapter1>("org.bluez", adapterPath);

        // Find existing device by address (cached)
        var devicePath = FindDeviceByAddress(objects, target);
        if (devicePath == default)
            throw new InvalidOperationException("Device not found in cache. Use scanner to discover it first.");

        return new BlueZBluetoothLEDevice(conn, objMgr, adapterPath, adapter, devicePath);
    }

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
        _conn.Dispose(); // older Tmds.DBus: no DisposeAsync
    }

    private static ObjectPath FindDeviceByAddress(
        IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> objects,
        string targetAddress)
    {
        foreach (var kv in objects)
        {
            if (!kv.Value.TryGetValue("org.bluez.Device1", out var props))
                continue;

            if (!props.TryGetValue("Address", out var addrObj) || addrObj is not string addr)
                continue;

            if (string.Equals(BleUtil.NormalizeAddress(addr), targetAddress, StringComparison.OrdinalIgnoreCase))
                return kv.Key;
        }
        return default;
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
            if (bytes is null && v is IEnumerable<byte> eb)
                bytes = eb.ToArray();

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

#region ---- Scan event model using the wrappers ----

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

    // May be partial on Update
    public string? Address { get; init; }
    public string? Name { get; init; }
    public string? Alias { get; init; }
    public short? Rssi { get; init; }
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

        // InterfacesAdded = discover
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

        // InterfacesRemoved = lost
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

        // Initial dump (cached devices already known to BlueZ)
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

        // Start discovery
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
                    Rssi = BleUtil.TryGetInt16(ev.changed, "RSSI")
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

#region ---- Program: log output (DISCOVER/LOST/UPDATE) ----

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
            // Merge cache for better display on partial updates
            if (ev.Type != BlueZDeviceEventType.Lost)
            {
                var c = cache.GetOrAdd(ev.DevicePath, _ => new DeviceIdentityCache());
                if (ev.Address is not null) c.Address = ev.Address;
                if (ev.Name is not null) c.Name = ev.Name;
                if (ev.Alias is not null) c.Alias = ev.Alias;
                if (ev.Rssi is not null) c.Rssi = ev.Rssi;
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

        // Do not block D-Bus dispatch: wait input in background
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

        Console.WriteLine($"{ts} [{addr}] RSSI:{rssi} {name} <{tag}/{ev.Source}{FormatKeys(ev)}>");
    }

    private static void PrintLost(BlueZDeviceEvent ev, ConcurrentDictionary<string, DeviceIdentityCache> cache)
    {
        cache.TryRemove(ev.DevicePath, out var c);

        var ts = BleUtil.FmtTs(ev.Timestamp);
        var addr = c?.Address ?? "(NoAddr)";
        var name = c?.Name ?? c?.Alias ?? "(Unknown)";

        Console.WriteLine($"{ts} [{addr}] {name} <LOST/{ev.Source} ifaces=[{string.Join(",", ev.Keys)}]>");
    }

    private static string FormatKeys(BlueZDeviceEvent ev)
    {
        if (ev.Source != "PropertiesChanged")
            return string.Empty;
        return $" keys=[{string.Join(",", ev.Keys)}]";
    }
}

#endregion
