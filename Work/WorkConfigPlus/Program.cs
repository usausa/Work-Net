using System.Diagnostics;

using WorkConfigPlus;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args
});

builder.Configuration
    .AddJsonFile("options.json");

var array = builder.Configuration.GetSection("Data").Get<Data[]>()!;
foreach (var data in array)
{
    Debug.WriteLine($"{data.Id} : {data.Name}");
}

var host = builder.Build();
host.Run();
