using System.Device.Gpio;

using Smdn.Devices.MCP2221;

using var device = MCP2221.Open();

device.GP2.ConfigureAsGPIO(PinMode.Output);
device.GP3.ConfigureAsGPIO(PinMode.Output);

while (true)
{
    device.GP2.SetValue(true);
    Thread.Sleep(1000);
    device.GP2.SetValue(false);
    device.GP3.SetValue(true);
    Thread.Sleep(1000);
    device.GP3.SetValue(false);
}
