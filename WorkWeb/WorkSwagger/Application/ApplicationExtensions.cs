namespace WorkSwagger.Application;

using Microsoft.OpenApi.Models;

using WorkSwagger.Application.Swagger;
using WorkSwagger.Areas;

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

            // Custom
            options.SchemaFilter<CustomSchemaFilter>();
            options.ParameterFilter<CustomParameterFilter>();
            options.RequestBodyFilter<CustomRequestBodyFilter>();
            options.OperationFilter<CustomOperationFilter>();
            options.DocumentFilter<CustomDocumentFilter<Tags>>();

            // Change scheme name
            options.CustomSchemaIds(t => t.FullName?
                .Replace("WorkSwagger.Areas.", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(".Controllers.", string.Empty, StringComparison.OrdinalIgnoreCase));

            // Order
            options.OrderActionsBy(api =>
            {
                var area = api.ActionDescriptor.RouteValues["area"];
                var controller = api.ActionDescriptor.RouteValues["controller"];
                return $"{area}_{controller}";
            });
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
