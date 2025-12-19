namespace WorkCliHost;

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class CliArgumentAttribute : Attribute
{
    public int Position { get; }
    public string Name { get; }
    public string? Description { get; set; }
    public bool IsRequired { get; set; } = true;

    public CliArgumentAttribute(int position, string name)
    {
        Position = position;
        Name = name;
    }
}
