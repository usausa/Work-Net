namespace WorkInterceptor.Library;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CommandAttribute : Attribute
{
    public string Name { get; }

    public CommandAttribute(string name)
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class OptionAttribute : Attribute
{
    public int Order { get; }

    public string Name { get; }

    public string[] Values { get; set; } = [];

    public OptionAttribute(string name)
    {
        Order = int.MaxValue;
        Name = name;
    }

    public OptionAttribute(int order, string name)
    {
        Order = order;
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class OptionAttribute<T> : Attribute
{
    public int Order { get; }

    public string Name { get; }

    public T[] Values { get; set; } = [];

    public OptionAttribute(string name)
    {
        Order = int.MaxValue;
        Name = name;
    }

    public OptionAttribute(int order, string name)
    {
        Order = order;
        Name = name;
    }
}

public interface IBuilder
{
    void Execute<T>();

    void Execute<T>(Action action);
}

#pragma warning disable CA1822
public sealed class Builder : IBuilder
{
    public void Execute<T>()
    {
        Console.WriteLine("Execute");
    }

    public void Execute<T>(Action action)
    {
        Console.WriteLine("Execute");
        action();
    }
}
#pragma warning restore CA1822
