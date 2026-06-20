namespace WorkML.Tests;

using WorkML.Core;

using Xunit;

public sealed class CsvLoaderTests
{
    private static string DataDir => Path.Combine(AppContext.BaseDirectory, "test-data");

    [Fact]
    public void LoadDevices_reads_all_rows()
    {
        var devices = CsvLoader.LoadDevices(Path.Combine(DataDir, "devices.csv"));

        Assert.Equal(2, devices.Count);
        Assert.Equal(200f, devices.Single(d => d.DeviceId == "dev02").BaseVoltage);
    }

    [Fact]
    public void LoadReadings_parses_timestamp_and_value()
    {
        var readings = CsvLoader.LoadReadings(Path.Combine(DataDir, "readings.csv"));

        Assert.Equal(4, readings.Count);
        Assert.Equal("dev01", readings[0].DeviceId);
        Assert.Equal(new DateTime(2026, 6, 20, 0, 0, 0), readings[0].Timestamp);
        Assert.Equal(100.0f, readings[0].Value, 3);
    }
}
