namespace WorkLoader.Library;

[AttributeUsage(AttributeTargets.Class)]
public sealed class LoaderAttribute : Attribute
{
    public string Name { get; }

    public LoaderAttribute(string name)
    {
        Name = name;
    }
}
