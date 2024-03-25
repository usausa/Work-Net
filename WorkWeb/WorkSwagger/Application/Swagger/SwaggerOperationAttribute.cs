namespace WorkSwagger.Application.Swagger;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SwaggerOperationAttribute : Attribute
{
    public string Summary { get; }

    public string Description { get; }

    public SwaggerOperationAttribute(string summary = "", string description = "")
    {
        Summary = summary;
        Description = description;
    }
}
