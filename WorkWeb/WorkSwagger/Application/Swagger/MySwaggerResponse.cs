namespace WorkSwagger.Application.Swagger;

using Microsoft.AspNetCore.Mvc;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MySwaggerResponse : ProducesResponseTypeAttribute
{
    public string[] ContentTypes { get; }

    public MySwaggerResponse(int statusCode, Type? type = null)
        : base(type ?? typeof(void), statusCode)
    {
        ContentTypes = [];
    }

    public MySwaggerResponse(int statusCode, params string[] contentTypes)
        : base(typeof(void), statusCode)
    {
        ContentTypes = contentTypes;
    }
}
