// TODO Turing
using InfoPanel.TuringPanel;

using LibUsbDotNet;

var usbRegistry = UsbDevice.AllDevices.FirstOrDefault(x => x.Vid == 0x1cbe && x.Pid == 0x0088);
if (usbRegistry is null)
{
    return;
}

using var device = new TuringDevice();
if (!device.Initialize(usbRegistry))
{
    return;
}

device.ClearScreen();

device.SendImage("image.png");
