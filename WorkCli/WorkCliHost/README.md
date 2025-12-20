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
- ✅ **整理されたフォルダ構造（Core/Samples/Docs）**
- ✅ **フォルダ構造に合わせた名前空間（WorkCliHost.Core/Samples）**

## ⚡ クイックスタート

### インストール

```bash
# プロジェクトをクローン
git clone https://github.com/yourusername/WorkCliHost
cd WorkCliHost
```

### プロジェクト構造

```
WorkCliHost/
├── Core/           # フレームワーク本体 (15ファイル)
│   ├── CliHost.cs
│   ├── CliHostBuilder.cs
│   ├── ICommandDefinition.cs
│   ├── ICommandFilter.cs
│   └── ...
├── Samples/        # サンプル実装 (10ファイル)
│   ├── Program.cs
│   ├── MessageCommand.cs
│   ├── UserCommands.cs
│   └── ...
└── Docs/           # ドキュメント (3ファイル)
    ├── API_DESIGN.md
    ├── TECHNICAL_GUIDE.md
    └── INDEX.md
```

**名前空間**:
- `WorkCliHost.Core` - フレームワーク本体
- `WorkCliHost.Samples` - サンプル実装

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

フレームワークは以下の名前空間で構成されています：

- **`WorkCliHost.Core`** - フレームワークの中核機能
- **`WorkCliHost.Samples`** - サンプル実装

### WorkCliHost.Core（フレームワーク本体）

ライブラリとして使用する際にインポートする名前空間：
フレームワークの中核機能を提供する名前空間：

- `CliHost` - ファクトリメソッド
- `ICliHostBuilder` - ビルダーインターフェース
- `ICommandDefinition` - コマンド定義インターフェース
- `ICommandFilter` - フィルターインターフェース群
- `CliCommandAttribute` - コマンド属性
- `CliArgumentAttribute<T>` - 引数属性
- その他、フレームワーク機能

### WorkCliHost.Samples（サンプル実装）

サンプル実装を含む名前空間。学習や参照用：

- `MessageCommand` - シンプルなコマンド例
- `UserCommand` - 階層的なコマンド例
- `TimingFilter` - フィルター実装例
- その他、サンプルコマンドとフィルター

## 実行例

`Samples/` フォルダに各種サンプルが含まれています：

```bash
dotnet run -- message "Hello, World!"
dotnet run -- user role assign alice admin
dotnet run -- test-filter "Testing filters"
```

## API概要

### ファクトリメソッド

#### CreateBuilder（最小構成版）⭐推奨⭐

```csharp
using WorkCliHost.Core;

var builder = CliHost.CreateBuilder(args);
```

- Console logging のみ
- 高速起動（50-100ms高速化）
- 必要な機能だけを追加可能

#### CreateDefaultBuilder（フル機能版）

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

#### 利用可能なフィルター（WorkCliHost.Core）

- `ICommandExecutionFilter` - コマンド実行の前後で処理
- `IBeforeCommandFilter` - コマンド実行前に処理
- `IAfterCommandFilter` - コマンド実行後に処理
- `IExceptionFilter` - 例外発生時に処理

### サンプル実装（WorkCliHost.Samples）

実装例として以下のフィルターを提供：

- `TimingFilter` - 実行時間の計測
- `LoggingFilter` - ログ出力
- `ExceptionHandlingFilter` - 例外ハンドリング
- `AuthorizationFilter` - 認可チェック
- `ValidationFilter` - 引数検証
- `TransactionFilter` - トランザクション管理
- `CleanupFilter` - クリーンアップ処理

詳細は [API設計ガイド](Docs/API_DESIGN.md) を参照してください。

## 📖 ドキュメント

- **[API設計ガイド](Docs/API_DESIGN.md)** - API設計思想と使い方の完全ガイド
- **[技術解説](Docs/TECHNICAL_GUIDE.md)** - フレームワークの技術詳細・内部実装・プロジェクト構造
- **[ドキュメントインデックス](Docs/INDEX.md)** - 全ドキュメントへのリンクと学習パス
