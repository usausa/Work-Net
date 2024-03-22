namespace WorkSwagger.Application.Swagger;

using System.Diagnostics;

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Smart.Collections.Generic;
using Smart.Text;

using Swashbuckle.AspNetCore.SwaggerGen;

using WorkSwagger.Application.Authentication;

public sealed class CustomOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        Debug.WriteLine($"===== Operation {context.MethodInfo.DeclaringType?.FullName}.{context.MethodInfo.Name}");

        // OperationId
        operation.OperationId = Inflector.Camelize(context.ApiDescription.RelativePath?.Replace("api/", "").Replace("/", "_"));

        // TODO TagsはここでController毎？

        // Common setting
        operation.Responses.Add("400", new OpenApiResponse { Description = "不正要求" });

        // For credential
        var parameter = context.MethodInfo.GetParameters().FirstOrDefault(static x => x.ParameterType == typeof(Credential));
        if (parameter is not null)
        {
            operation.Parameters.RemoveWhere(x => x.Name == parameter.Name);
            operation.Parameters.Insert(0, new OpenApiParameter
            {
                Name = ExtensionHeaders.ClientId,
                In = ParameterLocation.Header,
                Example = new OpenApiString("0000000000000000")
            });

            operation.Responses.Add("401", new OpenApiResponse { Description = "認証失敗" });
        }
    }

    //private static string ResolveMethodVerb()
    //{

    //}
}
