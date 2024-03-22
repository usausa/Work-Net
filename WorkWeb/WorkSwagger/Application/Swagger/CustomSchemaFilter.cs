namespace WorkSwagger.Application.Swagger;

using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using System.Diagnostics;

public sealed class CustomSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        Debug.WriteLine($"===== Schema {context.Type} Parameter:{context.ParameterInfo?.Member.DeclaringType}.{context.ParameterInfo?.Name} Member:{context.MemberInfo?.DeclaringType}.{context.MemberInfo?.Name}");

        // TODO 辞書ベース？
    }
}
