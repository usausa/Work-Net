namespace WorkSwagger.Application.Swagger;

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using WorkSwagger.Application.Authentication;

public sealed class CustomDocumentFilter<TTag> : IDocumentFilter
    where TTag : struct, Enum
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        Debug.WriteLine($"===== Document {context.DocumentName}");

        // Info
        swaggerDoc.Info.Description = "Example APIの詳細";
        swaggerDoc.Info.TermsOfService = new Uri("http://example/terms");
        swaggerDoc.Info.Contact = new OpenApiContact
        {
            Name = "連絡先の名称",
            Url = new Uri("http://example/contact"),
            Email = "contact@example.local"
        };
        swaggerDoc.Info.License = new OpenApiLicense
        {
            Name = "ライセンス 2.0",
            Url = new Uri("http://example/licenses/LICENSE-2.0.html")
        };

        // ExternalDocs
        swaggerDoc.ExternalDocs = new OpenApiExternalDocs
        {
            Description = "更なるドキュメント",
            Url = new Uri("http://example/docs")
        };

        // Servers
        // TODO 出力時のみ？
        //swaggerDoc.Servers.Add(new OpenApiServer
        //{
        //    Description = "Exampleサーバー",
        //    Url = "http://server/api/v1"
        //});

        // Tags
        foreach (var value in Enum.GetValues<TTag>())
        {
            var name = value.ToString();
            var fi = typeof(TTag).GetField(name);
            var attribute = fi!.GetCustomAttribute<DescriptionAttribute>();
            swaggerDoc.Tags.Add(new OpenApiTag
            {
                Name = name.ToLowerInvariant(),
                Description = attribute?.Description
            });
        }

        // SecurityRequirements
        // TODO ?

        // Remove from scheme
        swaggerDoc.Components.Schemas.Remove(typeof(Credential).FullName!);
    }
}
