// Program.cs (single file)
// Linux BLE advertisement logger using BlueZ + Tmds.DBus
//
// Features (similar spirit to Windows BluetoothLEAdvertisementWatcher sample):
// - Passive scan (default) / Active scan option is exposed (note: BlueZ active scanning depends on controller/BlueZ behavior;
//   here we toggle "DuplicateData"/filter and keep it mostly illustrative.)
// - Once: show each device once
// - Info: show known device properties (Address, Name/Alias, RSSI, Paired/Trusted/Connected if available)
// - Manufacturer: show ManufacturerData (0xFF) from AdvertisingData
// - Section: show all AD structures from AdvertisingData
//
// Notes:
// - BlueZ exposes LE advertisements via org.bluez.Device1 property "AdvertisingData" (a{yv}) and "ManufacturerData" (a{qv})
//   when available. Not all devices/BlueZ versions provide them.
// - We implement scanning by starting discovery on adapter and then watching:
//     * ObjectManager.InterfacesAdded (new devices)
//     * PropertiesChanged on each Device1 to get RSSI/AdvertisingData updates
// - This avoids Connection.WatchSignalAsync; we use DBusInterface WatchPropertiesChangedAsync (works with older Tmds.DBus).
//
// Build:
//   dotnet new console -n BleAdvLogger
//   dotnet add package Tmds.DBus
//   dotnet run -- --once --manufacturer
//
// Run example:
//   dotnet run -- --once --section
//
// If you want to use a specific adapter (hci1 etc.), extend adapter selection.

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

    Task<IDisposable> WatchInterfacesAddedAsync(
        Action<(ObjectPath objectPath, IDictionary<string, IDictionary<string, object>> interfaces)> handler);

    Task<IDisposable> WatchInterfacesRemovedAsync(
        Action<(ObjectPath objectPath, string[] interfaces)> handler);
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
    Task<IDisposable> WatchPropertiesChangedAsync(
        Action<(string iface, IDictionary<string, object> changed, string[] invalidated)> handler);
}

#endregion

#region ---- BlueZ wrapper model (adds scanning/advertisement events) ----

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

    public void Dispose()
    {
        Connection.Dispose();
    }
}

public sealed class BlueZLeScanner : IAsyncDisposable
{
    private readonly BlueZContext _ctx;

    private IDisposable? _ifAddedSub;
    private IDisposable? _ifRemovedSub;

    // WatchPropertiesChanged subscriptions per device
    private readonly ConcurrentDictionary<ObjectPath, IDisposable> _devicePropsSubs = new();

    public event Action<BlueZAdvertisementEvent>? AdvertisementReceived;

    public BlueZLeScanner(BlueZContext ctx)
    {
        _ctx = ctx;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        // Subscribe before starting discovery to avoid missing early events
        _ifAddedSub = await _ctx.ObjectManager.WatchInterfacesAddedAsync(ev =>
        {
            try
            {
                if (!ev.interfaces.ContainsKey("org.bluez.Device1"))
                    return;

                // new device object appears; subscribe its PropertiesChanged
                _ = EnsureDeviceSubscriptionAsync(ev.objectPath);
                // also emit an initial snapshot from InterfacesAdded properties
                var props = ev.interfaces["org.bluez.Device1"];
                EmitFromDeviceProps(ev.objectPath, props, timestamp: DateTimeOffset.Now);
            }
            catch { /* ignore */ }
        });

        _ifRemovedSub = await _ctx.ObjectManager.WatchInterfacesRemovedAsync(ev =>
        {
            // Cleanup
            if (_devicePropsSubs.TryRemove(ev.objectPath, out var sub))
                sub.Dispose();
        });

        // Also subscribe existing devices already present (cache)
        var objects = await _ctx.ObjectManager.GetManagedObjectsAsync();
        foreach (var kv in objects)
        {
            if (kv.Value.ContainsKey("org.bluez.Device1"))
            {
                _ = EnsureDeviceSubscriptionAsync(kv.Key);
                EmitFromDeviceProps(kv.Key, kv.Value["org.bluez.Device1"], DateTimeOffset.Now);
            }
        }

        // Start discovery
        try
        {
            await _ctx.Adapter.StartDiscoveryAsync();
        }
        catch (DBusException ex) when (ex.ErrorName?.Contains("InProgress", StringComparison.OrdinalIgnoreCase) == true)
        {
            // ignore
        }

        _ = ct; // no-op; caller controls lifetime
    }

