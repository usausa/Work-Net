namespace WorkCliHost.Core;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CliCommandAttribute : Attribute
{
    public string Name { get; }
    public string? Description { get; set; }

    public CliCommandAttribute(string name)
    {
        Name = name;
    }
}
