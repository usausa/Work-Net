namespace WorkLinuxBleScan2;

using System.Collections.Concurrent;

using Tmds.DBus;

public enum BleScanEventType
{
    Discover,
    Update,
    Lost
}

public sealed class BleScanEvent
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    public BleScanEventType Type { get; init; }

    public string DevicePath { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Keys { get; init; } = [];

    public string? Address { get; init; }

    public string? Name { get; init; }

    public string? Alias { get; init; }

    public short? Rssi { get; init; }

    public IReadOnlyDictionary<ushort, byte[]>? ManufacturerData { get; init; }
}

public sealed class BleScanSession : IAsyncDisposable
{
    public event Action<string>? Debug;
    public event Action<BleScanEvent>? DeviceEvent;

    private readonly Connection connection;
    private readonly IObjectManager objectManager;
    private readonly IAdapter1 adapter;
    private readonly IProperties adapterProperties;

    private IDisposable? addedSubscription;
    private IDisposable? removedSubscription;
    private IDisposable? adapterPropertySubscriptions;

    private readonly ConcurrentDictionary<ObjectPath, IDisposable> devicePropertySubscriptions = new();

    private CancellationTokenSource? keepAliveCts;
    private Task? keepAliveTask;

    private volatile bool discovering;

    private BleScanSession(Connection connection, IObjectManager objectManager, IAdapter1 adapter, IProperties adapterProperties)
    {
        this.connection = connection;
        this.objectManager = objectManager;
        this.adapter = adapter;
        this.adapterProperties = adapterProperties;
    }

    public static async Task<BleScanSession> CreateAsync()
    {
        var con = new Connection(Address.System);
        await con.ConnectAsync();

        var manager = con.CreateProxy<IObjectManager>("org.bluez", new ObjectPath("/"));
        var objects = await manager.GetManagedObjectsAsync();
        var adapterPath = objects.Keys.FirstOrDefault(p => objects[p].ContainsKey("org.bluez.Adapter1"));
        if (adapterPath == default)
        {
            throw new InvalidOperationException("Bluetooth adapter (org.bluez.Adapter1) not found.");
        }

        var adapter = con.CreateProxy<IAdapter1>("org.bluez", adapterPath);
        var adapterProperties = con.CreateProxy<IProperties>("org.bluez", adapterPath);

        return new BleScanSession(con, manager, adapter, adapterProperties);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        connection.Dispose();
    }

    private void RaiseEvent(BleScanEvent e) => DeviceEvent?.Invoke(e);

