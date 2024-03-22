namespace WorkSwagger.Application.Swagger;

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Smart.Collections.Generic;

using Swashbuckle.AspNetCore.SwaggerGen;

using WorkSwagger.Application.Authentication;

public sealed class CustomOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // For credential
        if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor descriptor)
        {
            var parameter = descriptor.MethodInfo.GetParameters()
                .FirstOrDefault(static x => x.ParameterType == typeof(Credential));
            if (parameter is not null)
            {
                operation.Parameters.RemoveWhere(x => x.Name == parameter.Name);
            }

            operation.Parameters.Insert(0, new OpenApiParameter
            {
                Name = ExtensionHeaders.ClientId,
                In = ParameterLocation.Header,
                Example = new OpenApiString("0000000000000000")
            });
        }
    }
}
