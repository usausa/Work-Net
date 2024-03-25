namespace WorkSwagger.Application.Swagger;

public sealed class SchemeEntry
{
    public string? Description { get; }

    public object? Example { get; set; }

    public string? Format { get; set; }

    public SchemeEntry()
    {
    }

    public SchemeEntry(string description)
    {
        Description = description;
    }
}
