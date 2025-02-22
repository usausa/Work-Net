using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WorkHost2;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<App>();
builder.Services.AddHostedService<WpfHostedService<App>>();
builder.Services.AddSingleton<MainWindow>();

//Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
//Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

var host = builder.Build();
host.Run();
