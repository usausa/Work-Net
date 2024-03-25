namespace WorkSwagger.Application.Swagger;

using System.Diagnostics;
using System.Reflection;

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

        // Summary/Description
        var operationAttribute = context.MethodInfo.GetCustomAttribute<SwaggerOperationAttribute>();
        if (operationAttribute is not null)
        {
            operation.Summary = operationAttribute.Summary;
            operation.Description = operationAttribute.Description;
        }

        // OperationId
        operation.OperationId = Inflector.Camelize(context.ApiDescription.RelativePath?.Replace("api/", "").Replace("/", "_"));

        // Tags
        var tagAttribute = context.MethodInfo.DeclaringType?.GetCustomAttribute<SwaggerTagAttribute>();
        if (tagAttribute is not null)
        {
            operation.Tags.Clear();
            operation.Tags.Add(new OpenApiTag { Name = tagAttribute.Value.ToString()?.ToLowerInvariant() });
        }

        // Response
        foreach (var responseAttribute in context.MethodInfo.GetCustomAttributes<SwaggerResponseAttribute>())
        {
            var statusCode = responseAttribute.StatusCode.ToString();
            if (!operation.Responses.TryGetValue(statusCode, out var response))
            {
                response = new OpenApiResponse();
                operation.Responses[statusCode] = response;
            }

            response.Description = responseAttribute.StatusCode switch
            {
                StatusCodes.Status200OK => "処理成功",
                StatusCodes.Status404NotFound => "該当なし",
                _ => response.Description
            };

            if (responseAttribute.ContentTypes.Length > 0)
            {
                response.Content.Clear();
                foreach (var contentType in responseAttribute.ContentTypes)
                {
                    var schema = ((responseAttribute.Type != null) && (responseAttribute.Type != typeof(void)))
                        ? context.SchemaGenerator.GenerateSchema(responseAttribute.Type, context.SchemaRepository)
                        : null;
                    response.Content.Add(contentType, new OpenApiMediaType { Schema = schema });
                }
            }
        }

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
