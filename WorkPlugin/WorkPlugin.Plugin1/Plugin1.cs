namespace WorkPlugin.Plugin1;

using WorkPlugin.Abstraction;

public class Plugin1 : IPlugin
{
    public string Name => "Plugin1";

    public string Platform => "Cross-Platform";

    public void Execute()
    {
        Console.WriteLine($"[{Name}] Executing on {Platform}");
    }
}
