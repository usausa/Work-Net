// Program.cs (single file)
// Linux BLE advertisement logger using BlueZ + Tmds.DBus
//
// Changes per request:
// - Removed --once option
// - Keep --active option (best-effort; BlueZ doesn't map 1:1 to Windows active scan)
// - Fix: continuously receive advertisements (the earlier sample could look "one-shot" because it only printed
//        on InterfacesAdded or did not reliably subscribe to PropertiesChanged for existing devices).
//
// Approach for continuous reception:
// - Start discovery (Adapter1.StartDiscovery)
// - For every Device1 object (existing and newly added), subscribe to its PropertiesChanged
// - Print on RSSI updates and/or AdvertisingData/ManufacturerData updates (not only once)
// - Also print initial snapshot for existing devices, then keep printing as updates arrive
//
// Notes:
// - Many devices will repeatedly update RSSI while discovery is running, so you'll see continuous logs.
// - Some BlueZ configurations may not refresh AdvertisingData on every interval; RSSI is the reliable "heartbeat".
// - ManufacturerData/AdvertisingData presence depends on BlueZ version and controller capabilities.

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

// Signal subscription only (avoid Get/Set signature issues across Tmds.DBus versions)
[DBusInterface("org.freedesktop.DBus.Properties")]
public interface IProperties : IDBusObject
{
    Task<IDisposable> WatchPropertiesChangedAsync(
        Action<(string iface, IDictionary<string, object> changed, string[] invalidated)> handler);
}

#endregion

#region ---- BlueZ wrapper scanning/advertisement events ----

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

    // Which property caused this event (useful for debugging/understanding update cadence)
    public string? CauseProperty { get; init; }
}

public sealed class BlueZLeScanner : IAsyncDisposable
{
    private readonly BlueZContext _ctx;

    private IDisposable? _ifAddedSub;
    private IDisposable? _ifRemovedSub;

    // Device PropertiesChanged subscription per Device1 object
    private readonly ConcurrentDictionary<ObjectPath, IDisposable> _devicePropsSubs = new();

    public event Action<BlueZAdvertisementEvent>? AdvertisementReceived;

    public BlueZLeScanner(BlueZContext ctx) => _ctx = ctx;

    public async Task StartAsync(bool active, CancellationToken ct)
    {
        // Watch newly added devices
        _ifAddedSub = await _ctx.ObjectManager.WatchInterfacesAddedAsync(ev =>
        {
            try
            {
                if (!ev.interfaces.TryGetValue("org.bluez.Device1", out var props))
                    return;

                // subscribe to continuous updates
                _ = EnsureDeviceSubscriptionAsync(ev.objectPath);

                // also emit initial snapshot
                EmitSnapshot(ev.objectPath, props, "InterfacesAdded");
            }
            catch { /* ignore */ }
        });

        // Cleanup when removed
        _ifRemovedSub = await _ctx.ObjectManager.WatchInterfacesRemovedAsync(ev =>
        {
            if (_devicePropsSubs.TryRemove(ev.objectPath, out var sub))
                sub.Dispose();
        });

        // Subscribe for all existing cached Device1 objects (important for continuous logging)
        var objects = await _ctx.ObjectManager.GetManagedObjectsAsync();
        foreach (var kv in objects)
        {
            if (kv.Value.TryGetValue("org.bluez.Device1", out var props))
            {
                _ = EnsureDeviceSubscriptionAsync(kv.Key);
                EmitSnapshot(kv.Key, props, "Initial");
            }
        }

        // Start discovery
        // Note: BlueZ doesn't expose a simple per-app active/passive toggle identical to Windows.
        // We keep the flag; if you need strict control, we can add SetDiscoveryFilter (UUID/RSSI/Transport).
        if (active)
        {
            Console.WriteLine("Note: --active is best-effort on BlueZ (no 1:1 mapping to Windows active scan).");
        }

        try
        {
            await _ctx.Adapter.StartDiscoveryAsync();
        }
        catch (DBusException ex) when (ex.ErrorName?.Contains("InProgress", StringComparison.OrdinalIgnoreCase) == true)
        {
            // ignore
        }

        // keep running until cancelled
        _ = ct;
    }

