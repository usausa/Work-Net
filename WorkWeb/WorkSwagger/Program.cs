using WorkSwagger.Application;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
});

// Swagger
builder.ConfigureSwaggerDefaults();

var app = builder.Build();

// Swagger
app.UseSwaggerDefaults();

app.UseAuthorization();

app.MapControllers();

app.Run();
