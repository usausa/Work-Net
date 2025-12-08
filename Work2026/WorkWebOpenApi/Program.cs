using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();
// [MEMO] Custom
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // ドキュメント情報の設定
        document.Info.Title = "My API";
        document.Info.Version = "v1";
        document.Info.Description = "説明をここに書く";
        document.Info.Contact = new OpenApiContact
        {
            Name = "Team",
            Email = "team@example.com"
        };
        document.Servers!.Add(new OpenApiServer
        {
            Url = "https://api.example.com",
            Description = "Production"
        });

        // 認証スキームの追加例
        //var bearerScheme = new OpenApiSecurityScheme
        //{
        //    Type = SecuritySchemeType.Http,
        //    Scheme = "bearer",
        //    BearerFormat = "JWT",
        //    Description = "JWT Authorization header using the Bearer scheme."
        //};
        //document.Components.SecuritySchemes["Bearer"] = bearerScheme;
        //...

        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        operation.Description = "Custom operation description";

        // context.ApiDescriptionからController情報が取れるのでそれを使用する
        // 情報の書き換え
        operation.Summary = "サマリ";
        operation.Description = "Custom operation description";
        operation.Responses ??= new OpenApiResponses();
        operation.Responses["200"].Description = "成功";

        // ヘッダパラメータの追加
        operation.Parameters ??= new List<IOpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Correlation-ID",
            In = ParameterLocation.Header,
            Required = false,
            Description = "トレース用"
        });

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // [MEMO] Add yaml support
    app.MapOpenApi("/openapi/{documentName}.yaml");

    // Enable Swagger UI to use MapOpenApi generated specification
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "My API v1");
    });
}

app.UseAuthorization();

app.MapControllers();

app.Run();
