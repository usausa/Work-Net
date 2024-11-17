using Windows.Devices.Enumeration;
using Windows.Devices.WiFi;

var result = await DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());
if (result.Count == 0)
{
    Console.WriteLine("Not found.");
    return;
}

Console.WriteLine($"Info: {result[0].Name} {result[0].Kind}");
foreach (var property in result[0].Properties)
{
    Console.WriteLine($"  {property.Key}: {property.Value}");
}

var adapter = await WiFiAdapter.FromIdAsync(result[0].Id);

while (true)
{
    await adapter.ScanAsync();

    foreach (var item in adapter.NetworkReport.AvailableNetworks)
    {
        Console.WriteLine($"{item.Ssid} {item.Bssid} {item.NetworkRssiInDecibelMilliwatts} {item.NetworkKind} {item.PhyKind} {item.SecuritySettings.NetworkEncryptionType}");
    }

    await Task.Delay(5000);
}
