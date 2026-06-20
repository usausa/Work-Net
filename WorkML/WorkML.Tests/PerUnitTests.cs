namespace WorkML.Tests;

using WorkML.Core;

using Xunit;

public sealed class PerUnitTests
{
    [Theory]
    [InlineData(198f, 200f, 0.99)]   // 200V系の電圧降下
    [InlineData(99f, 100f, 0.99)]    // 100V系も同じ p.u. に揃う
    [InlineData(200f, 200f, 1.0)]    // 定格どおり
    public void Voltage_normalizes_by_base(float measured, float baseVoltage, double expected)
    {
        var spec = new DeviceSpec { DeviceId = "d", SiteId = "s", BaseVoltage = baseVoltage, RatedCurrent = 30f };

        Assert.Equal(expected, PerUnit.Voltage(measured, spec), 3);
    }

    [Fact]
    public void UniqueId_combines_site_device_channel()
    {
        var spec = new DeviceSpec { DeviceId = "dev03", SiteId = "site02", BaseVoltage = 200f, RatedCurrent = 30f };

        Assert.Equal("site02-dev03-ch1", PerUnit.UniqueId(spec, 1));
    }
}
