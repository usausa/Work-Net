namespace WorkPlugin.Plugin1;

using WorkPlugin.Abstraction;

public class Plugin1 : IPlugin
{
    public string GetMessage() => "Hello from Plugin1!";
}
