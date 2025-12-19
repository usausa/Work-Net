using Microsoft.Extensions.DependencyInjection;

namespace WorkCliHost;

public static class ServiceCollectionExtensions
{
    // 注: AddCliCommandとAddGlobalCommandFilterは削除されました
    // 新しいAPIでは、ICommandConfigurator経由でコマンドとフィルタを追加します
    // 
    // 使用例:
    // builder.ConfigureCommands(commands =>
    // {
    //     commands.AddCommand<MyCommand>();
    //     commands.AddGlobalFilter<MyFilter>();
    // });
}
