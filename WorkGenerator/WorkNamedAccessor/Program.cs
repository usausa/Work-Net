namespace WorkNamedAccessor;

internal static class Program
{
    public static void Main()
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class NamedGetterAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class NamedSetterAttribute : Attribute
{
}

// 継承可能でない public
public partial class Type1
{
    public int Value1 { get; set; }

    [NamedGetter]
    public partial object? GetValue(string name);
}

partial class Type1
{
    public partial object? GetValue(string name)
    {
        return name switch
        {
            "Value1" => Value1,
            _ => null
        };
    }
}

// virtual
public abstract partial class Type2
{
    public int Value1 { get; set; }

    [NamedGetter]
    public virtual partial object? GetValue(string name);
}

partial class Type2
{
    public virtual partial object? GetValue(string name)
    {
        return name switch
        {
            "Value1" => Value1,
            _ => null
        };
    }
}

public partial class Type2A : Type2
{
    public int Value2 { get; set; }
}

partial class Type2A
{
    public override object? GetValue(string name)
    {
        return name switch
        {
            "Value2" => Value2,
            _ => base.GetValue(name)
        };
    }
}

public partial class Type2B : Type2A
{
    public int Value3 { get; set; }
}

partial class Type2B
{
    public override object? GetValue(string name)
    {
        return name switch
        {
            "Value3" => Value3,
            _ => base.GetValue(name)
        };
    }
}

// abstract
public abstract class Type3
{
    public int Value1 { get; set; }

    public int Value1B { get; set; }

    [NamedGetter]
    public abstract object? GetValue(string name);
}

public partial class Type3A : Type3
{
    public int Value2 { get; set; }
}

partial class Type3A
{
    public override object? GetValue(string name)
    {
        return name switch
        {
            "Value3" => Value2,
            _ => null
        };
    }
}

public partial class Type3B : Type3A
{
    public int Value3 { get; set; }
}

partial class Type3B
{
    public override object? GetValue(string name)
    {
        return name switch
        {
            "Value3" => Value2,
            _ => base.GetValue(name)
        };
    }
}
