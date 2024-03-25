namespace WorkSwagger.Application.Swagger;

using Microsoft.AspNetCore.Mvc;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SwaggerResponseAttribute : ProducesResponseTypeAttribute
{
    public string[] ContentTypes { get; }

    public SwaggerResponseAttribute(int statusCode, Type? type = null)
        : base(type ?? typeof(void), statusCode)
    {
        ContentTypes = [];
    }

    public SwaggerResponseAttribute(int statusCode, params string[] contentTypes)
        : base(typeof(void), statusCode)
    {
        ContentTypes = contentTypes;
    }
}
