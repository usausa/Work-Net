using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkCliHost.Core;
using WorkCliHost.Samples;

// 最小構成版を使用（高速起動）
var builder = CliHost.CreateBuilder(args);

// 必要に応じて標準設定を追加
// builder.UseDefaults(); // または個別に UseDefaultConfiguration() と UseDefaultLogging()

// Environment情報
Console.WriteLine($"Application: {builder.Environment.ApplicationName}");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");

// サービスの追加
builder.Services.AddSingleton<IMyCustomService, MyCustomService>();

// コマンド設定
builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("My Attribute-based CLI tool with Filters");
    });

    commands.AddGlobalFilter<TimingFilter>(order: -100);
    commands.AddGlobalFilter<ExceptionHandlingFilter>(order: int.MaxValue);

    commands.AddCommand<MessageCommand>();
    commands.AddCommand<GreetCommand>();

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

    commands.AddCommand<ConfigCommand>(config =>
    {
        config.AddSubCommand<ConfigSetCommand>();
        config.AddSubCommand<ConfigGetCommand>();
    });

    commands.AddCommand<DeployCommand>();
    commands.AddCommand<TestFilterCommand>();
    commands.AddCommand<TestExceptionCommand>();
});

var host = builder.Build();
return await host.RunAsync();

// ダミーサービス
public interface IMyCustomService { }
public class MyCustomService : IMyCustomService { }