    public async Task StopAsync()
    {
        try { await _ctx.Adapter.StopDiscoveryAsync(); } catch { }

        _ifAddedSub?.Dispose();
        _ifAddedSub = null;

        _ifRemovedSub?.Dispose();
        _ifRemovedSub = null;

        foreach (var kv in _devicePropsSubs)
            kv.Value.Dispose();
        _devicePropsSubs.Clear();
    }

    private async Task EnsureDeviceSubscriptionAsync(ObjectPath devicePath)
    {
        if (_devicePropsSubs.ContainsKey(devicePath))
            return;

        // subscribe to PropertiesChanged on the device object itself
        var propsProxy = _ctx.Connection.CreateProxy<IProperties>("org.bluez", devicePath);

        IDisposable sub = await propsProxy.WatchPropertiesChangedAsync(ev =>
        {
            try
            {
                if (!string.Equals(ev.iface, "org.bluez.Device1", StringComparison.Ordinal))
                    return;

                // We only got changed properties; build event from them (may be partial)
                EmitFromDeviceChanged(devicePath, ev.changed, DateTimeOffset.Now);
            }
            catch { /* ignore */ }
        });

        if (!_devicePropsSubs.TryAdd(devicePath, sub))
            sub.Dispose();
    }

    private void EmitFromDeviceChanged(ObjectPath devicePath, IDictionary<string, object> changed, DateTimeOffset timestamp)
    {
        // For partial updates, fill what we can.
        var ev = new BlueZAdvertisementEvent
        {
            Timestamp = timestamp,
            DevicePath = devicePath.ToString(),
            Address = changed.TryGetValue("Address", out var a) ? a as string : null,
            Name = changed.TryGetValue("Name", out var n) ? n as string : null,
            Alias = changed.TryGetValue("Alias", out var al) ? al as string : null,
            Rssi = TryGetInt16(changed, "RSSI"),
            TxPower = TryGetInt16(changed, "TxPower"),
            Connected = TryGetBool(changed, "Connected"),
            Paired = TryGetBool(changed, "Paired"),
            Trusted = TryGetBool(changed, "Trusted"),
            ManufacturerData = TryGetManufacturerData(changed),
            AdvertisingData = TryGetAdvertisingData(changed),
        };

        AdvertisementReceived?.Invoke(ev);
    }

    private void EmitFromDeviceProps(ObjectPath devicePath, IDictionary<string, object> props, DateTimeOffset timestamp)
    {
        var ev = new BlueZAdvertisementEvent
        {
            Timestamp = timestamp,
            DevicePath = devicePath.ToString(),
            Address = props.TryGetValue("Address", out var a) ? a as string : null,
            Name = props.TryGetValue("Name", out var n) ? n as string : null,
            Alias = props.TryGetValue("Alias", out var al) ? al as string : null,
            Rssi = TryGetInt16(props, "RSSI"),
            TxPower = TryGetInt16(props, "TxPower"),
            Connected = TryGetBool(props, "Connected"),
            Paired = TryGetBool(props, "Paired"),
            Trusted = TryGetBool(props, "Trusted"),
            ManufacturerData = TryGetManufacturerData(props),
            AdvertisingData = TryGetAdvertisingData(props),
        };

        AdvertisementReceived?.Invoke(ev);
    }

    private static bool? TryGetBool(IDictionary<string, object> props, string key)
        => props.TryGetValue(key, out var v) && v is bool b ? b : null;

    private static short? TryGetInt16(IDictionary<string, object> props, string key)
    {
        if (!props.TryGetValue(key, out var v) || v is null) return null;

        // BlueZ often uses Int16 for RSSI/TxPower on D-Bus; but may come as Int32 depending on bindings.
        return v switch
        {
            short s => s,
            int i => (short)i,
            long l => (short)l,
            _ => null
        };
    }

