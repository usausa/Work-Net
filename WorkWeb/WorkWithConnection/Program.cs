using Microsoft.AspNetCore.Connections;

using WorkWithConnection;
using WorkWithConnection.Settings;

var builder = WebApplication.CreateBuilder(args);

var commandSetting = builder.Configuration.GetRequiredSection("Command").Get<CommandSetting>()!;

//builder.WebHost.UseKestrel(options =>
//{
//    options.ListenLocalhost(5000);
//    options.ListenLocalhost(5001, builder =>
//    {
//        builder.UseHttps();
//    });

//    options.ListenLocalhost(commandSetting.Port, config =>
//    {
//        config.UseConnectionHandler<CommandHandler>();
//    });
//});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<CommandWorker>();

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
