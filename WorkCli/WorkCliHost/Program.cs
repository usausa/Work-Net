using WorkCliHost;

var builder = CliHost.CreateDefaultBuilder(args);

// アプリケーションサービスの設定（コマンド以外）
builder.ConfigureServices(services =>
{
    // ここにDBコンテキスト、HTTPクライアント、その他のサービスを登録
    // 例: services.AddDbContext<MyDbContext>();
    //     services.AddHttpClient();
    //     services.AddSingleton<IMyService, MyService>();
});

// コマンド関連の設定
builder.ConfigureCommands(commands =>
{
    // RootCommandの設定
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("My Attribute-based CLI tool with Filters");
    });
    
    // グローバルフィルタの追加
    commands.AddGlobalFilter<TimingFilter>(order: -100);
    commands.AddGlobalFilter<ExceptionHandlingFilter>(order: int.MaxValue);
    
    // シンプルなコマンド
    commands.AddCommand<MessageCommand>();
    commands.AddCommand<GreetCommand>();
    
    // 階層的なコマンド構造
    commands.AddCommand<UserCommand>(user =>
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
    commands.AddCommand<ConfigCommand>(config =>
    {
        config.AddSubCommand<ConfigSetCommand>();
        config.AddSubCommand<ConfigGetCommand>();
    });
    
    // 基底クラスでのPosition省略テスト
    commands.AddCommand<DeployCommand>();
    
    // フィルタテストコマンド
    commands.AddCommand<TestFilterCommand>();
    commands.AddCommand<TestExceptionCommand>();
});

var host = builder.Build();
return await host.RunAsync();
