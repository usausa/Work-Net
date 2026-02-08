// Program.cs
// NuGet: Tmds.DBus
//
// dotnet new console -n WorkLinuxBle
// dotnet add package Tmds.DBus
//
// Run:
//   dotnet run -- F0:24:F9:BC:48:F6
//
// This version is compatible with older Tmds.DBus versions that don't have Connection.WatchSignalAsync.
// It subscribes to notifications via org.freedesktop.DBus.Properties.PropertiesChanged using a proxy
// interface that ONLY declares WatchPropertiesChangedAsync (no Get/Set), avoiding signature mismatch issues.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

#region BlueZ D-Bus interfaces (public)

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

// IMPORTANT: only signal subscription is declared.
// Do NOT declare Get/Set/GetAll because older Tmds.DBus may enforce very specific signatures.
[DBusInterface("org.freedesktop.DBus.Properties")]
public interface IProperties : IDBusObject
{
    Task<IDisposable> WatchPropertiesChangedAsync(
        Action<(string iface, IDictionary<string, object> changed, string[] invalidated)> handler);
}

#endregion

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: ./WorkLinuxBle <BLE_ADDRESS>");
            Console.Error.WriteLine("Example: ./WorkLinuxBle AA:BB:CC:DD:EE:FF");
            return 1;
        }

        var targetAddress = BleUtil.NormalizeAddress(args[0]);

        Console.WriteLine("BLE ATOM Control Shell (.NET)");
        Console.WriteLine("============================");
        Console.WriteLine($"Target device: {targetAddress}");

        using var appCts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; appCts.Cancel(); };

        await using var shell = new BleAtomShell();

        while (!appCts.IsCancellationRequested)
        {
            BleUtil.PrintMenu();
            Console.Write("Select> ");
            var line = Console.ReadLine();
            if (line is null) break;

            if (!int.TryParse(line.Trim(), out int choice))
            {
                Console.WriteLine("Invalid input.");
                continue;
            }

            try
            {
                switch (choice)
                {
                    case 1:
                        await shell.ConnectAsync(targetAddress, appCts.Token);
                        break;
                    case 2:
                        await shell.DisconnectAsync();
                        break;
                    case 3:
                        await shell.SetLedPresetAsync();
                        break;
                    case 4:
                        await shell.SetLedRgbAsync();
                        break;
                    case 5:
                        await shell.GetTemperatureAsync(appCts.Token);
                        break;
                    case 6:
                        await shell.DisconnectAsync();
                        Console.WriteLine("Goodbye!");
                        return 0;
                    default:
                        Console.WriteLine("Invalid command.");
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        return 0;
    }
}

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
            if (int.TryParse(line, out var v) && v >= min && v <= max)
                return v;

            Console.WriteLine("Invalid value.");
        }
    }
}

internal sealed class BleAtomShell : IAsyncDisposable
{
    // UUIDs
    private const string RX_CHAR_UUID = "6e400002-b5a3-f393-e0a9-e50e24dcca9e"; // Write
    private const string TX_CHAR_UUID = "6e400003-b5a3-f393-e0a9-e50e24dcca9e"; // Notify

    private Connection? _conn;
    private IObjectManager? _objMgr;

    private ObjectPath _adapterPath = default;
    private IAdapter1? _adapter;

    private ObjectPath _devicePath = default;
    private IDevice1? _device;

    private ObjectPath _rxCharPath = default;
    private ObjectPath _txCharPath = default;
    private IGattCharacteristic1? _rxChar;
    private IGattCharacteristic1? _txChar;

    private IProperties? _txProps;
    private IDisposable? _propsChangedSub;

    private volatile bool _connected;

    private readonly object _lock = new();
    private TaskCompletionSource<string>? _pendingResponseTcs;

    public async Task ConnectAsync(string targetAddress, CancellationToken ct)
    {
        if (_connected)
        {
            Console.WriteLine("Already connected. Disconnect first.");
            return;
        }

        await EnsureDbusAsync();

        Console.WriteLine($"Connecting to {targetAddress}...");

        _devicePath = await DiscoverDeviceByAddressAsync(targetAddress, TimeSpan.FromSeconds(10), ct);
        Console.WriteLine($"Found device at: {_devicePath}");

        _device = _conn!.CreateProxy<IDevice1>("org.bluez", _devicePath);

        Console.WriteLine("Connecting to device...");
        await _device.ConnectAsync();
        Console.WriteLine("Connected successfully!");

        (_rxCharPath, _txCharPath) = await WaitAndFindCharacteristicsAsync(
            _devicePath, RX_CHAR_UUID, TX_CHAR_UUID, timeout: TimeSpan.FromSeconds(8), ct);

        Console.WriteLine($"Found RX characteristic: {_rxCharPath}");
        Console.WriteLine($"Found TX characteristic: {_txCharPath}");

        _rxChar = _conn!.CreateProxy<IGattCharacteristic1>("org.bluez", _rxCharPath);
        _txChar = _conn!.CreateProxy<IGattCharacteristic1>("org.bluez", _txCharPath);

        // Enable notifications
        await _txChar.StartNotifyAsync();
        Console.WriteLine("Notifications enabled.");

        // Subscribe to PropertiesChanged on TX characteristic (Value updates)
        _txProps = _conn!.CreateProxy<IProperties>("org.bluez", _txCharPath);
        _propsChangedSub = await _txProps.WatchPropertiesChangedAsync(OnTxPropertiesChanged);

        _connected = true;
        Console.WriteLine("Connection established successfully!");
    }

