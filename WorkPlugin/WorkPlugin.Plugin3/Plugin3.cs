namespace WorkPlugin.Plugin3;

using WorkPlugin.Abstraction;

public class Plugin3 : IPlugin
{
    public string Name => "Plugin3";

    public string Platform => "MacOS";

    public void Execute()
    {
        Console.WriteLine($"[{Name}] Executing on {Platform}");
    }
}
