namespace WorkDivoom;

using System.Diagnostics;
using System.Text.Json.Serialization;

using System.Text.Json;

internal static class Program
{
    static async Task Main()
    {
        using var serviceClient = new ServiceClient();
        var list = await serviceClient.FindDevices();
        foreach (var device in list)
        {
            Debug.WriteLine($"{device.Id} {device.Hardware} {device.Name} {device.IpAddress} {device.MacAddress}");
        }

        // TODO post perf
        // TODO post perf1
        // TODO db
        // TODO ...
    }
}


public sealed class DeviceInfo
{
    [JsonPropertyName("DeviceId")]
    public int Id { get; set; }

    [JsonPropertyName("Hardware")]
    public int Hardware { get; set; }

    [JsonPropertyName("DeviceName")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("DevicePrivateIP")]
    public string IpAddress { get; set; } = default!;

    [JsonPropertyName("DeviceMac")]
    public string MacAddress { get; set; } = default!;
}

internal sealed class DeviceListResult
{
    public DeviceInfo[] DeviceList { get; set; } = default!;
}

public sealed class ServiceClient : IDisposable
{
    private readonly HttpClient client = new()
    {
        BaseAddress = new Uri("http://app.divoom-gz.com"),
    };

    public void Dispose()
    {
        client.Dispose();
    }

    public async Task<IEnumerable<DeviceInfo>> FindDevices()
    {
        var response = await client.GetAsync("Device/ReturnSameLANDevice");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<DeviceListResult>(json);
        return result?.DeviceList ?? [];
    }
}

// TODO DeviceClient
