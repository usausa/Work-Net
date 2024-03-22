namespace WorkSwagger.Application.Swagger;

using System.Diagnostics;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using WorkSwagger.Application.Authentication;

public sealed class CustomDocumentFilter : IDocumentFilter
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
        swaggerDoc.Tags.Add(new OpenApiTag
        {
            Name = Tags.Data1,
            Description = "Data1業務",
            ExternalDocs = new OpenApiExternalDocs
            {
                Description = "Data1業務の詳細情報",
                Url = new Uri("http://example/docs")
            }
        });
        swaggerDoc.Tags.Add(new OpenApiTag
        {
            Name = Tags.Data2,
            Description = "Data2業務"
            //ExternalDocs = new OpenApiExternalDocs
            //{
            //    Description = "Data2業務の詳細情報",
            //    Url = new Uri("http://example/docs")
            //}
        });

        // SecurityRequirements
        // TODO ?

        // Remove from scheme
        swaggerDoc.Components.Schemas.Remove(typeof(Credential).FullName!);
    }
}
