using System.Diagnostics;
using Vortice.XInput;

if (XInput.GetCapabilities(0, DeviceQueryType.Gamepad, out var capabilities))
{
    Debug.WriteLine($"Type: {capabilities.Type}");
    Debug.WriteLine($"SubType: {capabilities.SubType}");
    Debug.WriteLine($"CapabilityFlags: {capabilities.Flags}");
    Debug.WriteLine($"Gamepad: {capabilities.Gamepad}");
    Debug.WriteLine($"Vibration: {capabilities.Vibration.LeftMotorSpeed}/{capabilities.Vibration.RightMotorSpeed}");
}

var battery = XInput.GetBatteryInformation(0, BatteryDeviceType.Gamepad);
Debug.WriteLine($"Battery: {battery.BatteryType} {battery.BatteryLevel}");

//XInput.SetVibration(0, 32767, 0);
//XInput.SetVibration(0, 0, 32767);

while (true)
{
    if (XInput.GetKeystroke(0, out var keystroke))
    {
        Debug.WriteLine($"Keystroke: VirtualKey={keystroke.VirtualKey}, Unicode={(int)keystroke.Unicode}, Flags={keystroke.Flags}, UserIndex={keystroke.UserIndex}, HidCode={keystroke.HidCode}");
    }
    if (XInput.GetState(0, out var state))
    {
        Debug.WriteLine($"State: {state.PacketNumber} : {state.Gamepad}");
    }

    Thread.Sleep(100);
}