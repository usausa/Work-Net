namespace WorkPlugin.Abstraction;

public interface IPlugin
{
    string Name { get; }

    string Platform { get; }

    void Execute();
}
