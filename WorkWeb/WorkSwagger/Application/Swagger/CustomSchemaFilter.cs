namespace WorkSwagger.Application.Swagger;

using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using System.Diagnostics;
using System.Reflection;

public sealed class CustomSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        Debug.WriteLine($"===== Schema {context.Type} Parameter:{context.ParameterInfo?.Member.DeclaringType}.{context.ParameterInfo?.Name} Member:{context.MemberInfo?.DeclaringType}.{context.MemberInfo?.Name}");

        if (context.MemberInfo is not null)
        {
            ApplySchema(schema, context.MemberInfo, context.MemberInfo.Name, context.MemberInfo.DeclaringType);
        }
        else if (context.ParameterInfo is not null)
        {
            ApplySchema(schema, context.ParameterInfo, context.ParameterInfo.Name, context.ParameterInfo.ParameterType);
        }
    }

    private static void ApplySchema(OpenApiSchema schema, ICustomAttributeProvider provider, string? name, Type? type)
    {
        if (name?.EndsWith("Param") ?? false)
        {
            Debug.WriteLine($"++++ {name} Type:{schema.Type} Format:{schema.Format} Pattern:{schema.Pattern} Required:{schema.Required.Count} Nullable:{schema.Nullable}");
        }

        // TODO 辞書ベース Description

        // TODO 属性ベース Required, Nullable ...
    }
}
