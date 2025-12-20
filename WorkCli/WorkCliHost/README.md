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
- ✅ **HostApplicationBuilder風のプロパティベースAPI**

## 基本的な使い方

### 1. プログラムのセットアップ

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkCliHost;

var builder = CliHost.CreateDefaultBuilder(args);

// Configuration - プロパティ経由で直接アクセス
builder.Configuration.AddJsonFile("custom-settings.json", optional: true);

// Environment - ホスト環境情報
Console.WriteLine($"Application: {builder.Environment.ApplicationName}");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");

// Logging - ログ設定
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Services - DIコンテナへのサービス登録
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddHttpClient<IApiClient, ApiClient>();
builder.Services.AddSingleton<IMyService, MyService>();

// Commands - コマンド設定
builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("My CLI Application");
    });
    
    commands.AddGlobalFilter<TimingFilter>();
    commands.AddCommand<MessageCommand>();
    commands.AddCommand<UserCommand>(user =>
    {
        user.AddSubCommand<UserListCommand>();
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

## プロパティベースAPI

`ICliHostBuilder`は`Microsoft.Extensions.Hosting.HostApplicationBuilder`と同様のプロパティベースAPIをサポート：

```csharp
public interface ICliHostBuilder
{
    ConfigurationManager Configuration { get; }  // Configuration管理
    IHostEnvironment Environment { get; }        // ホスト環境情報
    IServiceCollection Services { get; }         // DIコンテナ
    ILoggingBuilder Logging { get; }             // Logging設定
    
    ICliHostBuilder ConfigureCommands(...);      // コマンド設定
    ICliHost Build();                            // ビルド
}
```

### Configuration

```csharp
// JSON設定ファイル
builder.Configuration.AddJsonFile("appsettings.json", optional: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

// 環境変数とコマンドライン（デフォルトで設定済み）
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

// 設定値の取得
var apiKey = builder.Configuration["ApiKey"];
```

### Environment

```csharp
// ホスト環境情報へのアクセス
Console.WriteLine(builder.Environment.ApplicationName);  // "WorkCliHost"
Console.WriteLine(builder.Environment.EnvironmentName);  // "Production" / "Development"
Console.WriteLine(builder.Environment.ContentRootPath);  // アプリのベースディレクトリ

// 環境チェック
if (builder.Environment.IsDevelopment())
{
    // 開発環境でのみ実行
}
```

### Services

```csharp
// DIコンテナへのサービス登録
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();

// DbContext
builder.Services.AddDbContext<AppDbContext>();

// HttpClient
builder.Services.AddHttpClient<IApiClient, ApiClient>();
```

### Logging

```csharp
// ログプロバイダーの追加
builder.Logging.AddConsole();  // デフォルトで追加済み
builder.Logging.AddDebug();

// 最小ログレベル
builder.Logging.SetMinimumLevel(LogLevel.Information);

// フィルター
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
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

    public ValueTask ExecuteAsync(CommandContext context)
    {
        Console.WriteLine($"Set {Key}={Value}");
        return ValueTask.CompletedTask;
    }
}
```

## フィルタ機構

ASP.NET Coreライクなフィルタ機構をサポート：

### グローバルフィルタ

```csharp
builder.ConfigureCommands(commands =>
{
    commands.AddGlobalFilter<TimingFilter>(order: -100);
    commands.AddGlobalFilter<LoggingFilter>();
    commands.AddGlobalFilter<ExceptionHandlingFilter>(order: int.MaxValue);
});
```

### コマンド個別フィルタ

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

## 共通引数の定義

抽象基底クラスによる共通引数の定義（推奨）：

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

# サブコマンド
app user list 5

# サブサブコマンド
app user role assign john admin

# フィルタ付きコマンド
app test-filter "Hello!"
⏱  Command executed in 114ms
```

## 設計の利点

1. **ASP.NET Coreとの一貫性**: `HostApplicationBuilder`と同様のAPI
2. **プロパティベース**: 直感的で発見しやすい
3. **明確な責任分離**: Services（アプリケーションサービス）とCommands（CLI設定）
4. **型安全性**: コンパイル時の型チェック
5. **拡張性**: 新機能の追加が容易
6. **自動化**: Position決定、フィルタDI登録など
7. **保守性**: 統一されたAPI設計

## ドキュメント

- [プロパティベースAPI](PROPERTY_BASED_API.md) - Configuration、Environment、Services、Loggingの詳細
- [新しいAPI設計](NEW_API_DESIGN.md) - 責任分離と型安全性
- [フィルタ機構](FILTERS.md) - フィルタの設計と実装
- [Position自動決定](POSITION_AUTO.md) - Position省略機能

## パッケージ要件

```xml
<PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.1" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.1" />
<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="10.0.1" />
<PackageReference Include="System.CommandLine" Version="2.0.1" />
