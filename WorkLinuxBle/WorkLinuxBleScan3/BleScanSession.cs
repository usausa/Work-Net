using System.Collections.Concurrent;
using Tmds.DBus;

namespace WorkLinuxBleScan3;

public enum BleScanEventType
{
    Discover,
    Lost,
    Update
}

public sealed class BleScanEvent
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    public BleScanEventType Type { get; init; }

    public string Source { get; init; } = string.Empty;

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
    public event Action<BleScanEvent>? DeviceEvent;

    private readonly Connection con;
    private readonly IObjectManager manager;
    private readonly IAdapter1 adapter;
    private readonly IProperties adapterProperties;

    private IDisposable? addedSubscription;
    private IDisposable? removedSubscription;
    private IDisposable? adapterPropertySubscription;
    private readonly ConcurrentDictionary<ObjectPath, IDisposable> devicePropertySubscriptions = new();

    private CancellationTokenSource? keepAliveCts;
    private Task? keepAliveTask;

    private volatile bool discovering;

    private BleScanSession(Connection con, IObjectManager manager, IAdapter1 adapter, IProperties adapterProperties)
    {
        this.con = con;
        this.manager = manager;
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
        con.Dispose();
    }

    private void RaiseEvent(BleScanEvent e) => DeviceEvent?.Invoke(e);



    public async Task StopAsync()
    {
        await keepAliveCts?.CancelAsync()!;

        if (keepAliveTask is not null)
        {
            try
            {
                await keepAliveTask;
            }
            catch
            {
                // Ignore
            }
        }
        keepAliveTask = null;
        keepAliveCts?.Dispose();
        keepAliveCts = null;

        try
        {
            await adapter.StopDiscoveryAsync();
        }
        catch
        {
            // Ignore
        }

        adapterPropertySubscription?.Dispose();
        adapterPropertySubscription = null;

        addedSubscription?.Dispose();
        addedSubscription = null;

        removedSubscription?.Dispose();
        removedSubscription = null;

        foreach (var (_, value) in devicePropertySubscriptions)
        {
            value.Dispose();
        }
        devicePropertySubscriptions.Clear();
    }

    public async Task StartAsync(CancellationToken token)
    {
        // Discover changed subscription
        adapterPropertySubscription = await adapterProperties.WatchPropertiesChangedAsync(ev =>
        {
            if (!String.Equals(ev.Interface, "org.bluez.Adapter1", StringComparison.Ordinal))
            {
                return;
            }

            if (ev.Changed.TryGetValue("Discovering", out var value) && (value is bool boolValue))
            {
                discovering = boolValue;
            }
        });
        // Device added subscription
        addedSubscription = await manager.WatchInterfacesAddedAsync(ev =>
        {
            if (!ev.Interfaces.TryGetValue("org.bluez.Device1", out var props))
            {
                return;
            }

            RaiseEvent(new BleScanEvent
            {
                Timestamp = DateTimeOffset.Now,
                Type = BleScanEventType.Discover,
                Source = "InterfacesAdded",
                DevicePath = ev.ObjectPath.ToString(),
                Keys = props.Keys.ToArray(),
                Address = props.TryGetValue("Address", out var address) ? address as string : null,
                Name = props.TryGetValue("Name", out var name) ? name as string : null,
                Alias = props.TryGetValue("Alias", out var alias) ? alias as string : null,
                Rssi = TryGetInt16(props, "RSSI")
            });

            _ = SubscribeDevicePropertyAsync(ev.ObjectPath);
        });
        // Device removed subscription
        removedSubscription = await manager.WatchInterfacesRemovedAsync(ev =>
        {
            if (devicePropertySubscriptions.TryRemove(ev.ObjectPath, out var subscription))
            {
                subscription.Dispose();
            }

            RaiseEvent(new BleScanEvent
            {
                Timestamp = DateTimeOffset.Now,
                Type = BleScanEventType.Lost,
                Source = "InterfacesRemoved",
                DevicePath = ev.ObjectPath.ToString(),
                Keys = ev.Interfaces.ToArray()
            });
        });

        var objects = await manager.GetManagedObjectsAsync();
        foreach (var (key, value) in objects)
        {
            if (!value.TryGetValue("org.bluez.Device1", out var props))
            {
                continue;
            }

            RaiseEvent(new BleScanEvent
            {
                Timestamp = DateTimeOffset.Now,
                Type = BleScanEventType.Discover,
                Source = "InitialDump",
                DevicePath = key.ToString(),
                Keys = props.Keys.ToArray(),
                Address = TryGetString(props, "Address"),
                Name = TryGetString(props, "Name"),
                Alias = TryGetString(props, "Alias"),
                Rssi = TryGetInt16(props, "RSSI")
            });

            _ = SubscribeDevicePropertyAsync(key);
        }

        await StartDiscoverySafeAsync();

        keepAliveCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        // ReSharper disable once MethodSupportsCancellation
        keepAliveTask = Task.Run(() => KeepAliveLoopAsync(keepAliveCts.Token));
    }

    private async Task SubscribeDevicePropertyAsync(ObjectPath devicePath)
    {
        if (devicePropertySubscriptions.ContainsKey(devicePath))
        {
            return;
        }

        var propsProxy = con.CreateProxy<IProperties>("org.bluez", devicePath);
        var subscription = await propsProxy.WatchPropertiesChangedAsync(ev =>
        {
            if (!string.Equals(ev.Interface, "org.bluez.Device1", StringComparison.Ordinal))
            {
                return;
            }

            if (ev.Changed.Count == 0)
            {
                return;
            }

            var props = ev.Changed;
            RaiseEvent(new BleScanEvent
            {
                Timestamp = DateTimeOffset.Now,
                Type = BleScanEventType.Update,
                Source = "PropertiesChanged",
                DevicePath = devicePath.ToString(),
                Keys = props.Keys.ToArray(),
                Address = TryGetString(props, "Address"),
                Name = TryGetString(props, "Name"),
                Alias = TryGetString(props, "Alias"),
                Rssi = TryGetInt16(props, "RSSI"),
                ManufacturerData = TryGetManufacturerData(props)
            });
        });

        if (!devicePropertySubscriptions.TryAdd(devicePath, subscription))
        {
            subscription.Dispose();
        }
    }

    private async Task KeepAliveLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!discovering)
                {
                    await StartDiscoverySafeAsync();
                }
            }
            catch
            {
                // Ignore
            }

            // Wait
            try
            {
                await Task.Delay(1000, token);
            }
            catch
            {
                // Ignore
            }
        }
    }

    private async Task StartDiscoverySafeAsync()
    {
        try
        {
            await adapter.StartDiscoveryAsync();
            discovering = true;
        }
        catch (DBusException ex) when (ex.ErrorName?.Contains("InProgress", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Already discovering
            discovering = true;
        }
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
