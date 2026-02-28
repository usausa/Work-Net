using WorkStorage.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Add S3-compatible response header
app.Use(async (context, next) =>
{
    context.Response.Headers["x-amz-request-id"] = Guid.NewGuid().ToString("N");
    await next();
});

// Apply per-bucket CORS rules stored via the S3 PutBucketCors API
app.UseMiddleware<S3CorsMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
