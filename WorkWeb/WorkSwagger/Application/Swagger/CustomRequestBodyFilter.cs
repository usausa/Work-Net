namespace WorkSwagger.Application.Swagger;

using System.Diagnostics;

using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

public sealed class CustomRequestBodyFilter : IRequestBodyFilter
{
    public void Apply(OpenApiRequestBody requestBody, RequestBodyFilterContext context)
    {
        Debug.WriteLine($"===== RequestBody {context.BodyParameterDescription.Type}");

        // Common setting
        requestBody.Description = "リクエストボディ";
        requestBody.Required = true;
    }
}