    public async Task StopAsync()
    {
        try { await _ctx.Adapter.StopDiscoveryAsync(); } catch { }

        _ifAddedSub?.Dispose(); _ifAddedSub = null;
        _ifRemovedSub?.Dispose(); _ifRemovedSub = null;

        foreach (var kv in _devicePropsSubs)
            kv.Value.Dispose();
        _devicePropsSubs.Clear();
    }

    private async Task EnsureDeviceSubscriptionAsync(ObjectPath devicePath)
    {
        if (_devicePropsSubs.ContainsKey(devicePath))
            return;

        var propsProxy = _ctx.Connection.CreateProxy<IProperties>("org.bluez", devicePath);

        IDisposable sub = await propsProxy.WatchPropertiesChangedAsync(ev =>
        {
            try
            {
                if (!string.Equals(ev.iface, "org.bluez.Device1", StringComparison.Ordinal))
                    return;

                // Continuous updates typically come as RSSI changes.
                // We'll emit on RSSI OR ManufacturerData OR AdvertisingData OR Name/Alias changes.
                if (!ShouldEmit(ev.changed, out var cause))
                    return;

                var adv = BuildEventFromChanged(devicePath, ev.changed, cause);
                AdvertisementReceived?.Invoke(adv);
            }
            catch
            {
                // ignore to keep scanner alive
            }
        });

        if (!_devicePropsSubs.TryAdd(devicePath, sub))
            sub.Dispose();
    }

    private void EmitSnapshot(ObjectPath devicePath, IDictionary<string, object> props, string cause)
    {
        var ev = new BlueZAdvertisementEvent
        {
            Timestamp = DateTimeOffset.Now,
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
            CauseProperty = cause
        };

        AdvertisementReceived?.Invoke(ev);
    }

    private static bool ShouldEmit(IDictionary<string, object> changed, out string cause)
    {
        // Emit on changes we care about; RSSI will usually refresh continuously during discovery.
        if (changed.ContainsKey("RSSI")) { cause = "RSSI"; return true; }
        if (changed.ContainsKey("AdvertisingData")) { cause = "AdvertisingData"; return true; }
        if (changed.ContainsKey("ManufacturerData")) { cause = "ManufacturerData"; return true; }
        if (changed.ContainsKey("Name")) { cause = "Name"; return true; }
        if (changed.ContainsKey("Alias")) { cause = "Alias"; return true; }
        if (changed.ContainsKey("TxPower")) { cause = "TxPower"; return true; }

        // If you want truly "any change prints", return true here.
        cause = "";
        return false;
    }

    private BlueZAdvertisementEvent BuildEventFromChanged(ObjectPath devicePath, IDictionary<string, object> changed, string cause)
    {
        return new BlueZAdvertisementEvent
        {
            Timestamp = DateTimeOffset.Now,
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
            CauseProperty = cause
        };
    }

    private static bool? TryGetBool(IDictionary<string, object> props, string key)
        => props.TryGetValue(key, out var v) && v is bool b ? b : null;

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

    // ManufacturerData: a{qv}
    private static IReadOnlyDictionary<ushort, byte[]>? TryGetManufacturerData(IDictionary<string, object> props)
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

    // AdvertisingData: a{yv}
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

    public async ValueTask DisposeAsync() => await StopAsync();
}

#endregion

#region ---- App ----

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        var opt = Options.Parse(args);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

        using var ctx = await BlueZContext.CreateAsync();
        await using var scanner = new BlueZLeScanner(ctx);

        var gate = new object();

        scanner.AdvertisementReceived += ev =>
        {
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
                if (!string.IsNullOrEmpty(ev.CauseProperty))
                {
                    Console.Write(" ");
                    WriteColored(ConsoleColor.DarkGray, $"({ev.CauseProperty})");
                }
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

        await scanner.StartAsync(opt.Active, cts.Token);

        Console.WriteLine("Scanning... Press Enter to stop. (Ctrl+C also works)");
        Console.ReadLine();
        cts.Cancel();

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
        return o;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: dotnet run -- [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  -a, --active         Active scanning (best-effort; BlueZ differs from Windows)");
        Console.WriteLine("  -i, --info           Show device information (from Device1 properties)");
        Console.WriteLine("  -m, --manufacturer   Show manufacturer data (if available)");
        Console.WriteLine("  -s, --section        Show advertising data sections (if available)");
        Console.WriteLine("  -h, --help           Show help");
    }
}

#endregion
