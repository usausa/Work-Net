# CLI Host Framework

System.CommandLineを使用した、属性ベースのCLIホストフレームワークです。

## フォルダ構造

```
WorkCliHost/
├── Core/        # ライブラリコア（フレームワーク本体）
├── Samples/     # サンプル実装
└── Docs/        # ドキュメント
```

詳細は [Docs/FOLDER_STRUCTURE.md](Docs/FOLDER_STRUCTURE.md) を参照してください。

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
- ✅ **整理されたフォルダ構造（Core/Samples/Docs）**

## クイックスタート

### 最小構成版

```csharp
using Microsoft.Extensions.DependencyInjection;
using WorkCliHost;

var builder = CliHost.CreateBuilder(args);

builder.Services.AddSingleton<IMyService, MyService>();

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

### フル機能版

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkCliHost;

var builder = CliHost.CreateDefaultBuilder(args);

builder.Services.AddDbContext<AppDbContext>();

builder.ConfigureCommands(commands =>
{
    commands.AddGlobalFilter<TimingFilter>();
    commands.AddCommand<UserCommand>(user =>
    {
        user.AddSubCommand<UserListCommand>();
    });
});

var host = builder.Build();
return await host.RunAsync();
```

### コマンドの定義

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

## サンプル

`Samples/` フォルダに各種サンプルが含まれています：

### コマンドサンプル
- **MessageCommand.cs** - 最もシンプルなコマンド
- **GreetCommand.cs** - デフォルト値を持つコマンド
- **UserCommands.cs** - 階層的なコマンド構造
- **ConfigCommands.cs** - Position自動決定
- **AdvancedCommandPatterns.cs** - 基底クラスを使った共通引数

### フィルターサンプル
- **CommonFilters.cs** - `TimingFilter`, `LoggingFilter`, `ExceptionHandlingFilter`
- **AdvancedFilters.cs** - `AuthorizationFilter`, `ValidationFilter`, `TransactionFilter`, `CleanupFilter`
- **TestFilterCommands.cs** - フィルターテストコマンド

### その他
- **Program.cs** - メインのサンプルアプリケーション
- **Program_Minimal.cs.example** - 最小構成版の例

実行例：

```bash
dotnet run -- message "Hello, World!"
dotnet run -- user role assign alice admin
dotnet run -- test-filter "Testing filters"
```

## ファクトリメソッド

### CreateBuilder（最小構成版）

```csharp
var builder = CliHost.CreateBuilder(args);
```

- Console logging のみ
- 高速起動（50-100ms高速化）
- 必要な機能だけを追加可能

### CreateDefaultBuilder（フル機能版）

```csharp
var builder = CliHost.CreateDefaultBuilder(args);
```

- appsettings.json
- 環境変数
- Console logging
- すべて設定済み

## 拡張メソッド

```csharp
var builder = CliHost.CreateBuilder(args);

builder
    .UseDefaultConfiguration()      // JSON + 環境変数
    .UseDefaultLogging()             // Console + Configuration
    .AddJsonFile("settings.json")
    .AddEnvironmentVariables("APP_")
    .AddUserSecrets<Program>()
    .SetMinimumLogLevel(LogLevel.Warning)
    .AddDebugLogging();
```

## フィルター機構

### 利用可能なフィルターインターフェース（Core）

- `ICommandExecutionFilter` - コマンド実行の前後で処理
- `IBeforeCommandFilter` - コマンド実行前に処理
- `IAfterCommandFilter` - コマンド実行後に処理
- `IExceptionFilter` - 例外発生時に処理

### サンプル実装（Samples）

- `TimingFilter` - 実行時間の計測
- `LoggingFilter` - ログ出力
- `ExceptionHandlingFilter` - 例外ハンドリング
- `AuthorizationFilter` - 認可チェック
- `ValidationFilter` - 引数検証
- `TransactionFilter` - トランザクション管理
- `CleanupFilter` - クリーンアップ処理

## ドキュメント

- [フォルダ構造](Docs/FOLDER_STRUCTURE.md) - プロジェクトの構成
- [プロパティベースAPI](Docs/PROPERTY_BASED_API.md) - Configuration、Environment、Services、Logging
- [新しいAPI設計](Docs/NEW_API_DESIGN.md) - 責任分離と型安全性
- [レビュー結果](Docs/REVIEW_RESULTS.md) - レビューで発見された問題と解決策
- [フォルダ整理サマリー](Docs/FOLDER_REORGANIZATION_SUMMARY.md) - 整理の経緯

## パッケージ要件

```xml
<PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.1" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.1" />
<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="10.0.1" />
<PackageReference Include="System.CommandLine" Version="2.0.1" />
```

完全なパッケージリストは [WorkCliHost.csproj](WorkCliHost.csproj) を参照してください。

## ライセンス

MIT License

## 今後の予定

- NuGetパッケージとしての公開を検討
- Core フォルダの別プロジェクト化
- より多くのサンプルとフィルター実装
