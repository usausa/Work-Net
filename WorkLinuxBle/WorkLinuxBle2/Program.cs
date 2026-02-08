using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

#region ---- Low level D-Bus interfaces (hidden behind wrapper classes) ----

[DBusInterface("org.freedesktop.DBus.ObjectManager")]
public interface IObjectManager : IDBusObject
{
    Task<IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>>> GetManagedObjectsAsync();
    Task<IDisposable> WatchInterfacesAddedAsync(
        Action<(ObjectPath objectPath, IDictionary<string, IDictionary<string, object>> interfaces)> handler);
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

// Signal subscription only (avoid Get/Set signature issues across Tmds.DBus versions)
[DBusInterface("org.freedesktop.DBus.Properties")]
public interface IProperties : IDBusObject
{
    Task<IDisposable> WatchPropertiesChangedAsync(
        Action<(string iface, IDictionary<string, object> changed, string[] invalidated)> handler);
}

#endregion

#region ---- Windows-like BLE model (public API for app layer) ----

public sealed class BlueZBluetoothLEDevice : IAsyncDisposable
{
    private readonly Connection _conn;
    private readonly IObjectManager _objMgr;
    private readonly IAdapter1 _adapter;
    private readonly ObjectPath _adapterPath;

    private readonly ObjectPath _devicePath;
    private readonly IDevice1 _device;

    private bool _connected;

    private BlueZBluetoothLEDevice(Connection conn, IObjectManager objMgr, IAdapter1 adapter, ObjectPath adapterPath, ObjectPath devicePath)
    {
        _conn = conn;
        _objMgr = objMgr;
        _adapter = adapter;
        _adapterPath = adapterPath;
        _devicePath = devicePath;
        _device = _conn.CreateProxy<IDevice1>("org.bluez", _devicePath);
    }

    public string DevicePath => _devicePath.ToString();

    public static async Task<BlueZBluetoothLEDevice> FromAddressAsync(string address, CancellationToken ct)
    {
        var normalized = BleUtil.NormalizeAddress(address);

        var conn = new Connection(Address.System);
        await conn.ConnectAsync();

        var objMgr = conn.CreateProxy<IObjectManager>("org.bluez", new ObjectPath("/"));
        var objects = await objMgr.GetManagedObjectsAsync();

        var adapterPath = objects.Keys.FirstOrDefault(p => objects[p].ContainsKey("org.bluez.Adapter1"));
        if (adapterPath == default)
            throw new InvalidOperationException("Bluetooth adapter (org.bluez.Adapter1) not found.");

        var adapter = conn.CreateProxy<IAdapter1>("org.bluez", adapterPath);

        // event-based discovery (InterfacesAdded) + initial cache check
        var devicePath = FindDeviceByAddress(objects, normalized);
        if (devicePath == default)
            devicePath = await DiscoverDeviceByAddressAsync(conn, objMgr, adapter, normalized, TimeSpan.FromSeconds(10), ct);

        return new BlueZBluetoothLEDevice(conn, objMgr, adapter, adapterPath, devicePath);
    }

    public async Task ConnectAsync()
    {
        if (_connected) return;
        await _device.ConnectAsync();
        _connected = true;
    }

    public async Task DisconnectAsync()
    {
        if (!_connected) return;
        try { await _device.DisconnectAsync(); } catch { }
        _connected = false;
    }

