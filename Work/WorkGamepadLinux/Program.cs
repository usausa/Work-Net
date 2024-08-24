using Gamepad;

using var gamepad = new GamepadController("/dev/input/js0");

gamepad.ButtonChanged += (_, e) =>
{
    Console.WriteLine($"Button {e.Button} Changed: {e.Pressed}");
};
gamepad.AxisChanged += (_, e) =>
{
    Console.WriteLine($"Axis {e.Axis} Changed: {e.Value}");
};

Console.ReadLine();
