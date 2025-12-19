using WorkCliHost;

var builder = CliHost.CreateDefaultBuilder(args);

builder.ConfigureCommands(root =>
{
    root.Description = "My Attribute-based CLI tool with Filters";
});

builder.ConfigureServices(services =>
{
    // グローバルフィルタを登録
    services.AddGlobalCommandFilter<TimingFilter>(order: -100);
    services.AddGlobalCommandFilter<ExceptionHandlingFilter>(order: int.MaxValue);

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
            role.AddSubCommand<UserRoleVerifyCommand>();
        });
        user.AddSubCommand<UserPermissionGrantCommand>();
    });

    // Position省略のテスト
    services.AddCliCommand<ConfigCommand>(config =>
    {
        config.AddSubCommand<ConfigSetCommand>();
        config.AddSubCommand<ConfigGetCommand>();
    });

    // 基底クラスでのPosition省略テスト
    services.AddCliCommand<DeployCommand>();

    // フィルタテストコマンド
    services.AddCliCommand<TestFilterCommand>();
    services.AddCliCommand<TestExceptionCommand>();
});

var host = builder.Build();
return await host.RunAsync();
