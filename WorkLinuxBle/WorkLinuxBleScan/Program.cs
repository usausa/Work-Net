// Program.cs (single file)
// BlueZ (Linux) BLE scan logger using Tmds.DBus
//
// Requirements implemented:
// - No periodic polling inside BlueZScannerDebug (event-driven only)
// - Print on:
//   * Device discovered (InterfacesAdded / initial cached devices at startup)
//   * Device lost (InterfacesRemoved)
//   * Device updated (PropertiesChanged) with changed keys
// - Cache Address/Name/Alias per devicePath so PropertiesChanged lines can show them even for partial updates.
//
// Options:
//   -a, --active   : accepted (note: no 1:1 mapping to Windows active scan)
//   --debug        : print internal debug logs to stderr

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

#region D-Bus interfaces

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

#region BlueZ context + scanner (no polling)

public sealed class BlueZContext : IDisposable
{
    public Connection Connection { get; }
    public IObjectManager ObjectManager { get; }
    public ObjectPath AdapterPath { get; }
    public IAdapter1 Adapter { get; }

    private BlueZContext(Connection conn, IObjectManager objMgr, ObjectPath adapterPath, IAdapter1 adapter)
    {
        Connection = conn;
        ObjectManager = objMgr;
        AdapterPath = adapterPath;
        Adapter = adapter;
    }

    public static async Task<BlueZContext> CreateAsync()
    {
        var conn = new Connection(Address.System);
        await conn.ConnectAsync();

        var objMgr = conn.CreateProxy<IObjectManager>("org.bluez", new ObjectPath("/"));
        var objects = await objMgr.GetManagedObjectsAsync();

        var adapterPath = objects.Keys.FirstOrDefault(p => objects[p].ContainsKey("org.bluez.Adapter1"));
        if (adapterPath == default)
            throw new InvalidOperationException("Bluetooth adapter (org.bluez.Adapter1) not found.");

        var adapter = conn.CreateProxy<IAdapter1>("org.bluez", adapterPath);
        return new BlueZContext(conn, objMgr, adapterPath, adapter);
    }

    public void Dispose() => Connection.Dispose();
}

public sealed class DeviceEvent
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    public string DevicePath { get; init; } = "";
    public string Source { get; init; } = ""; // InitialDump / InterfacesAdded / PropertiesChanged / InterfacesRemoved

    // Changed keys for PropertiesChanged; for others, may carry a property-key list
    public IReadOnlyCollection<string> Keys { get; init; } = Array.Empty<string>();

    public string? Address { get; init; }
    public string? Name { get; init; }
    public string? Alias { get; init; }
    public short? Rssi { get; init; }
}

public sealed class BlueZScannerDebug : IAsyncDisposable
{
    private readonly BlueZContext _ctx;

    private IDisposable? _addedSub;
    private IDisposable? _removedSub;
    private readonly ConcurrentDictionary<ObjectPath, IDisposable> _propsSubs = new();

    public event Action<string>? Debug;

    public event Action<DeviceEvent>? DeviceDiscovered; // InitialDump / InterfacesAdded
    public event Action<DeviceEvent>? DeviceLost;       // InterfacesRemoved
    public event Action<DeviceEvent>? DeviceUpdated;    // PropertiesChanged

    public BlueZScannerDebug(BlueZContext ctx) => _ctx = ctx;

