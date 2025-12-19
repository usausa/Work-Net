using WorkCliHost;

var builder = CliHost.CreateDefaultBuilder(args);

builder.ConfigureCommands(root =>
{
    root.Description = "My Attribute-based CLI tool";
});

builder.ConfigureServices(services =>
{
    // シンプルなコマンド
    services.AddCliCommand<MessageCommand>();
    services.AddCliCommand<GreetCommand>();

    // 階層的なコマンド構造
    services.AddCliCommand<UserCommand>(user =>
    {
        user.AddSubCommand<UserListCommand>();
        user.AddSubCommand<UserAddCommand>();
        user.AddSubCommand<UserRoleCommand>(role =>
        {
            role.AddSubCommand<UserRoleAssignCommand>();
            role.AddSubCommand<UserRoleRemoveCommand>();
        });
    });
});

var host = builder.Build();
return await host.RunAsync();