    public async Task<IReadOnlyList<BlueZGattDeviceService>> GetGattServicesAsync()
    {
        // BlueZでは service/char は ObjectManager で列挙するのが基本。
        // ここでは "device配下のGattService1" を拾って返す。
        var objects = await _objMgr.GetManagedObjectsAsync();
        var list = new List<BlueZGattDeviceService>();

        foreach (var kv in objects)
        {
            var path = kv.Key.ToString();
            if (!path.StartsWith(_devicePath.ToString(), StringComparison.Ordinal))
                continue;

            if (!kv.Value.TryGetValue("org.bluez.GattService1", out var props))
                continue;

            // UUID property
            if (!props.TryGetValue("UUID", out var uuidObj) || uuidObj is not string uuid)
                uuid = "";

            list.Add(new BlueZGattDeviceService(_conn, _objMgr, _devicePath, kv.Key, uuid));
        }

        return list;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
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

    private static async Task<ObjectPath> DiscoverDeviceByAddressAsync(
        Connection conn, IObjectManager objMgr, IAdapter1 adapter,
        string targetAddress, TimeSpan timeout, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<ObjectPath>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);
        using var reg = timeoutCts.Token.Register(() => tcs.TrySetCanceled(timeoutCts.Token));

        var sub = await objMgr.WatchInterfacesAddedAsync(ev =>
        {
            try
            {
                if (!ev.interfaces.TryGetValue("org.bluez.Device1", out var props))
                    return;

                if (!props.TryGetValue("Address", out var addrObj) || addrObj is not string addr)
                    return;

                if (string.Equals(BleUtil.NormalizeAddress(addr), targetAddress, StringComparison.OrdinalIgnoreCase))
                    tcs.TrySetResult(ev.objectPath);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        try
        {
            try { await adapter.StartDiscoveryAsync(); }
            catch (DBusException ex) when (ex.ErrorName?.Contains("InProgress", StringComparison.OrdinalIgnoreCase) == true) { }

            return await tcs.Task;
        }
        finally
        {
            sub.Dispose();
            try { await adapter.StopDiscoveryAsync(); } catch { }
        }
    }
}

public sealed class BlueZGattDeviceService
{
    private readonly Connection _conn;
    private readonly IObjectManager _objMgr;
    private readonly ObjectPath _devicePath;

    internal BlueZGattDeviceService(Connection conn, IObjectManager objMgr, ObjectPath devicePath, ObjectPath servicePath, string uuid)
    {
        _conn = conn;
        _objMgr = objMgr;
        _devicePath = devicePath;
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

            // typical layout: .../serviceXXXX/charYYYY so servicePath prefix is enough
            if (!path.StartsWith(ObjectPath.ToString(), StringComparison.Ordinal))
                continue;

            if (!kv.Value.TryGetValue("org.bluez.GattCharacteristic1", out var props))
                continue;

            if (!props.TryGetValue("UUID", out var uuidObj) || uuidObj is not string uuid)
                uuid = "";

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

    internal BlueZGattCharacteristic(Connection conn, ObjectPath charPath, string uuid)
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
        // 1) StartNotify
        await _ch.StartNotifyAsync();

        // 2) Subscribe PropertiesChanged(Value)
        _sub?.Dispose();
        _sub = await _props.WatchPropertiesChangedAsync(ev =>
        {
            if (!string.Equals(ev.iface, "org.bluez.GattCharacteristic1", StringComparison.Ordinal))
                return;

            if (!ev.changed.TryGetValue("Value", out var v) || v is null)
                return;

            // Valueの型が環境によってbyte[]以外になる場合があるので、最低限のフォールバック
            byte[]? bytes = v as byte[];
            if (bytes is null && v is IEnumerable<byte> eb)
                bytes = eb.ToArray();

            if (bytes is null || bytes.Length == 0)
                return;

            ValueChanged?.Invoke(this, bytes);
        });
    }

    public async Task StopNotifyAsync()
    {
        _sub?.Dispose();
        _sub = null;
        try { await _ch.StopNotifyAsync(); } catch { }
    }

    public Task WriteValueAsync(byte[] data)
        => _ch.WriteValueAsync(data, new Dictionary<string, object>());

    public async ValueTask DisposeAsync()
    {
        await StopNotifyAsync();
    }
}

#endregion

#region ---- App layer (Windows-like style) ----

internal static class Program
{
    private static readonly Guid ServiceUuid = Guid.Parse("6E400001-B5A3-F393-E0A9-E50E24DCCA9E");
    private static readonly Guid RxCharUuid = Guid.Parse("6E400002-B5A3-F393-E0A9-E50E24DCCA9E");
    private static readonly Guid TxCharUuid = Guid.Parse("6E400003-B5A3-F393-E0A9-E50E24DCCA9E");

    private static readonly SemaphoreSlim NotificationSemaphore = new(0);
    private static BlueZBluetoothLEDevice? device;
    private static BlueZGattDeviceService? service;
    private static BlueZGattCharacteristic? rxChar;
    private static BlueZGattCharacteristic? txChar;

    private static string lastNotifyData = "";
    private static bool waitingForResponse;

    static async Task<int> Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: ./WorkLinuxBle <BLE_ADDRESS>");
            return 1;
        }

        var address = BleUtil.NormalizeAddress(args[0]);

        Console.WriteLine("BLE ATOM Control Shell");
        Console.WriteLine("======================");
        Console.WriteLine($"Target device: {address}");

        while (true)
        {
            BleUtil.PrintMenu();
            Console.Write("Select> ");
            var input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;

            try
            {
                switch (input)
                {
                    case "1":
                        await ConnectAsync(address);
                        break;
                    case "2":
                        await DisconnectAsync();
                        break;
                    case "3":
                        await SetLedPresetAsync();
                        break;
                    case "4":
                        await SetLedRgbAsync();
                        break;
                    case "5":
                        await GetTemperatureAsync();
                        break;
                    case "6":
                        await DisconnectAsync();
                        Console.WriteLine("Goodbye!");
                        return 0;
                    default:
                        Console.WriteLine("Invalid command.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private static async Task ConnectAsync(string address)
    {
        if (device != null)
        {
            Console.WriteLine("Already connected. Disconnect first.");
            return;
        }

        Console.WriteLine("Connecting...");
        device = await BlueZBluetoothLEDevice.FromAddressAsync(address, CancellationToken.None);
        await device.ConnectAsync();
        Console.WriteLine($"Connected: {device.DevicePath}");

        // Find UART service
        var services = await device.GetGattServicesAsync();
        service = services.FirstOrDefault(s => s.Uuid == ServiceUuid);
        if (service == null)
        {
            Console.WriteLine("UART service not found.");
            await DisconnectAsync();
            return;
        }

        // Find characteristics
        var chars = await service.GetCharacteristicsAsync();
        rxChar = chars.FirstOrDefault(c => c.Uuid == RxCharUuid);
        txChar = chars.FirstOrDefault(c => c.Uuid == TxCharUuid);

        if (rxChar == null || txChar == null)
        {
            Console.WriteLine("RX/TX characteristic not found.");
            await DisconnectAsync();
            return;
        }

        // Subscribe notifications
        txChar.ValueChanged += TxCharOnValueChanged;
        await txChar.StartNotifyAsync();
        Console.WriteLine("Notifications enabled.");
        Console.WriteLine("Connection established successfully!");
    }

    private static void TxCharOnValueChanged(object? sender, byte[] bytes)
    {
        lastNotifyData = Encoding.ASCII.GetString(bytes);

        if (waitingForResponse)
        {
            if (NotificationSemaphore.CurrentCount == 0)
                NotificationSemaphore.Release();
        }
        else
        {
            Console.WriteLine($"\n[Notification] {lastNotifyData.TrimEnd()}");
            Console.Write("Select> ");
        }
    }

    private static async Task DisconnectAsync()
    {
        if (txChar != null)
        {
            txChar.ValueChanged -= TxCharOnValueChanged;
            await txChar.DisposeAsync();
            txChar = null;
        }

        if (rxChar != null)
        {
            await rxChar.DisposeAsync();
            rxChar = null;
        }

        service = null;

        if (device != null)
        {
            await device.DisposeAsync();
            device = null;
            Console.WriteLine("Disconnected.");
        }
        else
        {
            Console.WriteLine("Not connected.");
        }
    }

    private static Task EnsureConnectedAsync()
    {
        if (device == null || rxChar == null)
            throw new InvalidOperationException("Not connected. Please connect first (command 1).");
        return Task.CompletedTask;
    }

    private static async Task SendCommandAsync(string command)
    {
        await EnsureConnectedAsync();

        var cmd = command.EndsWith("\n", StringComparison.Ordinal) ? command : command + "\n";
        var bytes = Encoding.ASCII.GetBytes(cmd);

        await rxChar!.WriteValueAsync(bytes);
        Console.WriteLine($"Command sent: {cmd.TrimEnd()}");
    }

    private static async Task SetLedPresetAsync()
    {
        Console.WriteLine("Available colors: RED, GREEN, BLUE, WHITE, OFF");
        Console.Write("Enter color: ");
        var color = Console.ReadLine()?.Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(color) || (color is not ("RED" or "GREEN" or "BLUE" or "WHITE" or "OFF")))
        {
            Console.WriteLine("Invalid color.");
            return;
        }
        await SendCommandAsync(color);
    }

    private static async Task SetLedRgbAsync()
    {
        int r = BleUtil.ReadInt("Enter R (0-255): ", 0, 255);
        int g = BleUtil.ReadInt("Enter G (0-255): ", 0, 255);
        int b = BleUtil.ReadInt("Enter B (0-255): ", 0, 255);
        await SendCommandAsync($"RGB {r} {g} {b}");
    }

    private static async Task GetTemperatureAsync()
    {
        await EnsureConnectedAsync();

        while (NotificationSemaphore.CurrentCount > 0)
            await NotificationSemaphore.WaitAsync(0);

        lastNotifyData = "";
        waitingForResponse = true;

        await SendCommandAsync("TEMP");
        Console.WriteLine("Waiting for response...");

        var timeoutTask = Task.Delay(5000);
        var notificationTask = NotificationSemaphore.WaitAsync();
        var completed = await Task.WhenAny(notificationTask, timeoutTask);

        waitingForResponse = false;

        if (completed == notificationTask)
            Console.WriteLine($"Result: {lastNotifyData.TrimEnd()}");
        else
            Console.WriteLine("Timeout: No response received. Check connection.");
    }
}

#endregion

#region ---- Utilities ----

internal static class BleUtil
{
    public static void PrintMenu()
    {
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  1. Connect");
        Console.WriteLine("  2. Disconnect");
        Console.WriteLine("  3. Set LED (RED/GREEN/BLUE/WHITE/OFF)");
        Console.WriteLine("  4. Set LED (RGB)");
        Console.WriteLine("  5. Get Temperature");
        Console.WriteLine("  6. Exit");
    }

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

    public static int ReadInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write(prompt);
            var line = Console.ReadLine();
            if (int.TryParse(line, out var v) && v >= min && v <= max) return v;
            Console.WriteLine("Invalid value.");
        }
    }
}

#endregion
