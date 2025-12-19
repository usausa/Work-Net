# CLI Host Framework

System.CommandLineを使用した、属性ベースのCLIホストフレームワークです。

## 特徴

- ✅ **属性ベースの宣言的なコマンド定義**
- ✅ **階層的なコマンド構造のサポート**（サブサブコマンドまで無制限）
- ✅ **依存性注入（DI）のサポート**
- ✅ **型安全なジェネリック属性**
- ✅ **デフォルト値のサポート**
- ✅ **自動ヘルプ生成**
- ✅ **グループコマンドの自動ヘルプ表示**
- ✅ **共通引数の柔軟な定義パターン**
- ✅ **Position自動決定（省略可能）**
- ✅ **ASP.NET Coreライクなフィルタ機構**
- ✅ **明確な責任分離（サービス vs コマンド設定）**

## 基本的な使い方

### 1. プログラムのセットアップ

```csharp
using WorkCliHost;

var builder = CliHost.CreateDefaultBuilder(args);

// アプリケーションサービスの設定（コマンド以外）
builder.ConfigureServices(services =>
{
    services.AddDbContext<MyDbContext>();
    services.AddHttpClient();
    services.AddSingleton<IMyService, MyService>();
});

// コマンド関連の設定
builder.ConfigureCommands(commands =>
{
    // RootCommandの設定
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("My CLI Application")
            .WithName("mycli");
    });
    
    // グローバルフィルタの追加
    commands.AddGlobalFilter<TimingFilter>(order: -100);
    commands.AddGlobalFilter<LoggingFilter>();
    
    // コマンドの追加
    commands.AddCommand<MessageCommand>();
    commands.AddCommand<UserCommand>(user =>
    {
        user.AddSubCommand<UserListCommand>();
        user.AddSubCommand<UserAddCommand>();
    });
});

var host = builder.Build();
return await host.RunAsync();
```

### 2. シンプルなコマンド

実行可能なコマンドは`ICommandDefinition`を実装します：

```csharp
[CliCommand("message", Description = "Show message")]
public sealed class MessageCommand : ICommandDefinition
{
    private readonly ILogger<MessageCommand> _logger;

    public MessageCommand(ILogger<MessageCommand> logger)
    {
        _logger = logger;
    }

    [CliArgument<string>("text", Description = "Text to show")]
    public string Text { get; set; } = default!;

    public ValueTask ExecuteAsync(CommandContext context)
    {
        _logger.LogInformation("Show {Text}", Text);
        Console.WriteLine(Text);
        return ValueTask.CompletedTask;
    }
}
```

### 3. グループコマンド（サブコマンドのみ）

サブコマンドのみを持つグループコマンドは`ICommandGroup`を実装します。
サブコマンドを指定せずに実行すると、自動的にヘルプが表示されます：

```csharp
[CliCommand("user", Description = "User management commands")]
public sealed class UserCommand : ICommandGroup
{
    // 実装不要 - サブコマンドのみのグループコマンド
}
```

### 4. 階層的なコマンド構造の登録

```csharp
builder.ConfigureCommands(commands =>
{
    commands.AddCommand<UserCommand>(user =>
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
```

## API設計の特徴

### 明確な責任分離

#### ConfigureServices - アプリケーションサービス
コマンド実装で使用するサービス（DBコンテキスト、HTTPクライアント、ビジネスロジック等）を登録：

```csharp
builder.ConfigureServices(services =>
{
    services.AddDbContext<AppDbContext>();
    services.AddHttpClient<IApiClient, ApiClient>();
    services.AddSingleton<IEmailService, EmailService>();
});
```

#### ConfigureCommands - コマンド設定
CLI固有の設定（コマンド、フィルタ、ルートコマンド）を登録：

```csharp
builder.ConfigureCommands(commands =>
{
    // RootCommand設定
    commands.ConfigureRootCommand(root => { });
    
    // グローバルフィルタ
    commands.AddGlobalFilter<TimingFilter>();
    
    // コマンド登録
    commands.AddCommand<MyCommand>();
});
```

### 型安全な設定API

`ICommandConfigurator`経由でのみコマンド関連の設定が可能：

```csharp
public interface ICommandConfigurator
{
    ICommandConfigurator AddCommand<TCommand>(...);
    ICommandConfigurator AddGlobalFilter<TFilter>(...);
    ICommandConfigurator ConfigureRootCommand(...);
    ICommandConfigurator ConfigureFilterOptions(...);
}
```

