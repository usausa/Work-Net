using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using WorkCliHost;

var builder = CliHost.CreateDefaultBuilder(args);

// Configuration（プロパティ経由で直接アクセス可能）
Microsoft.Extensions.Configuration.JsonConfigurationExtensions.AddJsonFile(
    builder.Configuration, "custom-settings.json", optional: true);

// Environment情報
Console.WriteLine($"Application: {builder.Environment.ApplicationName}");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");

// Logging設定
Microsoft.Extensions.Logging.DebugLoggerFactoryExtensions.AddDebug(builder.Logging);
builder.Logging.SetMinimumLevel(LogLevel.Information);

// アプリケーションサービスの設定（プロパティ経由）
builder.Services.AddSingleton<IMyCustomService, MyCustomService>();
// 例: builder.Services.AddDbContext<AppDbContext>();
//     builder.Services.AddHttpClient();

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

// ダミーサービスインターフェース
public interface IMyCustomService { }
public class MyCustomService : IMyCustomService { }
