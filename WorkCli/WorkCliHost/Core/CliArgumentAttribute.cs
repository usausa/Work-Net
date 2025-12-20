namespace WorkCliHost.Core;

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class CliArgumentAttribute<T> : Attribute
{
    public const int AutoPosition = -1;

    public int Position { get; }
    public string Name { get; }
    public string? Description { get; set; }
    public bool IsRequired { get; set; } = true;
    public T? DefaultValue { get; set; }

    // Position省略可能なコンストラクタ
    public CliArgumentAttribute(string name)
    {
        Position = AutoPosition;
        Name = name;
    }

    // Position明示指定のコンストラクタ
    public CliArgumentAttribute(int position, string name)
    {
        Position = position;
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class CliArgumentAttribute : Attribute
{
    public const int AutoPosition = -1;

    public int Position { get; }
    public string Name { get; }
    public string? Description { get; set; }
    public bool IsRequired { get; set; } = true;

    // Position省略可能なコンストラクタ
    public CliArgumentAttribute(string name)
    {
        Position = AutoPosition;
        Name = name;
    }

    // Position明示指定のコンストラクタ
    public CliArgumentAttribute(int position, string name)
    {
        Position = position;
        Name = name;
    }
}
