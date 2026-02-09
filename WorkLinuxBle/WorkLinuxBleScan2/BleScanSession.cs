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

    public BleDiagnostics? Diagnostics { get; set; }

    private readonly Connection connection;
    private readonly IObjectManager objectManager;
    private readonly IAdapter1 adapter;

    private IDisposable? addedSubscription;
    private IDisposable? removedSubscription;

    private readonly ConcurrentDictionary<ObjectPath, IDisposable> devicePropertySubscriptions = new();

    private volatile bool discovering;

    private BleScanSession(Connection connection, IObjectManager objectManager, IAdapter1 adapter)
    {
        this.connection = connection;
        this.objectManager = objectManager;
        this.adapter = adapter;
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

        return new BleScanSession(con, manager, adapter);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        connection.Dispose();
    }

    private void RaiseEvent(BleScanEvent e) => DeviceEvent?.Invoke(e);

    private void LogInfo(string message)
    {
        Diagnostics?.Info(message);
        Debug?.Invoke(message);
    }

    //private void LogWarn(string message)
    //{
    //    Diagnostics?.Warn(message);
    //    Debug?.Invoke(message);
    //}

    private void LogException(Exception ex, string context)
    {
        Diagnostics?.Exception(ex, context);
        Debug?.Invoke($"[EX] {context}: {ex.GetType().Name}: {ex.Message}");
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (discovering)
        {
            return;
        }

        // Device added subscription
        addedSubscription = await objectManager.WatchInterfacesAddedAsync(ev =>
        {
            try
            {
                LogInfo($"[SUB] InterfacesAdded: path={ev.ObjectPath}, interfaces=[{string.Join(",", ev.Interfaces.Keys)}]");

                if (!ev.Interfaces.TryGetValue("org.bluez.Device1", out var props))
                {
                    return;
                }

                RaiseEvent(new BleScanEvent
                {
                    Timestamp = DateTimeOffset.Now,
                    Type = BleScanEventType.Discover,
                    DevicePath = ev.ObjectPath.ToString(),
                    Keys = props.Keys.ToArray(),
                    Address = TryGetString(props, "Address"),
                    Name = TryGetString(props, "Name"),
                    Alias = TryGetString(props, "Alias"),
                    Rssi = TryGetInt16(props, "RSSI")
                });

                _ = SubscribeDevicePropertyAsync(ev.ObjectPath);
            }
            catch (Exception ex)
            {
                LogException(ex, $"InterfacesAdded handler ({ev.ObjectPath})");
            }
        });
        // Device removed subscription
        removedSubscription = await objectManager.WatchInterfacesRemovedAsync(ev =>
        {
            try
            {
                LogInfo($"[SUB] InterfacesRemoved: path={ev.ObjectPath}, interfaces=[{string.Join(",", ev.Interfaces)}]");

                if (devicePropertySubscriptions.TryRemove(ev.ObjectPath, out var subscription))
                {
                    subscription.Dispose();
                }

                RaiseEvent(new BleScanEvent
                {
                    Timestamp = DateTimeOffset.Now,
                    Type = BleScanEventType.Lost,
                    DevicePath = ev.ObjectPath.ToString(),
                    Keys = ev.Interfaces.ToArray()
                });
            }
            catch (Exception ex)
            {
                LogException(ex, $"InterfacesRemoved handler ({ev.ObjectPath})");
            }
        });

        var objects = await objectManager.GetManagedObjectsAsync();
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
                DevicePath = key.ToString(),
                Keys = props.Keys.ToArray(),
                Address = TryGetString(props, "Address"),
                Name = TryGetString(props, "Name"),
                Alias = TryGetString(props, "Alias"),
                Rssi = TryGetInt16(props, "RSSI")
            });

            _ = SubscribeDevicePropertyAsync(key);
        }

        await adapter.StartDiscoveryAsync();

        discovering = true;
    }

    private async Task SubscribeDevicePropertyAsync(ObjectPath devicePath)
    {
        if (devicePropertySubscriptions.ContainsKey(devicePath))
        {
            return;
        }

        var properties = connection.CreateProxy<IProperties>("org.bluez", devicePath);
        var subscription = await properties.WatchPropertiesChangedAsync(ev =>
        {
            try
            {
                LogInfo($"[SUB] Device PropertiesChanged: path={devicePath}, interface={ev.Interface}, changedKeys=[{string.Join(",", ev.Changed.Keys)}], invalidatedKeys=[{string.Join(",", ev.Invalidated)}]");

                if (!String.Equals(ev.Interface, "org.bluez.Device1", StringComparison.Ordinal))
                {
                    return;
                }

                if (ev.Changed.Count == 0)
                {
                    return;
                }

                var props = ev.Changed;
                var md = TryGetManufacturerData(props);
                RaiseEvent(new BleScanEvent
                {
                    Timestamp = DateTimeOffset.Now,
                    Type = BleScanEventType.Update,
                    DevicePath = devicePath.ToString(),
                    Keys = props.Keys.ToArray(),
                    Address = TryGetString(props, "Address"),
                    Name = TryGetString(props, "Name"),
                    Alias = TryGetString(props, "Alias"),
                    Rssi = TryGetInt16(props, "RSSI"),
                    ManufacturerData = md
                });
            }
            catch (Exception ex)
            {
                LogException(ex, $"Device PropertiesChanged handler ({devicePath})");
            }
        });

        if (!devicePropertySubscriptions.TryAdd(devicePath, subscription))
        {
            subscription.Dispose();
        }
    }

    public async Task StopAsync()
    {
        if (!discovering)
        {
            return;
        }

        try
        {
            Debug?.Invoke("[DBG] Calling StopDiscovery...");
            await adapter.StopDiscoveryAsync();
            Debug?.Invoke("[DBG] StopDiscovery OK");
        }
        catch (Exception ex)
        {
            // Ignore
            Debug?.Invoke("[DBG] StopDiscovery error: " + ex.Message);
        }

        addedSubscription?.Dispose();
        addedSubscription = null;

        removedSubscription?.Dispose();
        removedSubscription = null;

        foreach (var (_, value) in devicePropertySubscriptions)
        {
            value.Dispose();
        }
        devicePropertySubscriptions.Clear();

        discovering = false;
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