    public async Task StartAsync(bool active, CancellationToken ct)
    {
        Debug?.Invoke("[DBG] StartAsync entered");

        _addedSub = await _ctx.ObjectManager.WatchInterfacesAddedAsync(ev =>
        {
            try
            {
                if (!ev.interfaces.TryGetValue("org.bluez.Device1", out var props))
                    return;

                var e = BuildEvent(ev.objectPath, props, source: "InterfacesAdded", keys: props.Keys);
                Debug?.Invoke($"[DBG] InterfacesAdded: path={ev.objectPath} keys=[{string.Join(",", props.Keys)}]");

                DeviceDiscovered?.Invoke(e);

                _ = EnsurePropsSubscriptionAsync(ev.objectPath);
            }
            catch (Exception ex)
            {
                Debug?.Invoke("[DBG] InterfacesAdded handler exception: " + ex);
            }
        });

        _removedSub = await _ctx.ObjectManager.WatchInterfacesRemovedAsync(ev =>
        {
            try
            {
                Debug?.Invoke($"[DBG] InterfacesRemoved: path={ev.objectPath} ifaces=[{string.Join(",", ev.interfaces)}]");

                if (_propsSubs.TryRemove(ev.objectPath, out var sub))
                    sub.Dispose();

                DeviceLost?.Invoke(new DeviceEvent
                {
                    Timestamp = DateTimeOffset.Now,
                    DevicePath = ev.objectPath.ToString(),
                    Source = "InterfacesRemoved",
                    Keys = ev.interfaces
                });
            }
            catch (Exception ex)
            {
                Debug?.Invoke("[DBG] InterfacesRemoved handler exception: " + ex);
            }
        });

        // One-time snapshot at start (not polling)
        var objects = await _ctx.ObjectManager.GetManagedObjectsAsync();
        int deviceCount = 0;
        foreach (var kv in objects)
        {
            if (!kv.Value.TryGetValue("org.bluez.Device1", out var props))
                continue;

            deviceCount++;
            var e = BuildEvent(kv.Key, props, source: "InitialDump", keys: props.Keys);
            DeviceDiscovered?.Invoke(e);

            _ = EnsurePropsSubscriptionAsync(kv.Key);
        }
        Debug?.Invoke($"[DBG] InitialDump Device1 count: {deviceCount}");

        if (active)
            Debug?.Invoke("[DBG] --active specified (note: no 1:1 mapping on BlueZ)");

        try
        {
            Debug?.Invoke("[DBG] Calling StartDiscovery...");
            await _ctx.Adapter.StartDiscoveryAsync();
            Debug?.Invoke("[DBG] StartDiscovery OK");
        }
        catch (DBusException ex)
        {
            Debug?.Invoke($"[DBG] StartDiscovery DBusException: {ex.ErrorName} {ex.Message}");
            if (ex.ErrorName?.Contains("InProgress", StringComparison.OrdinalIgnoreCase) != true)
                throw;
        }

        _ = ct;
        Debug?.Invoke("[DBG] StartAsync exit");
    }

    public async Task StopAsync()
    {
        try { await _ctx.Adapter.StopDiscoveryAsync(); } catch { }

        _addedSub?.Dispose(); _addedSub = null;
        _removedSub?.Dispose(); _removedSub = null;

        foreach (var kv in _propsSubs)
            kv.Value.Dispose();
        _propsSubs.Clear();
    }

    private async Task EnsurePropsSubscriptionAsync(ObjectPath devicePath)
    {
        if (_propsSubs.ContainsKey(devicePath))
            return;

        var propsProxy = _ctx.Connection.CreateProxy<IProperties>("org.bluez", devicePath);

        IDisposable sub = await propsProxy.WatchPropertiesChangedAsync(ev =>
        {
            try
            {
                if (!string.Equals(ev.iface, "org.bluez.Device1", StringComparison.Ordinal))
                    return;

                // Ignore empty changed set (often seen at stop)
                if (ev.changed.Count == 0)
                    return;

                Debug?.Invoke($"[DBG] PropertiesChanged path={devicePath} keys=[{string.Join(",", ev.changed.Keys)}]");

                var e = BuildEvent(devicePath, ev.changed, source: "PropertiesChanged", keys: ev.changed.Keys);
                DeviceUpdated?.Invoke(e);
            }
            catch (Exception ex)
            {
                Debug?.Invoke("[DBG] PropertiesChanged handler exception: " + ex);
            }
        });

        if (!_propsSubs.TryAdd(devicePath, sub))
            sub.Dispose();
        else
            Debug?.Invoke($"[DBG] Subscribed PropertiesChanged for {devicePath}");
    }

