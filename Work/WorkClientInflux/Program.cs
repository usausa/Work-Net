using System.Diagnostics;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core;

var url = args[0];
var organization = args[1];
var bucket = args[2];
var token = args[3];

using var client = new InfluxDBClient(url, token);

var rand = new Random();
while (true)
{
    var writer = client.GetWriteApiAsync();

    var value = new Speed
    {
        DateTime = DateTime.UtcNow,
        Download = rand.NextDouble() * 1000,
        Upload = rand.NextDouble() * 1000
    };
    try
    {
        await writer.WriteMeasurementAsync(value, WritePrecision.Ns, bucket, organization);
    }
    catch (Exception e)
    {
        Debug.WriteLine(e);
    }

    await Task.Delay(10_000);
}



[Measurement("Speed")]
public class Speed
{
    [Column(IsTimestamp = true)] public DateTime DateTime { get; set; }
    [Column("Download")] public double Download { get; set; }
    [Column("Upload")] public double Upload { get; set; }
}
