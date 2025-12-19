using WorkCliHost;

var builder = CliHost.CreateDefaultBuilder(args);

builder.ConfigureCommands(root =>
{
    root.Description = "My Attribute-based CLI tool";
});

builder.ConfigureServices(services =>
{
    services.AddCliCommand<MessageCommand>();
    services.AddCliCommand<GreetCommand>();
});

var host = builder.Build();
return await host.RunAsync();
