namespace WorkSwagger.Application.Swagger;

[AttributeUsage(AttributeTargets.Method)]
public sealed class MySwaggerOperationAttribute : Attribute
{
    public string Summary { get; }

    public string Description { get; }

    public MySwaggerOperationAttribute(string summary = "", string description = "")
    {
        Summary = summary;
        Description = description;
    }
}
