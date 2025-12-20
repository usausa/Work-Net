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
- ✅ **最小構成とフル機能版の選択可能**

## 基本的な使い方

### 1. プログラムのセットアップ（最小構成）

```csharp
using Microsoft.Extensions.DependencyInjection;
using WorkCliHost;

// 最小構成版（高速起動）
var builder = CliHost.CreateBuilder(args);

// サービスの追加
builder.Services.AddSingleton<IMyService, MyService>();

// コマンド設定
builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("My CLI Application");
    });
    
    commands.AddCommand<MessageCommand>();
});

var host = builder.Build();
return await host.RunAsync();
```

### 2. プログラムのセットアップ（フル機能版）

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkCliHost;

// フル機能版（従来通り）
var builder = CliHost.CreateDefaultBuilder(args);
// デフォルトで以下が設定済み：
// - appsettings.json
// - appsettings.{Environment}.json
// - 環境変数
// - Console logging

// または最小構成版に標準設定を追加
var builder = CliHost.CreateBuilder(args);
builder.UseDefaults(); // 上記の標準設定をすべて追加

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

## ファクトリメソッド

### CreateBuilder（最小構成版）

```csharp
var builder = CliHost.CreateBuilder(args);
```

**含まれる設定:**
- Console logging のみ

**利点:**
- 高速起動（50-100ms高速化）
- 必要な機能だけを追加可能

**使用ケース:**
- シンプルなCLIツール
- 起動速度が重要なケース

### CreateDefaultBuilder（フル機能版）

```csharp
var builder = CliHost.CreateDefaultBuilder(args);
```

**含まれる設定:**
- `appsettings.json`（optional）
- `appsettings.{Environment}.json`（optional）
- 環境変数
- Console logging

**使用ケース:**
- 複雑なアプリケーション
- 設定ファイルを使用するケース

## 拡張メソッドによるカスタマイズ

```csharp
var builder = CliHost.CreateBuilder(args);

// 標準設定を追加
builder.UseDefaultConfiguration();  // JSON + 環境変数
builder.UseDefaultLogging();         // Console + Configuration
// または
builder.UseDefaults();               // 上記2つをまとめて

// 個別に設定を追加
builder
    .AddJsonFile("settings.json", optional: true)
    .AddEnvironmentVariables("MYAPP_")
    .AddUserSecrets<Program>()
    .SetMinimumLogLevel(LogLevel.Warning)
    .AddLoggingFilter("Microsoft", LogLevel.Error)
    .AddDebugLogging();
```

## シンプルなコマンド

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

## レビュー結果と改善点

### 修正された問題

1. **ConfigurationManager.AddCommandLine の競合**
   - ❌ 問題: `System.CommandLine` とコマンドライン引数の解析が競合
   - ✅ 解決: `AddCommandLine()` の呼び出しを完全削除

2. **不要な初期化処理**
   - ❌ 問題: シンプルなCLIでも常にJSON、環境変数を読み込み
   - ✅ 解決: `CreateBuilder()` で最小構成版を提供

詳細は [REVIEW_RESULTS.md](REVIEW_RESULTS.md) を参照してください。

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
6. **パフォーマンス**: 最小構成版で高速起動
7. **柔軟性**: 拡張メソッドでカスタマイズ可能
8. **保守性**: 統一されたAPI設計

## ドキュメント

- [プロパティベースAPI](PROPERTY_BASED_API.md) - Configuration、Environment、Services、Loggingの詳細
- [新しいAPI設計](NEW_API_DESIGN.md) - 責任分離と型安全性
- [レビュー結果](REVIEW_RESULTS.md) - レビューで発見された問題と解決策
- [フィルタ機構](FILTER_IMPLEMENTATION.md) - フィルタの設計と実装

## パッケージ要件

```xml
<PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.1" />
<PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.1" />
<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="10.0.1" />
<PackageReference Include="Microsoft.Extensions.Logging.Configuration" Version="10.0.1" />
<PackageReference Include="Microsoft.Extensions.Logging.Console" Version="10.0.1" />
<PackageReference Include="System.CommandLine" Version="2.0.1" />
