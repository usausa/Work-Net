namespace WorkSwagger.Application.Swagger;

[AttributeUsage(AttributeTargets.Class)]
public sealed class SwaggerTagAttribute : Attribute
{
    public object Value { get; }

    public SwaggerTagAttribute(object value)
    {
        Value = value;
    }
}