    public async Task DisconnectAsync()
    {
        _propsChangedSub?.Dispose();
        _propsChangedSub = null;
        _txProps = null;

        lock (_lock)
        {
            _pendingResponseTcs?.TrySetCanceled();
            _pendingResponseTcs = null;
        }

        if (_txChar is not null)
        {
            try { await _txChar.StopNotifyAsync(); } catch { }
        }

        if (_device is not null)
        {
            try { await _device.DisconnectAsync(); } catch { }
        }

        _devicePath = default;
        _rxCharPath = default;
        _txCharPath = default;

        _device = null;
        _rxChar = null;
        _txChar = null;

        if (_connected)
            Console.WriteLine("Disconnected.");

        _connected = false;
    }

    public async Task SetLedPresetAsync()
    {
        Console.WriteLine("Available colors: RED, GREEN, BLUE, WHITE, OFF");
        Console.Write("Enter color: ");
        var color = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

        if (color is not ("RED" or "GREEN" or "BLUE" or "WHITE" or "OFF"))
        {
            Console.WriteLine("Invalid color.");
            return;
        }

        await SendCommandAsync(color);
    }

    public async Task SetLedRgbAsync()
    {
        int r = BleUtil.ReadInt("Enter R (0-255): ", 0, 255);
        int g = BleUtil.ReadInt("Enter G (0-255): ", 0, 255);
        int b = BleUtil.ReadInt("Enter B (0-255): ", 0, 255);

        await SendCommandAsync($"RGB {r} {g} {b}");
    }