    private static DeviceEvent BuildEvent(ObjectPath devicePath, IDictionary<string, object> props, string source, IEnumerable<string> keys)
    {
        return new DeviceEvent
        {
            Timestamp = DateTimeOffset.Now,
            DevicePath = devicePath.ToString(),
            Source = source,
            Keys = keys.ToArray(),
            Address = props.TryGetValue("Address", out var a) ? a as string : null,
            Name = props.TryGetValue("Name", out var n) ? n as string : null,
            Alias = props.TryGetValue("Alias", out var al) ? al as string : null,
            Rssi = TryGetInt16(props, "RSSI")
        };
    }

    private static short? TryGetInt16(IDictionary<string, object> props, string key)
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

    public async ValueTask DisposeAsync() => await StopAsync();
}

#endregion

#region Program (print discovered/lost/updated; cache identity)

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
                    o.Active = true;
                    break;
                case "--debug":
                    o.Debug = true;
                    break;
            }
        }
        return o;
    }
}

internal sealed class DeviceCache
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

        using var ctx = await BlueZContext.CreateAsync();
        await using var scanner = new BlueZScannerDebug(ctx);

        var cache = new ConcurrentDictionary<string, DeviceCache>(StringComparer.Ordinal);
        var gate = new object();

        if (opt.Debug)
        {
            scanner.Debug += msg =>
            {
                lock (gate)
                {
                    Console.Error.WriteLine(msg);
                    Console.Error.Flush();
                }
            };
        }

        scanner.DeviceDiscovered += e =>
        {
            var c = cache.GetOrAdd(e.DevicePath, _ => new DeviceCache());
            MergeCache(c, e);

            lock (gate)
            {
                var ts = FmtTs(e.Timestamp);
                Console.WriteLine($"{ts} [{c.Address ?? "(NoAddr)"}] RSSI:{(c.Rssi?.ToString() ?? "?")} {c.Name ?? c.Alias ?? "(Unknown)"} <DISCOVER/{e.Source}>");
                if (opt.Debug)
                    Console.WriteLine($"    path={e.DevicePath} keys=[{string.Join(",", e.Keys)}]");
                Console.Out.Flush();
            }
        };

        scanner.DeviceLost += e =>
        {
            cache.TryRemove(e.DevicePath, out var c);

            lock (gate)
            {
                var ts = FmtTs(e.Timestamp);
                var addr = c?.Address ?? "(NoAddr)";
                var name = c?.Name ?? c?.Alias ?? "(Unknown)";
                Console.WriteLine($"{ts} [{addr}] {name} <LOST/{e.Source} ifaces=[{string.Join(",", e.Keys)}]>");
                Console.Out.Flush();
            }
        };

        scanner.DeviceUpdated += e =>
        {
            if (e.Source != "PropertiesChanged")
                return;

            var c = cache.GetOrAdd(e.DevicePath, _ => new DeviceCache());
            MergeCache(c, e);

            lock (gate)
            {
                var ts = FmtTs(e.Timestamp);
                var addr = c.Address ?? "(NoAddr)";
                var name = c.Name ?? c.Alias ?? "(Unknown)";
                var rssi = (e.Rssi ?? c.Rssi)?.ToString(CultureInfo.InvariantCulture) ?? "?";

                Console.WriteLine($"{ts} [{addr}] RSSI:{rssi} {name} <UPDATE/PropertiesChanged keys=[{string.Join(",", e.Keys)}]>");
                Console.Out.Flush();
            }
        };

        await scanner.StartAsync(opt.Active, cts.Token);

        Console.WriteLine("Scanning... Press Enter to stop. (Ctrl+C also works)");
        Console.Out.Flush();

        // avoid blocking D-Bus dispatch: wait input in background
        var inputTask = Task.Run(() => Console.ReadLine());
        await Task.WhenAny(inputTask, Task.Delay(Timeout.Infinite, cts.Token)).ContinueWith(_ => { });
        cts.Cancel();

        await scanner.StopAsync();
        return 0;
    }

    private static void MergeCache(DeviceCache c, DeviceEvent e)
    {
        if (e.Address is not null) c.Address = e.Address;
        if (e.Name is not null) c.Name = e.Name;
        if (e.Alias is not null) c.Alias = e.Alias;
        if (e.Rssi is not null) c.Rssi = e.Rssi;
    }

    private static string FmtTs(DateTimeOffset ts) =>
        ts.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
}

#endregion
