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
    
    void ConfigureContainer<TContainerBuilder>(
        IServiceProviderFactory<TContainerBuilder> factory,
        Action<TContainerBuilder>? configure = null)
        where TContainerBuilder : notnull;
    
    ICliHostBuilder ConfigureCommands(Action<ICommandConfigurator> configure);
    ICliHost Build();
}
```

**責務**:
- ASP.NET Core の `HostApplicationBuilder` に倣ったプロパティベースAPI
- Configuration、Services、Logging への直接アクセス
- カスタムDIコンテナのサポート
- コマンド設定の分離

**設計思想**:
- **プロパティベース**: 従来の `ConfigureXxx()` メソッドではなく、プロパティ経由で直接設定
- **責任分離**: アプリケーションサービス（Services）とCLI設定（Commands）を明確に分離
- **拡張性**: `ConfigureContainer`でDIコンテナを差し替え可能

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

##### 2. カスタムDIコンテナのサポート

```csharp
// ConfigureContainerでカスタムファクトリを設定
public void ConfigureContainer<TContainerBuilder>(
    IServiceProviderFactory<TContainerBuilder> factory,
    Action<TContainerBuilder>? configure = null)
{
    _serviceProviderFactory = factory;
    _containerConfiguration = configure;
}

// Build時にファクトリを使用
public ICliHost Build()
{
    // ...サービス登録...
    
    IServiceProvider serviceProvider;
    if (_serviceProviderFactory != null)
    {
        // カスタムファクトリを使用（リフレクション経由）
        var containerBuilder = CreateBuilder(_services);
        _containerConfiguration?.Invoke(containerBuilder);
        serviceProvider = CreateServiceProvider(containerBuilder);
    }
    else
    {
        // デフォルト
        serviceProvider = _services.BuildServiceProvider();
    }
    
    // ...
}
```

**利点**:
- Autofac、DryIoc、Grace等のサードパーティDIコンテナを使用可能
- エンタープライズアプリケーションでの高度なDI機能を活用

##### 3. フィルタの自動DI登録とパイプライン構築

```csharp
// フィルタ型の収集とDI登録
var filterTypes = new HashSet<Type>();
CollectFilterTypes(commandType, filterTypes);
foreach (var filterType in filterTypes)
{
    _services.AddTransient(filterType);
}

// パイプライン構築（ASP.NET Coreパターン）
CommandExecutionDelegate pipeline = ctx => commandInstance.ExecuteAsync(ctx);

for (int i = executionFilters.Count - 1; i >= 0; i--)
{
    var filter = executionFilters[i];
    var next = pipeline;
    pipeline = ctx => filter.ExecuteAsync(ctx, next);
}

await pipeline(context);
```

**利点**:
- ASP.NET Coreのミドルウェアパターンと一貫性
- `CommandContext`を明示的に引数で渡すため、パイプライン内での状態変更が可能
- クロージャによるキャプチャがないため、メモリ効率が良い

---

### フィルター機構

#### ICommandFilter

**ファイル**: `ICommandFilter.cs`
**役割**: フィルター基底インターフェース

```csharp
public interface ICommandFilter
{
    // フィルター実行メソッド（何らかの処理）
}
```

#### ICommandExecutionFilter

**ファイル**: `ICommandExecutionFilter.cs`
**役割**: 実行フィルターインターフェース

```csharp
public interface ICommandExecutionFilter : ICommandFilter
{
    ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next);
}
```

**責務**:
- コマンド実行の前後に処理を挿入
- 次のフィルターまたはコマンド本体を呼び出す制御

**使用例**:
```csharp
public class MyLoggingFilter : ICommandExecutionFilter
{
    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        Console.WriteLine("Before executing command");
        
        // 次のフィルター/コマンドを実行
        await next(context);
        
        Console.WriteLine("After executing command");
    }
}
```

#### CommandFilterAttribute

**ファイル**: `CommandFilterAttribute.cs`
**役割**: フィルター属性（抽象クラス）

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public abstract class CommandFilterAttribute : Attribute, ICommandFilter
{
    // フィルターとしてのメタデータ
}
```

**使用例**:

```csharp
[CommandFilter]
public class MyFilter : ICommandFilter
{
    // フィルターの実装
}
```