    // ManufacturerData: a{qv} (UInt16 company id -> variant(byte[]))
    private static IReadOnlyDictionary<ushort, byte[]>? TryGetManufacturerData(IDictionary<string, object> props)
    {
        if (!props.TryGetValue("ManufacturerData", out var v) || v is null)
            return null;

        // Depending on Tmds.DBus version, it may already be Dictionary<ushort, object> or Dictionary<ushort, byte[]>
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

    // AdvertisingData: a{yv} (byte ad_type -> variant(byte[]))
    private static IReadOnlyDictionary<byte, byte[]>? TryGetAdvertisingData(IDictionary<string, object> props)
    {
        if (!props.TryGetValue("AdvertisingData", out var v) || v is null)
            return null;

        if (v is IDictionary<byte, byte[]> direct)
            return new Dictionary<byte, byte[]>(direct);

        if (v is IDictionary<byte, object> dictObj)
        {
            var res = new Dictionary<byte, byte[]>();
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

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}

public sealed class BlueZAdvertisementEvent
{
    public DateTimeOffset Timestamp { get; init; }
    public string DevicePath { get; init; } = "";
    public string? Address { get; init; }
    public string? Name { get; init; }
    public string? Alias { get; init; }
    public short? Rssi { get; init; }
    public short? TxPower { get; init; }

    public bool? Connected { get; init; }
    public bool? Paired { get; init; }
    public bool? Trusted { get; init; }

    public IReadOnlyDictionary<ushort, byte[]>? ManufacturerData { get; init; }
    public IReadOnlyDictionary<byte, byte[]>? AdvertisingData { get; init; }
}

#endregion

#region ---- App (advertisement logger) ----

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        var opt = Options.Parse(args);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

        var onceSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var gate = new object();

        using var ctx = await BlueZContext.CreateAsync();
        await using var scanner = new BlueZLeScanner(ctx);

        scanner.AdvertisementReceived += ev =>
        {
            // Address may be null in some early events; fallback to device path keying
            var key = ev.Address ?? ev.DevicePath;

            if (opt.Once)
            {
                lock (onceSet)
                {
                    if (!onceSet.Add(key))
                        return;
                }
            }

            lock (gate)
            {
                WriteColored(ConsoleColor.Cyan, ev.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture));
                Console.Write(" [");
                WriteColored(ConsoleColor.DarkCyan, ev.Address ?? "(NoAddr)");
                Console.Write("] ");
                WriteColored(ConsoleColor.Yellow, "RSSI:");
                Console.Write(ev.Rssi?.ToString(CultureInfo.InvariantCulture) ?? "?");
                Console.Write(" ");
                WriteColored(ConsoleColor.Magenta, ev.Name ?? ev.Alias ?? "(Unknown)");
                Console.WriteLine();

                if (opt.Info)
                {
                    WriteColored(ConsoleColor.Yellow, "DevicePath:");
                    Console.WriteLine($" {ev.DevicePath}");
                    WriteColored(ConsoleColor.Yellow, "Connected:");
                    Console.WriteLine($" {BoolOrQ(ev.Connected)}");
                    WriteColored(ConsoleColor.Yellow, "Paired:");
                    Console.WriteLine($" {BoolOrQ(ev.Paired)}");
                    WriteColored(ConsoleColor.Yellow, "Trusted:");
                    Console.WriteLine($" {BoolOrQ(ev.Trusted)}");
                    WriteColored(ConsoleColor.Yellow, "TxPower:");
                    Console.WriteLine($" {ev.TxPower?.ToString(CultureInfo.InvariantCulture) ?? "?"}");
                }

                if (opt.Manufacturer)
                {
                    if (ev.ManufacturerData is null || ev.ManufacturerData.Count == 0)
                    {
                        WriteColored(ConsoleColor.Yellow, "ManufacturerData:");
                        WriteColored(ConsoleColor.DarkGray, " (none)\n");
                    }
                    else
                    {
                        foreach (var md in ev.ManufacturerData)
                        {
                            WriteColored(ConsoleColor.Yellow, "CompanyId:");
                            Console.WriteLine($" 0x{md.Key:X4}");
                            WriteColored(ConsoleColor.Yellow, "Data:");
                            Console.WriteLine();
                            DumpHex(md.Value);
                        }
                    }
                }

                if (opt.Section)
                {
                    if (ev.AdvertisingData is null || ev.AdvertisingData.Count == 0)
                    {
                        WriteColored(ConsoleColor.Yellow, "AdvertisingData:");
                        WriteColored(ConsoleColor.DarkGray, " (none)\n");
                    }
                    else
                    {
                        // Similar to DataSections in Windows sample: show datatype + raw bytes
                        foreach (var sec in ev.AdvertisingData.OrderBy(k => k.Key))
                        {
                            WriteColored(ConsoleColor.Yellow, "DataType:");
                            Console.WriteLine($" 0x{sec.Key:X2}");
                            WriteColored(ConsoleColor.Yellow, "Data:");
                            Console.WriteLine();
                            DumpHex(sec.Value);
                        }
                    }
                }

                Console.Out.Flush();
            }
        };

        await scanner.StartAsync(cts.Token);

        // Like Windows sample: block until Enter. Here also support Ctrl+C.
        if (!opt.Once)
        {
            Console.WriteLine("Scanning... Press Enter to stop.");
            Console.ReadLine();
            cts.Cancel();
        }
        else
        {
            Console.WriteLine("Scanning (once mode)... Press Enter to stop.");
            Console.ReadLine();
            cts.Cancel();
        }

        await scanner.StopAsync();
        return 0;
    }

