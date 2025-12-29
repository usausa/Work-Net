namespace WorkInterceptor.Library;

public interface IBuilder
{
    void Execute();

    void Execute(int value);
}

#pragma warning disable CA1822
public sealed class Builder : IBuilder
{
    public void Execute()
    {
        Console.WriteLine("Execute");
    }

    public void Execute(int value)
    {
        Console.WriteLine($"Execute {value}");
    }
}
#pragma warning restore CA1822
