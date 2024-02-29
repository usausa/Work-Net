using WorkCustomConfig;
using WorkCustomConfig.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddCustomConfiguration(config =>
{
});

builder.Services.AddCustomConfigurationOperator();

//var setting = builder.Configuration.GetSection("Sub").Get<SubSetting>()!;

builder.Services.Configure<SubSetting>(builder.Configuration.GetSection("Sub"));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
