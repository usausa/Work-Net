namespace WorkSwagger.Application.Swagger;

using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using WorkSwagger.Application.Authentication;

public sealed class CustomDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Remove from scheme
        swaggerDoc.Components.Schemas.Remove(typeof(Credential).FullName!);
    }
}
