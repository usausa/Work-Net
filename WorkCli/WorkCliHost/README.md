# CLI Host Framework

System.CommandLineを使用した、属性ベースのCLIホストフレームワークです。

## フォルダ構造

```
WorkCliHost/
├── Core/        # ライブラリコア（フレームワーク本体）- namespace: WorkCliHost.Core
├── Samples/     # サンプル実装 - namespace: WorkCliHost.Samples
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
- ✅ **フォルダ構造に合わせた名前空間（WorkCliHost.Core/Samples）**

## クイックスタート

### 最小構成版

```csharp
using WorkCliHost.Core;

var builder = CliHost.CreateBuilder(args);

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

### コマンドの定義

```csharp
using Microsoft.Extensions.Logging;
using WorkCliHost.Core;

namespace MyApp.Commands;

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

### フィルターの実装

```csharp
using WorkCliHost.Core;

namespace MyApp.Filters;

public sealed class TimingFilter : ICommandExecutionFilter
{
    public int Order => -100;

    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        await next();
        
        stopwatch.Stop();
        Console.WriteLine($"⏱  Command executed in {stopwatch.ElapsedMilliseconds}ms");
    }
}
```

## 名前空間

### WorkCliHost.Core

フレームワークの中核機能を提供する名前空間：

- `CliHost` - ファクトリメソッド
- `ICliHostBuilder` - ビルダーインターフェース
- `ICommandDefinition` - コマンド定義インターフェース
- `ICommandFilter` - フィルターインターフェース群
- `CliCommandAttribute` - コマンド属性
- `CliArgumentAttribute<T>` - 引数属性
- その他、フレームワーク機能

### WorkCliHost.Samples

サンプル実装を含む名前空間：

- `MessageCommand` - シンプルなコマンド例
- `UserCommand` - 階層的なコマンド例
- `TimingFilter` - フィルター実装例
- その他、サンプルコマンドとフィルター

## サンプル

`Samples/` フォルダに各種サンプルが含まれています。詳細は [Samples/](Samples/) を参照してください。

実行例：

```bash
dotnet run -- message "Hello, World!"
dotnet run -- user role assign alice admin
dotnet run -- test-filter "Testing filters"
```

## ファクトリメソッド

### CreateBuilder（最小構成版）

```csharp
using WorkCliHost.Core;

var builder = CliHost.CreateBuilder(args);
```

- Console logging のみ
- 高速起動（50-100ms高速化）
- 必要な機能だけを追加可能

### CreateDefaultBuilder（フル機能版）

```csharp
using WorkCliHost.Core;

var builder = CliHost.CreateDefaultBuilder(args);
```

- appsettings.json
- 環境変数
- Console logging
- すべて設定済み

## 拡張メソッド

```csharp
using WorkCliHost.Core;

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

### 利用可能なフィルターインターフェース（WorkCliHost.Core）

- `ICommandExecutionFilter` - コマンド実行の前後で処理
- `IBeforeCommandFilter` - コマンド実行前に処理
- `IAfterCommandFilter` - コマンド実行後に処理
- `IExceptionFilter` - 例外発生時に処理

### サンプル実装（WorkCliHost.Samples）

- `TimingFilter` - 実行時間の計測
- `LoggingFilter` - ログ出力
- `ExceptionHandlingFilter` - 例外ハンドリング
- `AuthorizationFilter` - 認可チェック
- `ValidationFilter` - 引数検証
- `TransactionFilter` - トランザクション管理
- `CleanupFilter` - クリーンアップ処理

## ドキュメント

### 📚 ドキュメントインデックス

すべてのドキュメントの一覧は [Docs/INDEX.md](Docs/INDEX.md) を参照してください。

### 主要ドキュメント

#### 技術解説
- [技術解説](Docs/TECHNICAL_GUIDE.md) - Core ライブラリの詳細、クラス一覧、実装解説

#### 設計ドキュメント
- [新しいAPI設計](Docs/NEW_API_DESIGN.md) - 責任分離と型安全性に関する設計
- [プロパティベースAPI](Docs/PROPERTY_BASED_API.md) - Configuration、Environment、Services、Logging

#### 構造・整理
- [フォルダ構造](Docs/FOLDER_STRUCTURE.md) - プロジェクトの構成
- [名前空間の整理](Docs/NAMESPACE_REORGANIZATION.md) - 名前空間の変更内容

#### レビュー・問題解決
- [レビュー結果](Docs/REVIEW_RESULTS.md) - レビューで発見された問題と解決策

#### その他
- [フォルダ整理サマリー](Docs/FOLDER_REORGANIZATION_SUMMARY.md) - 整理の経緯
- [Filtersフォルダの削除](Docs/FILTERS_FOLDER_CLEANUP.md) - Filtersフォルダの削除理由

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
