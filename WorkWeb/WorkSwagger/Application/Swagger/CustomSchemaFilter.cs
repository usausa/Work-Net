namespace WorkSwagger.Application.Swagger;

using System.Diagnostics;
using System.Reflection;

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

public sealed class CustomSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        Debug.WriteLine($"===== Schema {context.Type} Parameter:{context.ParameterInfo?.Member.DeclaringType}.{context.ParameterInfo?.Name} Member:{context.MemberInfo?.DeclaringType}.{context.MemberInfo?.Name}");

        if (context.MemberInfo is not null)
        {
            ApplySchema(schema, context.MemberInfo.Name, context.MemberInfo);
        }
        else if (context.ParameterInfo is not null)
        {
            ApplySchema(schema, context.ParameterInfo.Name!, context.ParameterInfo);
        }
    }

    private static void ApplySchema(OpenApiSchema schema, string name, ICustomAttributeProvider provider)
    {
        // Debug
        if (name.EndsWith("Param"))
        {
            Debug.WriteLine($"++++ {name} Type:{schema.Type} Format:{schema.Format} Pattern:{schema.Pattern} Required:{schema.Required.Count} Nullable:{schema.Nullable}");
        }

        var entry = SchemeDictionary.Lookup(name);
        if (entry is not null)
        {
            schema.Description = entry.Description;
            if (entry.Example is not null)
            {
                schema.Example = ToOpenApiValue(entry.Example);
            }
            if (entry.Format is not null)
            {
                schema.Format = entry.Format;
            }
        }

        var schemeAttribute = provider.GetCustomAttributes(true).OfType<SwaggerSchemeAttribute>().FirstOrDefault();
        if (schemeAttribute is not null)
        {
            schema.Description = schemeAttribute.Description;
            if (schemeAttribute.Example is not null)
            {
                schema.Example = ToOpenApiValue(schemeAttribute.Example);
            }
            if (schemeAttribute.Format is not null)
            {
                schema.Format = schemeAttribute.Format;
            }
        }

        // TODO Required, Nullable ...
    }

    private static IOpenApiAny ToOpenApiValue(object value)
    {
        if (value is string stringValue)
        {
            return new OpenApiString(stringValue);
        }
        if (value is bool boolValue)
        {
            return new OpenApiBoolean(boolValue);
        }
        if (value is int intValue)
        {
            return new OpenApiInteger(intValue);
        }
        if (value is long longValue)
        {
            return new OpenApiLong(longValue);
        }
        if (value is float floatValue)
        {
            return new OpenApiFloat(floatValue);
        }
        if (value is double doubleValue)
        {
            return new OpenApiDouble(doubleValue);
        }

        throw new ArgumentException($"Unsupported value type. type=[{value.GetType()}], value=[{value}]");
    }
}