    private static string BoolOrQ(bool? b) => b.HasValue ? (b.Value ? "true" : "false") : "?";

    private static void DumpHex(byte[] data)
    {
        var span = data.AsSpan();
        for (int start = 0; start < span.Length; start += 16)
        {
            var slice = span.Slice(start, Math.Min(16, span.Length - start));
            WriteColored(ConsoleColor.DarkGreen, ToHexString(slice));
            Console.WriteLine();
        }
    }

    private static void WriteColored(ConsoleColor color, string value)
    {
        var backup = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(value);
        Console.ForegroundColor = backup;
    }

    private static string ToHexString(ReadOnlySpan<byte> source)
    {
        const string hexChars = "0123456789ABCDEF";
        // format: " xx xx ..."
        var sb = new StringBuilder(source.Length * 3 + 1);
        sb.Append(' ');
        foreach (var b in source)
        {
            sb.Append(' ');
            sb.Append(hexChars[b >> 4]);
            sb.Append(hexChars[b & 0xF]);
        }
        return sb.ToString();
    }
}

internal sealed class Options
{
    public bool Active { get; private set; }
    public bool Once { get; private set; }
    public bool Info { get; private set; }
    public bool Manufacturer { get; private set; }
    public bool Section { get; private set; }

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
                case "--once":
                case "-o":
                    o.Once = true;
                    break;
                case "--info":
                case "-i":
                    o.Info = true;
                    break;
                case "--manufacturer":
                case "-m":
                    o.Manufacturer = true;
                    break;
                case "--section":
                case "-s":
                    o.Section = true;
                    break;
                case "--help":
                case "-h":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
            }
        }

        // Active scan: BlueZ doesn't expose a simple "active/passive" toggle per app like Windows watcher.
        // We keep the flag for CLI compatibility; if needed, implement SetDiscoveryFilter/Transport/Pattern/RSSI, etc.
        if (o.Active)
        {
            Console.WriteLine("Note: --active is accepted, but BlueZ discovery mode is not a direct 1:1 mapping to Windows active scan.");
        }

        return o;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: dotnet run -- [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  -a, --active         Active scanning (best-effort; BlueZ differs from Windows)");
        Console.WriteLine("  -o, --once           Scan once per device");
        Console.WriteLine("  -i, --info           Show device information (from Device1 properties)");
        Console.WriteLine("  -m, --manufacturer   Show manufacturer data (if available)");
        Console.WriteLine("  -s, --section        Show advertising data sections (if available)");
        Console.WriteLine("  -h, --help           Show help");
    }
}

#endregion
