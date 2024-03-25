namespace WorkSwagger.Application.Swagger;

[AttributeUsage(AttributeTargets.Class)]
public sealed class MySwaggerTagAttribute : Attribute
{
    public object Value { get; }

    public MySwaggerTagAttribute(object value)
    {
        Value = value;
    }
}
