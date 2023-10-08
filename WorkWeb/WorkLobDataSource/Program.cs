using System.Data.Common;
using Npgsql;
using Smart.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// data
var connectionString = "Server=db-server;User ID=test;Password=test;Database=test";
builder.Services.AddSingleton<IDbProvider>(p =>
{
    var dataSource = new NpgsqlDataSourceBuilder(connectionString)
        .UseLoggerFactory(p.GetRequiredService<ILoggerFactory>())
        .Build();
    return new DelegateDbProvider(() => dataSource.OpenConnection());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
