# WorkCliHost.Core - 技術解説

## 概要

WorkCliHost.Core は、System.CommandLine をベースにした、属性ベースのCLIホストフレームワークです。ASP.NET Core の設計思想を取り入れ、依存性注入、フィルタパイプライン、プロパティベースAPIなどの機能を提供します。

## 目次

- [クラス・インターフェース一覧](#クラスインターフェース一覧)
- [プロジェクト構造](#プロジェクト構造)
- [アーキテクチャ](#アーキテクチャ)
- [詳細解説](#詳細解説)
  - [ホストビルダー](#ホストビルダー)
  - [コマンド定義](#コマンド定義)
  - [フィルター機構](#フィルター機構)
  - [属性システム](#属性システム)
  - [内部実装](#内部実装)

---

## クラス・インターフェース一覧

### サマリー表

| 分類 | ファイル名 | 型名 | 種類 | 主要メンバー数 | ステップ数(概算) | 説明 |
|------|-----------|------|------|---------------|-----------------|------|
| **ホストビルダー** | | | | | | |
| | CliHost.cs | CliHost | static class | 2 | 20 | ファクトリメソッド提供 |
| | ICliHostBuilder.cs | ICliHostBuilder | interface | 6 | 50 | ビルダーインターフェース |
| | | ICommandConfigurator | interface | 5 | 30 | コマンド設定インターフェース |
| | | ISubCommandConfigurator | interface | 1 | 10 | サブコマンド設定インターフェース |
| | | IRootCommandConfigurator | interface | 4 | 15 | ルートコマンド設定インターフェース |
| | CliHostBuilder.cs | CliHostBuilder | class (internal) | 8 | 350 | ビルダー実装 |
| | | HostEnvironment | class (internal) | 4 | 10 | 環境情報実装 |
| | | LoggingBuilder | class (internal) | 1 | 10 | ロギングビルダー実装 |
| | | CliHostImplementation | class (internal) | 2 | 30 | ホスト実装 |
| | | CliArgumentInfo | record (internal) | 5 | 5 | 引数情報 |
| | ICliHost.cs | ICliHost | interface | 1 | 5 | ホストインターフェース |
| | CliHostBuilderExtensions.cs | CliHostBuilderExtensions | static class | 9 | 130 | 拡張メソッド群 |
| **コマンド定義** | | | | | | |
| | ICommandDefinition.cs | ICommandDefinition | interface | 1 | 5 | 実行可能コマンド |
| | CommandContext.cs | CommandContext | class | 6 | 35 | コマンド実行コンテキスト |
| | CliCommandAttribute.cs | CliCommandAttribute | class | 2 | 10 | コマンド属性 |
| | CliArgumentAttribute.cs | CliArgumentAttribute<T> | class (generic) | 7 | 30 | 引数属性（ジェネリック） |
| | | CliArgumentAttribute | class | 7 | 20 | 引数属性（非ジェネリック） |
| **フィルター機構** | | | | | | |
| | ICommandFilter.cs | ICommandFilter | interface | 1 | 5 | フィルター基底 |
| | | ICommandExecutionFilter | interface | 1 | 5 | 実行フィルター |
| | | IBeforeCommandFilter | interface | 1 | 5 | 実行前フィルター |
| | | IAfterCommandFilter | interface | 1 | 5 | 実行後フィルター |
| | | IExceptionFilter | interface | 1 | 5 | 例外フィルター |
| | | CommandExecutionDelegate | delegate | - | 1 | パイプラインデリゲート |
| | CommandFilterAttribute.cs | CommandFilterAttribute | abstract class | 2 | 10 | フィルター属性（抽象） |
| | | CommandFilterAttribute<TFilter> | class (generic) | 1 | 5 | フィルター属性（ジェネリック） |
| | CommandFilterOptions.cs | CommandFilterOptions | class | 3 | 20 | フィルターオプション |
| | | GlobalFilterDescriptor | class | 2 | 15 | グローバルフィルター記述子 |
| | FilterPipeline.cs | FilterPipeline | class (internal) | 4 | 200 | フィルター実行エンジン |
| | | FilterDescriptor | class (internal) | 3 | 10 | フィルター記述子 |
| **内部実装** | | | | | | |
| | CommandConfigurators.cs | CommandConfigurator | class (internal) | 8 | 80 | コマンド設定実装 |
| | | SubCommandConfigurator | class (internal) | 2 | 30 | サブコマンド設定実装 |
| | | RootCommandConfigurator | class (internal) | 6 | 60 | ルートコマンド設定実装 |
| | | CommandRegistration | class (internal) | 2 | 15 | コマンド登録情報 |
| | ServiceCollectionExtensions.cs | ServiceCollectionExtensions | static class | 0 | 10 | 非推奨拡張メソッド |
| **合計** | **15ファイル** | **31型** | - | **100+** | **~1,160** | |

### 型の分類

#### パブリックインターフェース (11個)
- `ICliHostBuilder` - ビルダー
- `ICliHost` - ホスト
- `ICommandDefinition` - 実行可能コマンド
- `ICommandFilter` - フィルター基底
- `ICommandExecutionFilter` - 実行フィルター
- `IBeforeCommandFilter` - 実行前フィルター
- `IAfterCommandFilter` - 実行後フィルター
- `IExceptionFilter` - 例外フィルター
- `ICommandConfigurator` - コマンド設定
- `ISubCommandConfigurator` - サブコマンド設定
- `IRootCommandConfigurator` - ルートコマンド設定

#### パブリッククラス (9個)
- `CliHost` - ファクトリ
- `CliHostBuilderExtensions` - 拡張メソッド
- `CommandContext` - コンテキスト
- `CliCommandAttribute` - コマンド属性
- `CliArgumentAttribute<T>` - 引数属性（ジェネリック）
- `CliArgumentAttribute` - 引数属性（非ジェネリック）
- `CommandFilterAttribute<T>` - フィルター属性（ジェネリック）
- `CommandFilterAttribute` - フィルター属性（抽象）
- `CommandFilterOptions` - フィルターオプション
- `GlobalFilterDescriptor` - グローバルフィルター記述子

#### 内部クラス (11個)
- `CliHostBuilder` - ビルダー実装
- `HostEnvironment` - 環境情報実装
- `LoggingBuilder` - ロギングビルダー実装
- `CliHostImplementation` - ホスト実装
- `FilterPipeline` - フィルター実行
- `FilterDescriptor` - フィルター記述子
- `CommandConfigurator` - コマンド設定実装
- `SubCommandConfigurator` - サブコマンド設定実装
- `RootCommandConfigurator` - ルートコマンド設定実装
- `CommandRegistration` - コマンド登録情報
- `CliArgumentInfo` - 引数情報（record）

---

## プロジェクト構造

### フォルダ構成

```
WorkCliHost/
├── Core/                          # フレームワーク本体 (15ファイル)
│   ├── CliHost.cs                # ファクトリメソッド
│   ├── CliHostBuilder.cs         # ビルダー実装
│   ├── CliHostBuilderExtensions.cs # ビルダー拡張メソッド
│   ├── ICliHostBuilder.cs        # ビルダーインターフェース
│   ├── ICliHost.cs               # ホストインターフェース
│   ├── CliCommandAttribute.cs    # コマンド属性
│   ├── CliArgumentAttribute.cs   # 引数属性
│   ├── ICommandDefinition.cs     # コマンド定義インターフェース
│   ├── CommandContext.cs         # コマンド実行コンテキスト
│   ├── ICommandFilter.cs         # フィルターインターフェース（全種類）
│   ├── CommandFilterAttribute.cs # フィルター属性
│   ├── CommandFilterOptions.cs   # フィルターオプション
│   ├── FilterPipeline.cs         # フィルターパイプライン実装
│   ├── CommandConfigurators.cs   # コマンド設定クラス群
│   └── ServiceCollectionExtensions.cs # 非推奨拡張メソッド
│
├── Samples/                       # サンプル実装 (10ファイル)
│   ├── Program.cs                # エントリーポイント
│   ├── MessageCommand.cs         # シンプルなコマンド例
│   ├── GreetCommand.cs           # デフォルト値の例
│   ├── UserCommands.cs           # 階層構造の例
│   ├── ConfigCommands.cs         # Position自動決定の例
│   ├── AdvancedCommandPatterns.cs # 高度なパターン例
│   ├── CommonFilters.cs          # 共通フィルター実装
│   ├── AdvancedFilters.cs        # 高度なフィルター実装
│   ├── TestFilterCommands.cs     # フィルターテストコマンド
│   └── Program_Minimal.cs.example # 最小構成版の例
│
└── Docs/                          # ドキュメント (3ファイル)
    ├── API_DESIGN.md             # API設計と使い方
    ├── TECHNICAL_GUIDE.md        # このファイル（技術解説）
    └── INDEX.md                  # ドキュメントインデックス
```

### 名前空間の構成

| 名前空間 | フォルダ | 説明 | ファイル数 |
|---------|---------|------|-----------|
| `WorkCliHost.Core` | Core/ | フレームワーク本体 | 15 |
| `WorkCliHost.Samples` | Samples/ | サンプル実装 | 10 |

**設計原則**:
- フォルダ構造と名前空間が一致
- Core = 再利用可能なライブラリ
- Samples = 使い方の例

**使用方法**:
```csharp
// フレームワーク利用時
using WorkCliHost.Core;

var builder = CliHost.CreateBuilder(args);
builder.ConfigureCommands(commands =>
{
    commands.AddCommand<YourCommand>();
});
```

### Core（フレームワーク本体）

**役割**: 再利用可能なCLIフレームワークの提供

**分類**:
1. **ホストビルダー関連** (5ファイル)
   - ファクトリメソッド、ビルダー、拡張メソッド

2. **コマンド定義関連** (4ファイル)
   - 属性、インターフェース、コンテキスト

3. **フィルター機構関連** (4ファイル)
   - インターフェース、属性、オプション、パイプライン

4. **内部実装** (2ファイル)
   - Configurator、非推奨拡張メソッド

### Samples（サンプル実装）

**役割**: フレームワークの使い方を示す実例

**分類**:
1. **エントリーポイント** (2ファイル)
   - フル機能版、最小構成版

2. **コマンド例** (5ファイル)
   - シンプル、デフォルト値、階層構造、Position自動、継承パターン

3. **フィルター例** (3ファイル)
   - 共通フィルター、高度なフィルター、テストコマンド

### Docs（ドキュメント）

**役割**: 技術文書の提供

**構成**:
- **API_DESIGN.md** - API設計思想と使い方（利用者向け）
- **TECHNICAL_GUIDE.md** - 技術詳細と内部実装（開発者向け）
- **INDEX.md** - ドキュメントインデックスと学習パス

### 分離の利点

#### 1. 再利用性
- Coreフォルダのみをコピーすれば、別プロジェクトで使用可能
- すべての必要なインターフェースがCoreに含まれる

#### 2. 保守性
- フレームワーク本体とサンプルが混在しない
- 変更の影響範囲が明確（Coreの変更はSamplesに影響しない）

#### 3. 学習のしやすさ
- Samplesを見れば使い方がわかる
- Coreを見ればフレームワークの仕組みがわかる

#### 4. 拡張性
- 新しいコマンドやフィルターをSamplesに追加しやすい
- Coreの変更なしに機能追加が可能

### 今後の展開

#### NuGetパッケージ化

```
WorkCliHost.Core/              # NuGetパッケージ
  namespace: WorkCliHost.Core
  
WorkCliHost.Samples/           # サンプルプロジェクト
  namespace: WorkCliHost.Samples
  PackageReference: WorkCliHost.Core
```

#### オプショナルパッケージ

```
WorkCliHost.Filters/           # 共通フィルター集
  ├── AuthenticationFilter.cs  # JWT/OAuth認証
  ├── AuthorizationFilter.cs   # ロールベース認可
  ├── ValidationFilter.cs       # FluentValidation統合
  └── CachingFilter.cs          # 結果のキャッシング
```

---

## アーキテクチャ

### 全体構成

```
┌─────────────────────────────────────────────────┐
│              ユーザーコード                      │
│  (Program.cs, コマンド実装, フィルター実装)      │
└───────────────────┬─────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────┐
│         パブリックAPI (WorkCliHost.Core)         │
├─────────────────────────────────────────────────┤
│ CliHost.CreateBuilder()/CreateDefaultBuilder()  │
│ ICliHostBuilder (Configuration/Services/etc)    │
│ ICommandDefinition, ICommandFilter              │
│ Attributes (CliCommand, CliArgument, etc)       │
└───────────────────┬─────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────┐
│         内部実装 (Internal Classes)              │
├─────────────────────────────────────────────────┤
│ CliHostBuilder - ビルド処理                      │
│ CommandConfigurator - コマンド設定               │
│ FilterPipeline - フィルター実行                  │
└───────────────────┬─────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────┐
│      依存ライブラリ (External Dependencies)      │
├─────────────────────────────────────────────────┤
│ System.CommandLine                              │
│ Microsoft.Extensions.DependencyInjection        │
│ Microsoft.Extensions.Configuration              │
│ Microsoft.Extensions.Logging                    │
└─────────────────────────────────────────────────┘
```

### データフロー

```
1. ビルド時
   CliHost.CreateBuilder()
   → CliHostBuilder生成
   → ConfigureCommands()でコマンド登録
   → Build()でSystem.CommandLine構造を構築

2. 実行時
   host.RunAsync()
   → System.CommandLineでパース
   → ユーザーコマンドインスタンス生成（DI）
   → FilterPipeline実行
     ├─ Before Filters
     ├─ Execution Filters (around)
     ├─ Command.ExecuteAsync()
     └─ After Filters
   → 終了コード返却
```

---

## 詳細解説

### ホストビルダー

#### CliHost

**ファイル**: `CliHost.cs`
**役割**: ファクトリメソッドの提供

```csharp
public static class CliHost
{
    public static ICliHostBuilder CreateDefaultBuilder(string[] args);
    public static ICliHostBuilder CreateBuilder(string[] args);
}
```

**責務**:
- ビルダーインスタンスの作成
- デフォルト設定版と最小構成版の選択

**実装のポイント**:
- `CreateDefaultBuilder()`: appsettings.json、環境変数、Console loggingを自動設定
- `CreateBuilder()`: 最小構成（Console loggingのみ）で高速起動

**使用例**:
```csharp
// フル機能版
var builder = CliHost.CreateDefaultBuilder(args);

// 最小構成版
var builder = CliHost.CreateBuilder(args);
builder.UseDefaults(); // 必要に応じて標準設定を追加
```

---

#### ICliHostBuilder

**ファイル**: `ICliHostBuilder.cs`
**役割**: ビルダーの公開インターフェース

```csharp
public interface ICliHostBuilder
{
    ConfigurationManager Configuration { get; }
    IHostEnvironment Environment { get; }
    IServiceCollection Services { get; }
    ILoggingBuilder Logging { get; }
    
    ICliHostBuilder ConfigureCommands(Action<ICommandConfigurator> configureCommands);
    ICliHost Build();
}
```

**責務**:
- ASP.NET Core の `HostApplicationBuilder` に倣ったプロパティベースAPI
- Configuration、Services、Logging への直接アクセス
- コマンド設定の分離

**設計思想**:
- **プロパティベース**: 従来の `ConfigureXxx()` メソッドではなく、プロパティ経由で直接設定
- **責任分離**: アプリケーションサービス（Services）とCLI設定（Commands）を明確に分離

**使用パターン**:
```csharp
var builder = CliHost.CreateBuilder(args);

// Configuration設定
builder.Configuration.AddJsonFile("config.json");

// サービス登録
builder.Services.AddDbContext<MyDbContext>();

// Logging設定
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// コマンド設定（分離されている）
builder.ConfigureCommands(commands =>
{
    commands.AddCommand<MyCommand>();
});
```

---

#### CliHost

**ファイル**: `CliHostBuilder.cs`
**役割**: ビルダーの実装クラス

**主要メンバー**:
```csharp
internal sealed class CliHostBuilder : ICliHostBuilder
{
    // 8つの主要メソッド
    - コンストラクタ: 初期化とデフォルト設定
    - Build(): System.CommandLine構造の構築
    - CreateCommandWithSubCommands(): 階層的コマンド生成
    - CreateCommand(): 単一コマンド生成
    - CollectPropertiesWithArguments(): 引数収集（継承考慮）
    - CollectFilterTypes(): フィルタ型収集
    - GetCliArgumentAttribute(): 属性取得
    - GetDefaultValue(): デフォルト値決定
}
```

**責務**:
1. **初期化** (`ctor`)
   - `IHostEnvironment` の設定
   - `ConfigurationManager` の初期化
   - デフォルト設定の適用（オプション）
   - 基本サービスの登録

2. **ビルド** (`Build()`)
   - コマンド設定の実行
   - フィルタ型の収集とDI登録
   - System.CommandLine の `RootCommand` 構築
   - サービスプロバイダ生成

3. **コマンド生成** (`CreateCommand()`)
   - 属性からコマンド情報取得
   - 引数のプロパティ収集（継承階層考慮）
   - System.CommandLine の `Argument<T>` 生成
   - コマンド実行時のアクション設定

**実装の特徴**:

##### 1. 継承階層を考慮した引数収集

```csharp
private static List<(PropertyInfo Property, CliArgumentInfo Attribute)> 
    CollectPropertiesWithArguments(Type commandType)
{
    // 基底→派生の順で型階層を走査
    var typeHierarchy = CollectTypeHierarchy(commandType);
    
    // Position指定があれば優先、なければ定義順
    return allProperties
        .OrderBy(p => p.Attribute.Position != AutoPosition 
            ? (0, p.Attribute.Position, 0, 0)
            : (1, 0, p.TypeLevel, p.PropertyIndex))
        .ToList();
}
```

##### 2. フィルタの自動DI登録

```csharp
// グローバルフィルタ + コマンド属性フィルタを収集
var filterTypes = new HashSet<Type>();
foreach (var globalFilter in filterOptions.GlobalFilters)
    filterTypes.Add(globalFilter.FilterType);

foreach (var registration in commandRegistrations)
    CollectFilterTypes(registration.CommandType, filterTypes);

// DIコンテナに登録
foreach (var filterType in filterTypes)
    if (!_services.Any(sd => sd.ServiceType == filterType))
        _services.AddTransient(filterType);
```

##### 3. リフレクションによる引数バインディング

```csharp
command.SetAction(async parseResult =>
{
    // DIでコマンドインスタンス生成
    var instance = ActivatorUtilities.CreateInstance(serviceProvider, commandType);
    
    // 各引数をプロパティに設定
    foreach (var (argument, property, argumentType) in arguments)
    {
        var value = parseResult.GetValue(argument);
        property.SetValue(instance, value);
    }
    
    // フィルタパイプライン経由で実行
    var filterPipeline = serviceProvider.GetRequiredService<FilterPipeline>();
    return await filterPipeline.ExecuteAsync(commandType, instance, CancellationToken.None);
});
```

**パフォーマンス考慮**:
- リフレクションは起動時のみ（実行時は生成済みのデリゲート使用）
- フィルタ型の重複登録を防止（HashSet使用）

---

#### CliHostBuilderExtensions

**ファイル**: `CliHostBuilderExtensions.cs`
**役割**: ビルダーの拡張メソッド群

**提供する拡張メソッド** (9個):

| メソッド | 説明 | ステップ数 |
|---------|------|-----------|
| `UseDefaultConfiguration()` | JSON + 環境変数を追加 | ~15 |
| `UseDefaultLogging()` | Console + Configuration logging | ~20 |
| `UseDefaults()` | 上記2つをまとめて実行 | ~10 |
| `AddJsonFile()` | JSON設定ファイル追加 | ~10 |
| `AddEnvironmentVariables()` | 環境変数追加 | ~15 |
| `AddUserSecrets<T>()` | ユーザーシークレット追加 | ~5 |
| `SetMinimumLogLevel()` | ログレベル設定 | ~5 |
| `AddLoggingFilter()` | ログフィルタ追加 | ~5 |
| `AddDebugLogging()` | デバッグログ追加 | ~5 |

**責務**:
- よく使う設定パターンの簡略化
- オプトイン方式の設定追加

**実装の特徴**:

##### 1. 完全修飾名での拡張メソッド呼び出し

```csharp
public static ICliHostBuilder UseDefaultConfiguration(this ICliHostBuilder builder)
{
    // 名前衝突を避けるため完全修飾名で呼び出し
    Microsoft.Extensions.Configuration.JsonConfigurationExtensions.AddJsonFile(
        builder.Configuration, "appsettings.json", optional: true, reloadOnChange: true);
    
    // ...
}
```

理由: `using` による名前空間汚染を避け、明示的な依存関係を示す。

##### 2. メソッドチェーン対応

```csharp
public static ICliHostBuilder UseDefaults(this ICliHostBuilder builder)
{
    return builder
        .UseDefaultConfiguration()
        .UseDefaultLogging();
}
```

すべての拡張メソッドが `ICliHostBuilder` を返すため、流暢なAPI実現。

**使用例**:
```csharp
var builder = CliHost.CreateBuilder(args)
    .UseDefaultConfiguration()
    .AddJsonFile("custom.json")
    .AddEnvironmentVariables("MYAPP_")
    .SetMinimumLogLevel(LogLevel.Warning)
    .AddDebugLogging();
```

---

### コマンド定義

#### ICommandDefinition

**ファイル**: `ICommandDefinition.cs`
**役割**: 実行可能なコマンドのインターフェース

```csharp
public interface ICommandDefinition
{
    ValueTask ExecuteAsync(CommandContext context);
}
```

**責務**:
- コマンドが実行可能であることのマーカー
- 実行メソッドの定義

**設計思想**:
- シンプルな非同期実行モデル
- `CommandContext` による実行時情報へのアクセス

**実装パターン**:
```csharp
[CliCommand("greet", Description = "Greet someone")]
public sealed class GreetCommand : ICommandDefinition
{
    private readonly ILogger<GreetCommand> _logger;
    
    public GreetCommand(ILogger<GreetCommand> logger)
    {
        _logger = logger;
    }
    
    [CliArgument<string>("name")]
    public string Name { get; set; } = default!;
    
    public ValueTask ExecuteAsync(CommandContext context)
    {
        _logger.LogInformation("Greeting {Name}", Name);
        Console.WriteLine($"Hello, {Name}!");
        return ValueTask.CompletedTask;
    }
}
```

---

#### CommandContext

**ファイル**: `CommandContext.cs`
**役割**: コマンド実行時のコンテキスト情報

```csharp
public sealed class CommandContext
{
    public Type CommandType { get; }
    public ICommandDefinition Command { get; }
    public int ExitCode { get; set; }
    public Dictionary<string, object?> Items { get; }
    public bool IsShortCircuited { get; set; }
    public CancellationToken CancellationToken { get; set; }
}
```

**プロパティ詳細**:

| プロパティ | 型 | 説明 | 用途 |
|-----------|-----|------|------|
| `CommandType` | `Type` | 実行中のコマンド型 | フィルタでの型判定 |
| `Command` | `ICommandDefinition` | コマンドインスタンス | プロパティアクセス |
| `ExitCode` | `int` | 終了コード | フィルタでの制御 |
| `Items` | `Dictionary` | データ共有用 | フィルタ間通信 |
| `IsShortCircuited` | `bool` | 処理中断フラグ | フィルタでの早期終了 |
| `CancellationToken` | `CancellationToken` | キャンセル通知 | 非同期処理制御 |

**責務**:
1. **実行情報の保持**: コマンド型とインスタンス
2. **終了制御**: ExitCode、IsShortCircuited
3. **データ共有**: Items ディクショナリ
4. **キャンセル対応**: CancellationToken

**使用パターン**:

##### 1. フィルタ間でのデータ共有
```csharp
// Filter 1: データ設定
public ValueTask OnBeforeExecutionAsync(CommandContext context)
{
    context.Items["StartTime"] = DateTime.UtcNow;
    context.Items["CorrelationId"] = Guid.NewGuid();
    return ValueTask.CompletedTask;
}

// Filter 2: データ取得
public ValueTask OnAfterExecutionAsync(CommandContext context)
{
    var startTime = (DateTime)context.Items["StartTime"]!;
    var elapsed = DateTime.UtcNow - startTime;
    Console.WriteLine($"Elapsed: {elapsed.TotalMilliseconds}ms");
    return ValueTask.CompletedTask;
}
```

##### 2. 早期終了制御
```csharp
public ValueTask OnBeforeExecutionAsync(CommandContext context)
{
    if (!IsAuthorized())
    {
        context.IsShortCircuited = true;
        context.ExitCode = 403;
        Console.Error.WriteLine("Access denied");
    }
    return ValueTask.CompletedTask;
}
```

---

### フィルター機構

#### ICommandFilter（基底インターフェース）

**ファイル**: `ICommandFilter.cs`
**役割**: すべてのフィルターの基底

```csharp
public interface ICommandFilter
{
    int Order { get; }
}
```

**責務**:
- フィルタ実行順序の定義
- フィルタの共通プロパティ

**Order の動作**:
- 小さい値が先に実行される
- デフォルトは `0`
- 負の値で優先度を上げる（例: `-100`）
- `int.MaxValue` で最後に実行

---

#### ICommandExecutionFilter

**ファイル**: `ICommandFilter.cs`
**役割**: コマンド実行の前後で処理を行うフィルタ

```csharp
public interface ICommandExecutionFilter : ICommandFilter
{
    ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next);
}

public delegate ValueTask CommandExecutionDelegate();
```

**責務**:
- コマンド実行をラップする処理
- ASP.NET Core の `IAsyncActionFilter` 相当

**実装パターン**:
```csharp
public sealed class TimingFilter : ICommandExecutionFilter
{
    public int Order => -100; // 早めに実行
    
    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        var sw = Stopwatch.StartNew();
        
        await next(); // 次のフィルタまたはコマンド実行
        
        sw.Stop();
        Console.WriteLine($"⏱  {sw.ElapsedMilliseconds}ms");
    }
}
```

**`next` デリゲートの役割**:
- パイプライン内の次の処理を呼び出す
- 呼び出さなければ、以降の処理はスキップされる
- 例外ハンドリング、リトライ、キャッシングなどに利用

---

#### IBeforeCommandFilter

**ファイル**: `ICommandFilter.cs`
**役割**: コマンド実行前の処理

```csharp
public interface IBeforeCommandFilter : ICommandFilter
{
    ValueTask OnBeforeExecutionAsync(CommandContext context);
}
```

**責務**:
- 実行前の検証、ロギング、準備処理

**実装例**:
```csharp
public sealed class ValidationFilter : IBeforeCommandFilter
{
    public int Order => -500;
    
    public ValueTask OnBeforeExecutionAsync(CommandContext context)
    {
        if (!IsValid(context.Command))
        {
            context.IsShortCircuited = true;
            context.ExitCode = 400;
            Console.Error.WriteLine("Validation failed");
        }
        return ValueTask.CompletedTask;
    }
}
```

---

#### IAfterCommandFilter

**ファイル**: `ICommandFilter.cs`
**役割**: コマンド実行後の処理

```csharp
public interface IAfterCommandFilter : ICommandFilter
{
    ValueTask OnAfterExecutionAsync(CommandContext context);
}
```

**責務**:
- 実行後のクリーンアップ、ロギング、集計処理

**実装例**:
```csharp
public sealed class CleanupFilter : IAfterCommandFilter
{
    public int Order => 1000; // 遅めに実行
    
    public ValueTask OnAfterExecutionAsync(CommandContext context)
    {
        if (context.Items.TryGetValue("TempFiles", out var files))
        {
            // クリーンアップ処理
            DeleteTempFiles((List<string>)files!);
        }
        return ValueTask.CompletedTask;
    }
}
```

---

#### IExceptionFilter

**ファイル**: `ICommandFilter.cs`
**役割**: 例外ハンドリング

```csharp
public interface IExceptionFilter : ICommandFilter
{
    ValueTask OnExceptionAsync(CommandContext context, Exception exception);
}
```

**責務**:
- 例外のログ記録、変換、エラーメッセージ表示

**実装例**:
```csharp
public sealed class ExceptionHandlingFilter : IExceptionFilter
{
    public int Order => int.MaxValue; // 最後に実行
    
    public ValueTask OnExceptionAsync(CommandContext context, Exception exception)
    {
        context.ExitCode = exception switch
        {
            ArgumentException => 400,
            FileNotFoundException => 404,
            UnauthorizedAccessException => 403,
            _ => 500
        };
        
        Console.Error.WriteLine($"❌ {exception.Message}");
        return ValueTask.CompletedTask;
    }
}
```

---

#### FilterPipeline

**ファイル**: `FilterPipeline.cs`
**役割**: フィルタパイプラインの実行エンジン

**主要メソッド** (4個):
```csharp
internal sealed class FilterPipeline
{
    public ValueTask<int> ExecuteAsync(Type commandType, ICommandDefinition commandInstance, CancellationToken);
    private List<FilterDescriptor> CollectFilters(Type commandType);
    private ValueTask ExecutePipelineAsync(CommandContext, List<FilterDescriptor>, ICommandDefinition);
    private ValueTask HandleExceptionAsync(CommandContext, List<FilterDescriptor>, Exception);
}
```

**責務**:
1. **フィルタ収集**: グローバル + コマンド属性フィルタ
2. **順序決定**: Order プロパティに基づくソート
3. **パイプライン構築**: Execution → Before → Command → After の順
4. **例外処理**: Exception フィルタの実行

**実装の詳細**:

##### 1. フィルタ収集アルゴリズム

```csharp
private List<FilterDescriptor> CollectFilters(Type commandType)
{
    var filters = new List<FilterDescriptor>();
    
    // 1. グローバルフィルタ追加
    foreach (var globalFilter in _options.GlobalFilters)
        filters.Add(new FilterDescriptor(globalFilter.FilterType, globalFilter.Order, isGlobal: true));
    
    // 2. コマンド属性フィルタ追加（継承階層考慮）
    if (_options.IncludeBaseClassFilters)
    {
        // 基底→派生の順で走査
        var typeHierarchy = CollectTypeHierarchy(commandType);
        foreach (var type in typeHierarchy)
        {
            var attributes = type.GetCustomAttributes(typeof(CommandFilterAttribute), inherit: false);
            foreach (var attr in attributes)
                filters.Add(new FilterDescriptor(attr.FilterType, attr.Order, isGlobal: false));
        }
    }
    
    // 3. Order順にソート
    filters.Sort((a, b) => a.Order.CompareTo(b.Order));
    
    return filters;
}
```

##### 2. パイプライン構築

```csharp
private async ValueTask ExecutePipelineAsync(/* ... */)
{
    // フィルタをタイプ別に分類
    var executionFilters = new List<ICommandExecutionFilter>();
    var beforeFilters = new List<IBeforeCommandFilter>();
    var afterFilters = new List<IAfterCommandFilter>();
    
    foreach (var descriptor in filters)
    {
        var filterInstance = _serviceProvider.GetService(descriptor.FilterType);
        
        if (filterInstance is ICommandExecutionFilter execFilter)
            executionFilters.Add(execFilter);
        if (filterInstance is IBeforeCommandFilter beforeFilter)
            beforeFilters.Add(beforeFilter);
        if (filterInstance is IAfterCommandFilter afterFilter)
            afterFilters.Add(afterFilter);
    }
    
    // コアパイプライン: Before → Command → After
    CommandExecutionDelegate pipeline = async () =>
    {
        // Before filters
        foreach (var filter in beforeFilters)
        {
            if (context.IsShortCircuited) break;
            await filter.OnBeforeExecutionAsync(context);
        }
        
        // Command execution
        if (!context.IsShortCircuited)
            await commandInstance.ExecuteAsync(context);
        
        // After filters（逆順）
        for (int i = afterFilters.Count - 1; i >= 0; i--)
            await afterFilters[i].OnAfterExecutionAsync(context);
    };
    
    // Execution filtersでラップ（逆順）
    for (int i = executionFilters.Count - 1; i >= 0; i--)
    {
        var filter = executionFilters[i];
        var next = pipeline;
        pipeline = () => filter.ExecuteAsync(context, next);
    }
    
    // 実行
    await pipeline();
}
```

**実行順序の具体例**:

```
登録されたフィルタ:
  - TimingFilter (Order: -100, ICommandExecutionFilter)
  - AuthFilter (Order: -50, IBeforeCommandFilter)
  - LoggingFilter (Order: 0, ICommandExecutionFilter)
  - CleanupFilter (Order: 100, IAfterCommandFilter)

実行順序:
  1. TimingFilter.ExecuteAsync() 開始
  2.   LoggingFilter.ExecuteAsync() 開始
  3.     AuthFilter.OnBeforeExecutionAsync()
  4.     Command.ExecuteAsync()
  5.     CleanupFilter.OnAfterExecutionAsync()
  6.   LoggingFilter.ExecuteAsync() 終了
  7. TimingFilter.ExecuteAsync() 終了
```

##### 3. 例外ハンドリング

```csharp
private async ValueTask HandleExceptionAsync(/* ... */)
{
    // Exception filtersを収集
    var exceptionFilters = filters
        .Select(d => _serviceProvider.GetService(d.FilterType))
        .OfType<IExceptionFilter>()
        .OrderByDescending(f => f.Order) // Order降順
        .ToList();
    
    // Exception filtersを実行
    foreach (var filter in exceptionFilters)
        await filter.OnExceptionAsync(context, exception);
    
    // ExitCodeが設定されていなければデフォルト値
    if (context.ExitCode == 0)
        context.ExitCode = 1;
}
```

---

### 属性システム

#### CliCommandAttribute

**ファイル**: `CliCommandAttribute.cs`
**役割**: コマンドの定義

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CliCommandAttribute : Attribute
{
    public string Name { get; }
    public string? Description { get; set; }
    
    public CliCommandAttribute(string name)
    {
        Name = name;
    }
}
```

**責務**:
- コマンド名の定義
- 説明文の設定

**使用例**:
```csharp
[CliCommand("user", Description = "User management commands")]
public sealed class UserCommand : ICommandGroup
{
}

[CliCommand("add", Description = "Add a new user")]
public sealed class UserAddCommand : ICommandDefinition
{
    // ...
}
```

---

#### CliArgumentAttribute

**ファイル**: `CliArgumentAttribute.cs`
**役割**: コマンド引数の定義

**ジェネリック版**:
```csharp
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class CliArgumentAttribute<T> : Attribute
{
    public const int AutoPosition = -1;
    
    public int Position { get; }
    public string Name { get; }
    public string? Description { get; set; }
    public bool IsRequired { get; set; } = true;
    public T? DefaultValue { get; set; }
    
    // Position省略可能
    public CliArgumentAttribute(string name);
    
    // Position明示指定
    public CliArgumentAttribute(int position, string name);
}
```

**非ジェネリック版**:
```csharp
public sealed class CliArgumentAttribute : Attribute
{
    // ジェネリック版と同じ構造（DefaultValueなし）
}
```

**責務**:
1. **引数の識別**: Name、Position
2. **型安全性**: ジェネリック `<T>` でデフォルト値の型チェック
3. **必須/オプション**: IsRequired フラグ
4. **デフォルト値**: DefaultValue（ジェネリック版のみ）

**Position の動作**:
- **明示指定**: `[CliArgument<string>(0, "name")]`
- **自動決定**: `[CliArgument<string>("name")]` → AutoPosition (-1)
- 自動決定時は、基底クラス→派生クラス、プロパティ定義順

**使用パターン**:

##### 1. 基本的な使用
```csharp
[CliArgument<string>(0, "username", Description = "User name")]
public string Username { get; set; } = default!;

[CliArgument<string>(1, "email", Description = "Email address")]
public string Email { get; set; } = default!;
```

##### 2. Position省略（自動決定）
```csharp
[CliArgument<string>("key", Description = "Configuration key")]
public string Key { get; set; } = default!;

[CliArgument<string>("value", Description = "Configuration value")]
public string Value { get; set; } = default!;
```

##### 3. デフォルト値
```csharp
[CliArgument<string>("greeting", DefaultValue = "Hello", IsRequired = false)]
public string Greeting { get; set; } = default!;

[CliArgument<int>("count", DefaultValue = 1, IsRequired = false)]
public int Count { get; set; }
```

##### 4. 継承階層での使用
```csharp
// 基底クラス
public abstract class UserCommandBase : ICommandDefinition
{
    [CliArgument<string>("username")]
    public string Username { get; set; } = default!;
    
    public abstract ValueTask ExecuteAsync(CommandContext context);
}

// 派生クラス
[CliCommand("add", Description = "Add user")]
public sealed class UserAddCommand : UserCommandBase
{
    [CliArgument<string>("email")]
    public string Email { get; set; } = default!;
    
    public override ValueTask ExecuteAsync(CommandContext context)
    {
        // Username（基底）、Email（派生）の順で引数が設定される
        Console.WriteLine($"Adding {Username} ({Email})");
        return ValueTask.CompletedTask;
    }
}
```

---

#### CommandFilterAttribute

**ファイル**: `CommandFilterAttribute.cs`
**役割**: コマンドにフィルタを適用

**抽象基底クラス**:
```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public abstract class CommandFilterAttribute : Attribute
{
    public int Order { get; set; }
    public abstract Type FilterType { get; }
}
```

**ジェネリック版**:
```csharp
public sealed class CommandFilterAttribute<TFilter> : CommandFilterAttribute
    where TFilter : ICommandFilter
{
    public override Type FilterType => typeof(TFilter);
}
```

**責務**:
- コマンド固有のフィルタ指定
- フィルタ実行順序の指定
- 型安全なフィルタ指定

**使用例**:
```csharp
[CommandFilter<TimingFilter>(Order = -100)]
[CommandFilter<LoggingFilter>]
[CommandFilter<AuthorizationFilter>(Order = -1000)]
[CliCommand("secure", Description = "Secure command")]
public sealed class SecureCommand : ICommandDefinition
{
    // このコマンドには3つのフィルタが適用される
}
```

**継承との関係**:
```csharp
[CommandFilter<BaseFilter>]
public abstract class BaseCommand : ICommandDefinition
{
    // ...
}

[CommandFilter<DerivedFilter>]
[CliCommand("derived", Description = "Derived command")]
public sealed class DerivedCommand : BaseCommand
{
    // BaseFilter と DerivedFilter の両方が適用される
}
```

---

### 内部実装

#### CommandConfigurators

**ファイル**: `CommandConfigurators.cs`
**役割**: コマンド設定の内部実装クラス群

**含まれるクラス** (6個):
1. `CommandConfigurator` - メイン設定クラス
2. `SubCommandConfigurator` - サブコマンド設定
3. `RootCommandConfigurator` - ルートコマンド設定
4. `CommandRegistration` - コマンド登録情報
5. （その他内部用クラス）

##### CommandConfigurator

```csharp
internal sealed class CommandConfigurator : ICommandConfigurator
{
    public ICommandConfigurator AddCommand<TCommand>(Action<ISubCommandConfigurator>? configure);
    public ICommandConfigurator AddGlobalFilter<TFilter>(int order);
    public ICommandConfigurator AddGlobalFilter(Type filterType, int order);
    public ICommandConfigurator ConfigureRootCommand(Action<IRootCommandConfigurator> configure);
    public ICommandConfigurator ConfigureFilterOptions(Action<CommandFilterOptions> configure);
}
```

**責務**:
- コマンドの登録
- グローバルフィルタの登録
- ルートコマンドの設定
- フィルタオプションの設定

**内部データ構造**:
```csharp
private readonly List<CommandRegistration> _commandRegistrations = new();
private readonly CommandFilterOptions _filterOptions = new();
private Action<RootCommand>? _rootCommandConfiguration;
private RootCommand? _customRootCommand;
```

##### CommandRegistration

```csharp
internal sealed class CommandRegistration
{
    public Type CommandType { get; }
    public List<CommandRegistration> SubCommands { get; }
    
    public CommandRegistration(Type commandType)
    {
        CommandType = commandType;
        SubCommands = new List<CommandRegistration>();
    }
}
```

**責務**:
- コマンドの階層構造を表現
- 再帰的なサブコマンド管理

**データ構造の例**:
```
CommandRegistration (UserCommand)
  ├─ SubCommands[0]: CommandRegistration (UserListCommand)
  ├─ SubCommands[1]: CommandRegistration (UserAddCommand)
  └─ SubCommands[2]: CommandRegistration (UserRoleCommand)
       ├─ SubCommands[0]: CommandRegistration (UserRoleAssignCommand)
       └─ SubCommands[1]: CommandRegistration (UserRoleRemoveCommand)
```

---

#### ServiceCollectionExtensions

**ファイル**: `ServiceCollectionExtensions.cs`
**役割**: 非推奨の拡張メソッド（後方互換性）

```csharp
public static class ServiceCollectionExtensions
{
    // 注: AddCliCommandとAddGlobalCommandFilterは削除されました
    // 新しいAPIでは、ICommandConfigurator経由でコマンドとフィルタを追加します
}
```

**現在の状態**:
- 空のクラス
- コメントで新しいAPIへの移行方法を記載
- 後方互換性のために残存

**旧API（削除済み）**:
```csharp
// 旧方式（削除済み）
services.AddCliCommand<MyCommand>();
services.AddGlobalCommandFilter<MyFilter>();
```

**新API**:
```csharp
// 新方式
builder.ConfigureCommands(commands =>
{
    commands.AddCommand<MyCommand>();
    commands.AddGlobalFilter<MyFilter>();
});
```

---

## 付録

### ステップ数詳細

| ファイル | 行数（概算） | 備考 |
|---------|-------------|------|
| CliHost.cs | 20 | ファクトリメソッドのみ |
| ICliHostBuilder.cs | 95 | インターフェース定義 + コメント |
| CliHostBuilder.cs | 350 | 最大のクラス、リフレクション処理含む |
| ICliHost.cs | 5 | シンプルなインターフェース |
| CliHostBuilderExtensions.cs | 130 | 9つの拡張メソッド |
| ICommandDefinition.cs | 5 | シンプルなインターフェース |
| CommandContext.cs | 35 | データクラス + コンストラクタ |
| CliCommandAttribute.cs | 10 | シンプルな属性 |
| CliArgumentAttribute.cs | 50 | 2つのバージョン（ジェネリック/非ジェネリック） |
| ICommandFilter.cs | 60 | 5つのインターフェース定義 |
| CommandFilterAttribute.cs | 20 | 抽象 + ジェネリック版 |
| CommandFilterOptions.cs | 30 | オプションクラス + 記述子 |
| FilterPipeline.cs | 200 | フィルタ実行ロジック |
| CommandConfigurators.cs | 250 | 複数の設定クラス |
| **合計** | **~1,275行** | |

### パフォーマンス特性

| 処理 | タイミング | コスト | 備考 |
|------|-----------|--------|------|
| ビルダー生成 | 起動時 | O(1) | 軽量 |
| コマンド登録 | 起動時 | O(n) | n = コマンド数 |
| フィルタ収集 | 起動時 | O(m) | m = フィルタ数 |
| リフレクション | 起動時 | O(n×p) | p = プロパティ数/コマンド |
| DI登録 | 起動時 | O(n+m) | |
| System.CommandLine構築 | 起動時 | O(n) | |
| **起動時合計** | | **O(n×p + m)** | 通常は数十ms |
| コマンド実行 | 実行時 | O(f) | f = アクティブフィルタ数 |
| フィルタ実行 | 実行時 | O(f) | パイプライン構築済み |
| **実行時合計** | | **O(f)** | 通常は数ms |

### メモリ使用量

| データ | サイズ（概算） | 備考 |
|-------|---------------|------|
| ビルダーインスタンス | 1KB | ServiceCollection含む |
| コマンド登録情報 | 100B × n | n = コマンド数 |
| フィルタ記述子 | 50B × m | m = フィルタ数 |
| System.CommandLine構造 | 1KB × n | コマンドツリー |
| **起動時合計** | **~数KB** | 小規模アプリ想定 |

---

## まとめ

WorkCliHost.Core は以下の特徴を持つ、堅牢で拡張性の高いCLIフレームワークです：

### 設計原則
1. **型安全性**: ジェネリック属性による型チェック
2. **分離**: ビルダーパターンによる設定の分離
3. **拡張性**: フィルタパイプラインによる横断的関心事の実装
4. **パフォーマンス**: リフレクションは起動時のみ、実行時はデリゲート使用
5. **DI統合**: Microsoft.Extensions.DependencyInjection との完全な統合

### 実装の特徴
- 約1,200行のコンパクトな実装
- 20以上の型で機能を提供
- System.CommandLine との薄いラッパー
- ASP.NET Core の設計思想を踏襲

### 利用シーン
- 小〜中規模のCLIツール
- エンタープライズCLIアプリケーション
- マイクロサービスの管理ツール
- DevOpsスクリプト

このフレームワークは、.NET開発者にとって馴染みのあるパターンを採用し、学習コストを最小限に抑えながら、強力な機能を提供します。
