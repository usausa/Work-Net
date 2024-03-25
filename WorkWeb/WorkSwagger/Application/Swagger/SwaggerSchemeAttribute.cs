namespace WorkSwagger.Application.Swagger;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class SwaggerSchemeAttribute : Attribute
{
    public string? Description { get; }

    public object? Example { get; }

    public string? Format { get; }

    public SwaggerSchemeAttribute(
        string? description = null,
        object? example = null,
        string? format = null)
    {
        Description = description;
        Example = example;
        Format = format;
    }
}
