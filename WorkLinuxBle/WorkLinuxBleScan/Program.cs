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

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        bool active = args.Contains("--active") || args.Contains("-a");

        Console.Error.WriteLine("[DBG] Program start");
        Console.Error.Flush();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

        using var ctx = await BlueZContext.CreateAsync();
        await using var scanner = new BlueZScannerDebug(ctx);

        scanner.Debug += msg => { Console.Error.WriteLine(msg); Console.Error.Flush(); };

        scanner.DeviceDiscovered += e =>
        {
            Console.WriteLine($"{FmtTs(e.Timestamp)} [{e.Address ?? "(NoAddr)"}] RSSI:{e.Rssi?.ToString() ?? "?"} {e.Name ?? e.Alias ?? "(Unknown)"} <DISCOVER/{e.Source}>");
            Console.Out.Flush();
        };

        scanner.DeviceUpdated += e =>
        {
            Console.WriteLine($"{FmtTs(e.Timestamp)} [{e.Address ?? "(NoAddr)"}] RSSI:{e.Rssi?.ToString() ?? "?"} {e.Name ?? e.Alias ?? "(Unknown)"} <UPDATE/{e.Source}>");
            Console.Out.Flush();
        };

        await scanner.StartAsync(active, cts.Token);

        Console.Error.WriteLine("[DBG] Scanner started. Press Enter to stop.");
        Console.Error.Flush();

        // Enter wait on background
        var inputTask = Task.Run(() => Console.ReadLine());

        // Poll loop (to emulate "continuous Received")
        var pollTask = scanner.StartPollingAsync(
            interval: TimeSpan.FromSeconds(1),
            cts.Token);

        await Task.WhenAny(inputTask, Task.Delay(Timeout.Infinite, cts.Token)).ContinueWith(_ => { });
        cts.Cancel();

        Console.Error.WriteLine("[DBG] Stopping scanner...");
        Console.Error.Flush();

        await scanner.StopAsync();

        Console.Error.WriteLine("[DBG] Stopped.");
        Console.Error.Flush();

        return 0;
    }

    static string FmtTs(DateTimeOffset ts) => ts.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
}

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

        Console.Error.WriteLine($"[DBG] Using adapter: {adapterPath}");
        Console.Error.Flush();

        return new BlueZContext(conn, objMgr, adapterPath, adapter);
    }

    public void Dispose() => Connection.Dispose();
}

