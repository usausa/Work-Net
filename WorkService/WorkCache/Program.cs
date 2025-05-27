var builder = WebApplication.CreateBuilder(args);

//--------------------------------------------------------------------------------
// Add services to the container.
//--------------------------------------------------------------------------------

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = "TestInstance";
});

var app = builder.Build();

//--------------------------------------------------------------------------------
// Configure the HTTP request pipeline.
//--------------------------------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(static o => o.SwaggerEndpoint("/openapi/v1.json", "v1"));
}

app.UseAuthorization();

app.MapControllers();

app.Run();
