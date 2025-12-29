namespace WorkInterceptor.Library;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CommandAttribute : Attribute
{
    public string Name { get; }

    public string? Description { get; }

    public CommandAttribute(string name)
    {
        Name = name;
    }

    public CommandAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
}

#pragma warning disable CA1711
public interface IOptionAttribute
{
    string GetName();

    string[] GetAliases();

    int GetOrder();

    string? GetDescription();

    bool GetRequired();

    object? GetDefaultValue();

    string[] GetCompletions();
}
#pragma warning restore CA1711

[AttributeUsage(AttributeTargets.Property)]
public abstract class BaseOptionAttribute : Attribute, IOptionAttribute
{
    public int Order { get; }

    public string Name { get; }

    public string[] Aliases { get; }

    public string? Description { get; set; }

    public bool Required { get; set; }

    protected BaseOptionAttribute(int order, string name, string[] aliases)
    {
        Order = order;
        Name = name;
        Aliases = aliases;
    }

#pragma warning disable CA1033
    string IOptionAttribute.GetName() => Name;

    string[] IOptionAttribute.GetAliases() => Aliases;

    int IOptionAttribute.GetOrder() => Order;

    string? IOptionAttribute.GetDescription() => Description;

    bool IOptionAttribute.GetRequired() => Required;

    object? IOptionAttribute.GetDefaultValue() => ResolveDefaultValue();

    string[] IOptionAttribute.GetCompletions() => ResolveCompletions();

    protected abstract object? ResolveDefaultValue();

    protected abstract string[] ResolveCompletions();
#pragma warning restore CA1033
}

public sealed class OptionAttribute : BaseOptionAttribute
{
    public string[]? Completions { get; set; }

    public OptionAttribute(string name, params string[] aliases)
        : base(Int32.MaxValue, name, aliases)
    {
    }

    public OptionAttribute(int order, string name, params string[] aliases)
        : base(order, name, aliases)
    {
    }

    protected override object? ResolveDefaultValue() => null;

    protected override string[] ResolveCompletions() => Completions ?? [];
}

public sealed class OptionAttribute<T> : BaseOptionAttribute
{
    public T? DefaultValue { get; set; }

    public T[]? Completions { get; set; }

    public OptionAttribute(string name, params string[] aliases)
        : base(Int32.MaxValue, name, aliases)
    {
    }

    public OptionAttribute(int order, string name, params string[] aliases)
        : base(order, name, aliases)
    {
    }

    protected override object? ResolveDefaultValue() => DefaultValue;

    protected override string[] ResolveCompletions() =>
        Completions is { Length: > 0 } ? Completions.Select(c => c?.ToString() ?? string.Empty).ToArray() : [];
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
