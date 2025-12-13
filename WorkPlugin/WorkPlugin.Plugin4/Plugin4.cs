namespace WorkPlugin.Plugin4;

using WorkPlugin.Abstraction;

public class Plugin4 : IPlugin
{
    public string Name => "Plugin4";

    public string Platform => "Linux";

    public void Execute()
    {
        Console.WriteLine($"[{Name}] Executing on {Platform}");
    }
}