    public async Task GetTemperatureAsync(CancellationToken ct)
    {
        if (!_connected)
        {
            Console.WriteLine("Not connected. Please connect first (command 1).");
            return;
        }

        Task<string> responseTask;
        lock (_lock)
        {
            _pendingResponseTcs?.TrySetCanceled();
            _pendingResponseTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            responseTask = _pendingResponseTcs.Task;
        }

        await SendCommandAsync("TEMP");
        Console.WriteLine("Waiting for response...");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            var result = await responseTask.WaitAsync(timeoutCts.Token);
            Console.WriteLine($"Result: {result}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Timeout: No response received. Check connection.");
        }
        finally
        {
            lock (_lock) { _pendingResponseTcs = null; }
        }
    }

    private async Task SendCommandAsync(string command)
    {
        if (!_connected || _rxChar is null)
        {
            Console.WriteLine("Not connected. Please connect first (command 1).");
            return;
        }

        var cmd = command + "\n";
        var bytes = Encoding.ASCII.GetBytes(cmd);

        await _rxChar.WriteValueAsync(bytes, new Dictionary<string, object>());
        Console.WriteLine($"Command sent: {command}");
    }

    private void OnTxPropertiesChanged((string iface, IDictionary<string, object> changed, string[] invalidated) ev)
    {
        if (!string.Equals(ev.iface, "org.bluez.GattCharacteristic1", StringComparison.Ordinal))
            return;

        // まずchangedキー一覧を出す（デバッグ）
        // Console.Error.WriteLine($"[DBG] PropertiesChanged keys: {string.Join(",", ev.changed.Keys)}");

        if (!ev.changed.TryGetValue("Value", out var v) || v is null)
            return;

        // デバッグ：Valueの実体型を確認
        // TEMPのとき通知が来ているなら、この行が出るはず
        Console.Error.WriteLine($"[DBG] Value runtime type: {v.GetType().FullName}");

        byte[]? bytes = null;

        // ケース1：素直にbyte[]
        if (v is byte[] b1)
        {
            bytes = b1;
        }
        else
        {
            // ケース2：Tmds.DBusのバージョンによっては variant ラッパを剥がす必要がある
            // 代表的なパターンをいくつか試す（失敗しても落とさない）
            // ※ここはあなたの環境の型名が分かれば確実に対応できます。
            try
            {
                // よくある：objectがIEnumerable<byte>として見えるケース
                if (v is IEnumerable<byte> eb)
                    bytes = eb.ToArray();
            }
            catch { /* ignore */ }

            if (bytes is null)
            {
                // 最終手段：ToString()を表示（どんな形で入っているか判断材料）
                Console.Error.WriteLine($"[DBG] Value.ToString(): {v}");
                return;
            }
        }

        if (bytes.Length == 0) return;

        // 文字列として扱う（C版に合わせASCII）
        var text = Encoding.ASCII.GetString(bytes).TrimEnd('\0', '\r', '\n');

        TaskCompletionSource<string>? tcs;
        lock (_lock) tcs = _pendingResponseTcs;

        if (tcs is null)
        {
            Console.WriteLine();
            Console.WriteLine($"[Notification] {text}");
            Console.Write("Select> ");
        }
        else
        {
            // TEMP応答として返す
            tcs.TrySetResult(text);
        }
    }
    private async Task EnsureDbusAsync()
    {
        if (_conn is not null) return;

        _conn = new Connection(Address.System);
        await _conn.ConnectAsync();

        _objMgr = _conn.CreateProxy<IObjectManager>("org.bluez", new ObjectPath("/"));

        var objects = await _objMgr.GetManagedObjectsAsync();
        _adapterPath = objects.Keys.FirstOrDefault(p => objects[p].ContainsKey("org.bluez.Adapter1"));
        if (_adapterPath == default)
            throw new InvalidOperationException("Bluetooth adapter (org.bluez.Adapter1) not found.");

        _adapter = _conn.CreateProxy<IAdapter1>("org.bluez", _adapterPath);
    }

    private async Task<ObjectPath> DiscoverDeviceByAddressAsync(string targetAddress, TimeSpan timeout, CancellationToken ct)
    {
        if (_objMgr is null || _adapter is null)
            throw new InvalidOperationException("Not initialized.");

        var objects = await _objMgr.GetManagedObjectsAsync();
        var existing = FindDeviceByAddress(objects, targetAddress);
        if (existing != default)
            return existing;

        var tcs = new TaskCompletionSource<ObjectPath>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);
        using var reg = timeoutCts.Token.Register(() => tcs.TrySetCanceled(timeoutCts.Token));

        var sub = await _objMgr.WatchInterfacesAddedAsync(ev =>
        {
            try
            {
                if (!ev.interfaces.TryGetValue("org.bluez.Device1", out var props))
                    return;

                if (!props.TryGetValue("Address", out var addrObj) || addrObj is not string addr)
                    return;

                var normalizedDev = BleUtil.NormalizeAddress(addr);
                if (string.Equals(normalizedDev, targetAddress, StringComparison.OrdinalIgnoreCase))
                    tcs.TrySetResult(ev.objectPath);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        try
        {
            try
            {
                await _adapter.StartDiscoveryAsync();
            }
            catch (DBusException ex) when (ex.ErrorName?.Contains("InProgress", StringComparison.OrdinalIgnoreCase) == true)
            {
                // ignore
            }

            return await tcs.Task;
        }
        finally
        {
            sub.Dispose();
            try { await _adapter.StopDiscoveryAsync(); } catch { }
        }
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

            var normalizedDev = BleUtil.NormalizeAddress(addr);
            if (string.Equals(normalizedDev, targetAddress, StringComparison.OrdinalIgnoreCase))
                return kv.Key;
        }
        return default;
    }

    private async Task<(ObjectPath rx, ObjectPath tx)> WaitAndFindCharacteristicsAsync(
        ObjectPath devicePath, string rxUuid, string txUuid, TimeSpan timeout, CancellationToken ct)
    {
        if (_objMgr is null) throw new InvalidOperationException("Not initialized.");

        var start = DateTime.UtcNow;
        ObjectPath rx = default, tx = default;

        while (DateTime.UtcNow - start < timeout)
        {
            ct.ThrowIfCancellationRequested();

            var objects = await _objMgr.GetManagedObjectsAsync();
            foreach (var kv in objects)
            {
                var path = kv.Key.ToString();
                if (!path.StartsWith(devicePath.ToString(), StringComparison.Ordinal))
                    continue;

                if (!kv.Value.TryGetValue("org.bluez.GattCharacteristic1", out var props))
                    continue;

                if (!props.TryGetValue("UUID", out var uuidObj) || uuidObj is not string uuid)
                    continue;

                if (uuid.Equals(rxUuid, StringComparison.OrdinalIgnoreCase))
                    rx = kv.Key;
                else if (uuid.Equals(txUuid, StringComparison.OrdinalIgnoreCase))
                    tx = kv.Key;
            }

            if (rx != default && tx != default)
                return (rx, tx);

            await Task.Delay(200, ct);
        }

        throw new InvalidOperationException("Failed to find characteristics (RX/TX).");
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        if (_conn is not null)
        {
            _conn.Dispose(); // older Tmds.DBus: no DisposeAsync
            _conn = null;
        }
    }
}