namespace WorkSwagger.Application.Swagger;

using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

public sealed class CustomParameterFilter : IParameterFilter
{
    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        // Do Nothing
    }
}