## Position自動決定

`CliArgumentAttribute`の`Position`パラメータを省略できます：

```csharp
[CliCommand("set", Description = "Set configuration value")]
public sealed class ConfigSetCommand : ICommandDefinition
{
    // Position省略 - プロパティ定義順で自動決定
    [CliArgument<string>("key", Description = "Configuration key")]
    public string Key { get; set; } = default!;

    [CliArgument<string>("value", Description = "Configuration value")]
    public string Value { get; set; } = default!;

    [CliArgument<string>("environment", Description = "Target environment", 
        IsRequired = false, DefaultValue = "development")]
    public string Environment { get; set; } = default!;

    public ValueTask ExecuteAsync(CommandContext context)
    {
        Console.WriteLine($"Set {Key}={Value} for '{Environment}'");
        return ValueTask.CompletedTask;
    }
}
```

## フィルタ機構

ASP.NET Coreライクなフィルタ機構をサポート：

### フィルタの定義

```csharp
public sealed class LoggingFilter : ICommandExecutionFilter
{
    private readonly ILogger<LoggingFilter> _logger;

    public LoggingFilter(ILogger<LoggingFilter> logger)
    {
        _logger = logger;
    }

    public int Order => 0;

    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        _logger.LogInformation("Before: {CommandType}", context.CommandType.Name);
        await next();
        _logger.LogInformation("After: {CommandType}", context.CommandType.Name);
    }
}
```

### フィルタの適用

**グローバル**（全コマンドに適用）:
```csharp
builder.ConfigureCommands(commands =>
{
    commands.AddGlobalFilter<TimingFilter>(order: -100);
    commands.AddGlobalFilter<LoggingFilter>();
});
```

**コマンド個別**:
```csharp
[CommandFilter<TimingFilter>(Order = -100)]
[CommandFilter<LoggingFilter>]
[CliCommand("process", Description = "Process data")]
public sealed class ProcessCommand : ICommandDefinition
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        Console.WriteLine("Processing...");
        return ValueTask.CompletedTask;
    }
}
```

## 共通引数の定義パターン

### パターン1: 抽象基底クラス（推奨）

```csharp
public abstract class UserRoleCommandBase : ICommandDefinition
{
    [CliArgument<string>("username", Description = "Username")]
    public string Username { get; set; } = default!;

    [CliArgument<string>("role", Description = "Role name")]
    public string Role { get; set; } = default!;

    public abstract ValueTask ExecuteAsync(CommandContext context);
}

[CliCommand("assign", Description = "Assign role to user")]
public sealed class UserRoleAssignCommand : UserRoleCommandBase
{
    public override ValueTask ExecuteAsync(CommandContext context)
    {
        Console.WriteLine($"Assigned role '{Role}' to user '{Username}'");
        return ValueTask.CompletedTask;
    }
}
```

## コマンドライン使用例

```bash
# ヘルプ表示
app --help

# シンプルなコマンド
app message "Hello, World!"

# デフォルト値を使用
app config set database.host localhost
# Key=database.host, Value=localhost, Environment=development

# サブコマンド
app user list 5

# サブサブコマンド
app user role assign john admin

# フィルタ付きコマンド
app test-filter "Hello!"
⏱  Command executed in 114ms
```

## インターフェース

### ICommandDefinition

実行可能なコマンド：

```csharp
public interface ICommandDefinition
{
    ValueTask ExecuteAsync(CommandContext context);
}
```

### ICommandGroup

サブコマンドのみを持つグループコマンド：

```csharp
public interface ICommandGroup
{
}
```

### ICommandConfigurator

コマンド設定用のconfigurator：

```csharp
public interface ICommandConfigurator
{
    ICommandConfigurator AddCommand<TCommand>(...);
    ICommandConfigurator AddGlobalFilter<TFilter>(...);
    ICommandConfigurator ConfigureRootCommand(...);
    ICommandConfigurator ConfigureFilterOptions(...);
}
```

## 設計の利点

1. **明確な責任分離**: サービス設定とコマンド設定が分離
2. **型安全性**: 専用のconfigurator経由でのみ設定可能
3. **一貫性**: コマンド関連の設定が1か所に集約
4. **発見可能性**: IntelliSenseで利用可能なメソッドが明確
5. **拡張性**: 新しい設定メソッドを追加しやすい
6. **自動化**: グループコマンドのヘルプ表示、Position決定、フィルタDI登録
7. **保守性**: ASP.NET Coreと同様の設計思想