#### CommandFilterOptions

**ファイル**: `CommandFilterOptions.cs`
**役割**: フィルターオプション設定クラス

```csharp
public class CommandFilterOptions
{
    public bool EnableGlobalFilters { get; set; } = true;
    public bool IncludeBaseClassFilters { get; set; } = true;
}
```

**責務**:
- フィルター機能のグローバル設定
- 基底クラスのフィルターを含めるかどうか

---

#### FilterPipeline

**ファイル**: `FilterPipeline.cs`
**役割**: フィルター実行エンジン

**責務**:
1. **フィルタ収集**: グローバル + コマンド属性フィルタ
2. **順序決定**: Order プロパティに基づくソート
3. **パイプライン構築**: フィルタのネストされたデリゲート構造を構築（ASP.NET Coreパターン）
4. **実行**: パイプラインの実行

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

##### 2. パイプライン構築（ASP.NET Coreパターン）

```csharp
private async ValueTask ExecutePipelineAsync(CommandContext context, List<FilterDescriptor> filters, ICommandDefinition commandInstance)
{
    var executionFilters = new List<ICommandExecutionFilter>();
    
    // フィルタインスタンスを作成
    foreach (var descriptor in filters)
    {
        var filterInstance = _serviceProvider.GetService(descriptor.FilterType);
        if (filterInstance is ICommandExecutionFilter execFilter)
        {
            executionFilters.Add(execFilter);
        }
    }
    
    // パイプラインの中心: コマンド実行
    // CommandContextを引数で受け取る（ASP.NET Coreパターン）
    CommandExecutionDelegate pipeline = ctx => commandInstance.ExecuteAsync(ctx);
    
    // Execution filtersでラップ（逆順でラップして正順で実行）
    for (int i = executionFilters.Count - 1; i >= 0; i--)
    {
        var filter = executionFilters[i];
        var next = pipeline;
        pipeline = ctx => filter.ExecuteAsync(ctx, next);
    }
    
    // パイプライン実行
    await pipeline(context);
}
```

**実行順序の具体例**:

```
登録されたフィルタ:
  - AuthFilter (Order: -1000, ICommandExecutionFilter)
  - ValidationFilter (Order: -500, ICommandExecutionFilter)
  - TimingFilter (Order: -100, ICommandExecutionFilter)
  - LoggingFilter (Order: 0, ICommandExecutionFilter)
  - ExceptionHandlingFilter (Order: int.MaxValue, ICommandExecutionFilter)

実行順序:
  1. ExceptionHandlingFilter.ExecuteAsync(context, next) 開始 (try)
  2.   LoggingFilter.ExecuteAsync(context, next) 開始
  3.     TimingFilter.ExecuteAsync(context, next) 開始
  4.       ValidationFilter.ExecuteAsync(context, next) 開始
  5.         AuthFilter.ExecuteAsync(context, next) 開始
  6.           Command.ExecuteAsync(context)
  7.         AuthFilter.ExecuteAsync(context, next) 終了
  8.       ValidationFilter.ExecuteAsync(context, next) 終了
  9.     TimingFilter.ExecuteAsync(context, next) 終了
  10.   LoggingFilter.ExecuteAsync(context, next) 終了
  11. ExceptionHandlingFilter.ExecuteAsync(context, next) 終了 (catch if exception)
```

**パイプライン構築のメカニズム**:
1. 最初に `pipeline = ctx => commandInstance.ExecuteAsync(ctx)` を設定
2. フィルタを**逆順**でラップ: 最後のフィルタ → 最初のフィルタ
3. 各フィルタが前のデリゲートを `next` として受け取る
4. 各デリゲートは `CommandContext` を引数として受け取る（ASP.NET Coreパターン）
5. 結果として、実行時は**正順**で実行される

**ASP.NET Coreとの類似性**:
```csharp
// ASP.NET Core ミドルウェア
public delegate Task RequestDelegate(HttpContext context);
RequestDelegate pipeline = ctx => /* 処理 */;

// WorkCliHost.Core フィルタ
public delegate ValueTask CommandExecutionDelegate(CommandContext context);
CommandExecutionDelegate pipeline = ctx => commandInstance.ExecuteAsync(ctx);
