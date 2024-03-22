namespace WorkSwagger.Application;

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using WorkSwagger.Application.Swagger;

public static class ApplicationExtensions
{
    //--------------------------------------------------------------------------------
    // Swagger
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureSwaggerDefaults(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("example", new OpenApiInfo { Title = "Example APIs", Version = "v1" });

            options.TagActionsBy(api =>
            {
                if (!String.IsNullOrEmpty(api.GroupName))
                {
                    return new[] { api.GroupName };
                }

                if (api.ActionDescriptor is ControllerActionDescriptor descriptor)
                {
                    var area = descriptor.EndpointMetadata.OfType<AreaAttribute>().FirstOrDefault();
                    if (area is not null)
                    {
                        return new[] { $"{area.RouteValue} {descriptor.ControllerName}" };
                    }

                    return new[] { descriptor.ControllerName };
                }

                throw new InvalidOperationException("Unable to determine tag for endpoint.");
            });

            options.CustomSchemaIds(t => t.FullName?
                .Replace("Example.Areas.", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(".Models.", string.Empty, StringComparison.OrdinalIgnoreCase));

            options.EnableAnnotations();

            options.OperationFilter<CustomOperationFilter>();
            options.DocumentFilter<CustomDocumentFilter>();
        });

        return builder;
    }

    public static WebApplication UseSwaggerDefaults(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/example/swagger.json", "example APIs");
        });

        return app;
    }
}