    public async Task StartAsync(CancellationToken ct)
    {
        adapterPropertySubscriptions = await adapterProperties.WatchPropertiesChangedAsync(ev =>
        {
            if (!String.Equals(ev.Interface, "org.bluez.Adapter1", StringComparison.Ordinal))
            {
                return;
            }

            if (ev.Changed.TryGetValue("Discovering", out var v) && v is bool b)
            {
                discovering = b;
                Debug?.Invoke($"[DBG] Adapter Discovering changed: {discovering}");
            }
        });
        addedSubscription = await objectManager.WatchInterfacesAddedAsync(ev =>
        {
            if (!ev.Interfaces.TryGetValue("org.bluez.Device1", out var props))
                return;
            RaiseEvent(new BleScanEvent
            {
                Timestamp = DateTimeOffset.Now,
                Type = BleScanEventType.Discover,
                DevicePath = ev.ObjectPath.ToString(),
                Keys = props.Keys.ToArray(),
                Address = props.TryGetValue("Address", out var a) ? a as string : null,
                Name = props.TryGetValue("Name", out var n) ? n as string : null,
                Alias = props.TryGetValue("Alias", out var al) ? al as string : null,
                Rssi = TryGetInt16(props, "RSSI")
            });
            _ = EnsureDevicePropsSubscriptionAsync(ev.ObjectPath);
        });
        removedSubscription = await objectManager.WatchInterfacesRemovedAsync(ev =>
        {
            if (devicePropertySubscriptions.TryRemove(ev.ObjectPath, out var sub))
                sub.Dispose();
            RaiseEvent(new BleScanEvent
            {
                Timestamp = DateTimeOffset.Now,
                Type = BleScanEventType.Lost,
                DevicePath = ev.ObjectPath.ToString(),
                Keys = ev.Interfaces.ToArray()
            });
        });
        var objects = await objectManager.GetManagedObjectsAsync();
        foreach (var kv in objects)
        {
            if (!kv.Value.TryGetValue("org.bluez.Device1", out var props))
                continue;
            RaiseEvent(new BleScanEvent
            {
                Timestamp = DateTimeOffset.Now,
                Type = BleScanEventType.Discover,
                DevicePath = kv.Key.ToString(),
                Keys = props.Keys.ToArray(),
                Address = props.TryGetValue("Address", out var a) ? a as string : null,
                Name = props.TryGetValue("Name", out var n) ? n as string : null,
                Alias = props.TryGetValue("Alias", out var al) ? al as string : null,
                Rssi = TryGetInt16(props, "RSSI")
            });
            _ = EnsureDevicePropsSubscriptionAsync(kv.Key);
        }
        await StartDiscoverySafeAsync();
        keepAliveCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        keepAliveTask = Task.Run(() => KeepAliveLoopAsync(keepAliveCts.Token));
    }
    private async Task KeepAliveLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!discovering)
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
            await adapter.StartDiscoveryAsync();
            Debug?.Invoke("[DBG] StartDiscovery OK");
            discovering = true;
        }
        catch (DBusException ex) when (ex.ErrorName?.Contains("InProgress", StringComparison.OrdinalIgnoreCase) == true)
        {
            Debug?.Invoke("[DBG] StartDiscovery: already in progress");
            discovering = true;
        }
    }
    private async Task EnsureDevicePropsSubscriptionAsync(ObjectPath devicePath)
    {
        if (devicePropertySubscriptions.ContainsKey(devicePath))
            return;
        var propsProxy = connection.CreateProxy<IProperties>("org.bluez", devicePath);
        var sub = await propsProxy.WatchPropertiesChangedAsync(ev =>
        {
            if (!string.Equals(ev.Interface, "org.bluez.Device1", StringComparison.Ordinal))
                return;
            if (ev.Changed.Count == 0)
                return;
            var hasMd = ev.Changed.ContainsKey("ManufacturerData");
            var md = hasMd ? TryGetManufacturerData(ev.Changed) : null;
            RaiseEvent(new BleScanEvent
            {
                Timestamp = DateTimeOffset.Now,
                Type = BleScanEventType.Update,
                DevicePath = devicePath.ToString(),
                Keys = ev.Changed.Keys.ToArray(),
                Address = ev.Changed.TryGetValue("Address", out var a) ? a as string : null,
                Name = ev.Changed.TryGetValue("Name", out var n) ? n as string : null,
                Alias = ev.Changed.TryGetValue("Alias", out var al) ? al as string : null,
                Rssi = TryGetInt16(ev.Changed, "RSSI"),
                ManufacturerData = md
            });
        });
        if (!devicePropertySubscriptions.TryAdd(devicePath, sub))
            sub.Dispose();
    }

    public async Task StopAsync()
    {
        keepAliveCts?.Cancel();
        if (keepAliveTask is not null)
        {
            try { await keepAliveTask; } catch { }
        }
        keepAliveTask = null;
        keepAliveCts?.Dispose();
        keepAliveCts = null;
        try
        {
            Debug?.Invoke("[DBG] Calling StopDiscovery...");
            await adapter.StopDiscoveryAsync();
            Debug?.Invoke("[DBG] StopDiscovery OK");
        }
        catch (Exception ex)
        {
            Debug?.Invoke("[DBG] StopDiscovery error: " + ex.Message);
        }
        adapterPropertySubscriptions?.Dispose(); adapterPropertySubscriptions = null;
        addedSubscription?.Dispose(); addedSubscription = null;
        removedSubscription?.Dispose(); removedSubscription = null;
        foreach (var kv in devicePropertySubscriptions)
            kv.Value.Dispose();
        devicePropertySubscriptions.Clear();
    }

    private static string? TryGetString(IDictionary<string, object>? props, string key)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (props == null || !props.TryGetValue(key, out var value) || (value is null))
        {
            return null;
        }

        return value as string;
    }

    private static short? TryGetInt16(IDictionary<string, object>? props, string key)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (props == null || !props.TryGetValue(key, out var value) || (value is null))
        {
            return null;
        }

        return value switch
        {
            short s => s,
            int i => (short)i,
            long l => (short)l,
            _ => null
        };
    }

    private static IReadOnlyDictionary<ushort, byte[]>? TryGetManufacturerData(IDictionary<string, object> props)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (!props.TryGetValue("ManufacturerData", out var value) || (value is null))
        {
            return null;
        }

        if (value is IDictionary<ushort, byte[]> direct)
        {
            return new Dictionary<ushort, byte[]>(direct);
        }

        if (value is IDictionary<ushort, object> objectDictionary)
        {
            var res = new Dictionary<ushort, byte[]>();
            foreach (var (key, obj) in objectDictionary)
            {
                if (obj is byte[] bytes)
                {
                    res[key] = bytes;
                }
                else if (obj is IEnumerable<byte> eb)
                {
                    res[key] = eb.ToArray();
                }
            }
            return res;
        }

        return null;
    }
}