public sealed class DeviceEvent
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
    public string Source { get; init; } = "";
    public string DevicePath { get; init; } = "";
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

    // for polling diff (so we can say if RSSI changed)
    private readonly ConcurrentDictionary<ObjectPath, short?> _lastPolledRssi = new();

    public event Action<string>? Debug;
    public event Action<DeviceEvent>? DeviceDiscovered;
    public event Action<DeviceEvent>? DeviceUpdated;

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

                var e = BuildEvent(ev.objectPath, props, "InterfacesAdded");
                Debug?.Invoke($"[DBG] InterfacesAdded: path={ev.objectPath} addr={e.Address} name={e.Name ?? e.Alias} rssi={e.Rssi?.ToString() ?? "?"}");
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
                Debug?.Invoke($"[DBG] InterfacesRemoved: path={ev.objectPath}");
                if (_propsSubs.TryRemove(ev.objectPath, out var sub))
                    sub.Dispose();
                _lastPolledRssi.TryRemove(ev.objectPath, out _);
            }
            catch (Exception ex)
            {
                Debug?.Invoke("[DBG] InterfacesRemoved handler exception: " + ex);
            }
        });

        var objects = await _ctx.ObjectManager.GetManagedObjectsAsync();
        Debug?.Invoke($"[DBG] Initial GetManagedObjects: {objects.Count} objects");

        int deviceCount = 0;
        foreach (var kv in objects)
        {
            if (!kv.Value.TryGetValue("org.bluez.Device1", out var props))
                continue;

            deviceCount++;
            var e = BuildEvent(kv.Key, props, "InitialDump");
            Debug?.Invoke($"[DBG] InitialDump Device1: path={kv.Key} addr={e.Address} name={e.Name ?? e.Alias} rssi={e.Rssi?.ToString() ?? "?"}");
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

    public Task StartPollingAsync(TimeSpan interval, CancellationToken ct)
    {
        return Task.Run(async () =>
        {
            int tick = 0;
            while (!ct.IsCancellationRequested)
            {
                tick++;
                try
                {
                    var objects = await _ctx.ObjectManager.GetManagedObjectsAsync();

                    int devices = 0;
                    int withRssi = 0;
                    foreach (var kv in objects)
                    {
                        if (!kv.Value.TryGetValue("org.bluez.Device1", out var props))
                            continue;

                        devices++;
                        var e = BuildEvent(kv.Key, props, "Poll");
                        if (e.Rssi.HasValue) withRssi++;

                        // emulate "continuous adv": print every tick for every device
                        // (you can change this to only print for a target MAC etc.)
                        DeviceUpdated?.Invoke(e);

                        // diff info for RSSI
                        _lastPolledRssi.TryGetValue(kv.Key, out var last);
                        if (last != e.Rssi)
                        {
                            Debug?.Invoke($"[DBG] Poll RSSI changed path={kv.Key} {last?.ToString() ?? "?"} -> {e.Rssi?.ToString() ?? "?"}");
                            _lastPolledRssi[kv.Key] = e.Rssi;
                        }
                    }

                    Debug?.Invoke($"[DBG] Poll tick={tick} devices={devices} withRSSI={withRssi}");
                }
                catch (Exception ex)
                {
                    Debug?.Invoke("[DBG] Poll exception: " + ex.Message);
                }

                try { await Task.Delay(interval, ct); } catch { }
            }
        }, ct);
    }

    public async Task StopAsync()
    {
        Debug?.Invoke("[DBG] StopAsync entered");
        try
        {
            Debug?.Invoke("[DBG] Calling StopDiscovery...");
            await _ctx.Adapter.StopDiscoveryAsync();
            Debug?.Invoke("[DBG] StopDiscovery OK");
        }
        catch (Exception ex)
        {
            Debug?.Invoke("[DBG] StopDiscovery error: " + ex.Message);
        }

        _addedSub?.Dispose(); _addedSub = null;
        _removedSub?.Dispose(); _removedSub = null;

        foreach (var kv in _propsSubs)
            kv.Value.Dispose();
        _propsSubs.Clear();

        Debug?.Invoke("[DBG] StopAsync exit");
    }

    private async Task EnsurePropsSubscriptionAsync(ObjectPath devicePath)
    {
        if (_propsSubs.ContainsKey(devicePath))
            return;

        Debug?.Invoke($"[DBG] EnsurePropsSubscriptionAsync enter: {devicePath}");

        var propsProxy = _ctx.Connection.CreateProxy<IProperties>("org.bluez", devicePath);

        IDisposable sub = await propsProxy.WatchPropertiesChangedAsync(ev =>
        {
            try
            {
                if (!string.Equals(ev.iface, "org.bluez.Device1", StringComparison.Ordinal))
                    return;

                Debug?.Invoke($"[DBG] PropertiesChanged path={devicePath} keys=[{string.Join(",", ev.changed.Keys)}]");

                var e = BuildEvent(devicePath, ev.changed, "PropertiesChanged");
                DeviceUpdated?.Invoke(e);
            }
            catch (Exception ex)
            {
                Debug?.Invoke("[DBG] PropertiesChanged handler exception: " + ex);
            }
        });

        if (!_propsSubs.TryAdd(devicePath, sub))
        {
            sub.Dispose();
            Debug?.Invoke($"[DBG] EnsurePropsSubscriptionAsync lost race: {devicePath}");
            return;
        }

        Debug?.Invoke($"[DBG] Subscribed PropertiesChanged for {devicePath}");
    }

    private static DeviceEvent BuildEvent(ObjectPath devicePath, IDictionary<string, object> props, string source)
    {
        return new DeviceEvent
        {
            Timestamp = DateTimeOffset.Now,
            Source = source,
            DevicePath = devicePath.ToString(),
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
