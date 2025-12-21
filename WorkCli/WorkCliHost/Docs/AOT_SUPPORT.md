# AOT対応ガイド

## 概要

WorkCliHost.Coreは将来的なAOT（Ahead-of-Time）コンパイル対応を見据えた設計になっています。

## アーキテクチャ

### 実行フロー

```
1. Command生成 (基本構造)
   ↓
2. CommandActionBuilder呼び出し
   ├─ Argument生成
   └─ CommandActionDelegate生成
   ↓
3. Command.Arguments.Add() (引数追加)
   ↓
4. Command.SetAction() (アクション設定)
   ├─ CommandContext生成
   ├─ コマンドインスタンス生成
   ├─ FilterPipeline取得
   └─ コアアクションをフィルタでラップして実行
   ↓
5. コマンド実行
```

### 役割分担

| 役割 | 実装場所 | カスタマイズ可能 |
|------|---------|----------------|
| **Command生成** | フレームワーク | ❌ 共通処理 |
| **Argument生成** | **ActionBuilder** | **✅ カスタム可能** |
| **Arguments追加** | フレームワーク | ❌ 共通処理 |
| **CommandContext生成** | フレームワーク | ❌ 共通処理 |
| **インスタンス生成** | フレームワーク | ❌ 共通処理（DI） |
| **FilterPipeline取得・実行** | フレームワーク | ❌ 共通処理 |
| **引数値の設定** | **ActionBuilder** | **✅ カスタム可能** |
| **コマンド実行** | **ActionBuilder** | **✅ 委譲** |
| **SetAction** | フレームワーク | ❌ 共通処理 |

## 現在の実装

### リフレクションベース（デフォルト）

現在の実装はリフレクションを使用して引数を生成・設定します：

```csharp
builder.ConfigureCommands(commands =>
{
    // リフレクションで自動的に処理
    commands.AddCommand<MyCommand>();
});
```

**内部動作**:
```csharp
// 1. Command生成（共通）
var command = new Command(name, description);

// 2. ActionBuilder呼び出し（リフレクション）
var (arguments, coreAction) = CreateReflectionBasedActionBuilder()(context);

// 3. 引数追加（共通）
foreach (var arg in arguments)
    command.Arguments.Add(arg);

// 4. SetAction（共通）
command.SetAction(async parseResult =>
{
    var ctx = new CommandContext { ... };          // 共通
    var instance = CreateInstance(...);            // 共通
    
    await filterPipeline.ExecuteAsync(...,         // 共通
        async () => await coreAction(              // ActionBuilder呼び出し
            instance, parseResult, ctx));
    
    return ctx.ExitCode;                           // 共通
});
```

**利点**:
- シンプルな記述
- 属性から自動的に処理

**欠点**:
- AOTコンパイル時にリフレクションメタデータが必要

### カスタムアクションビルダー（AOTフレンドリー）

リフレクションを使わずに引数を生成・設定する方法：

```csharp
builder.ConfigureCommands(commands =>
{
    // カスタムアクションビルダーを指定
    commands.AddCommand<MyCommand>(
        actionBuilder: CreateMyActionBuilder()
    );
});

// カスタムアクションビルダーの実装
static CommandActionBuilder CreateMyActionBuilder()
{
    return context =>
    {
        // 1. 引数を作成
        var nameArg = new Argument<string>("name")
        {
            Description = "User name"
        };
        
        var arguments = new Argument[] { nameArg };
        
        // 2. コアアクションを作成
        CommandActionDelegate coreAction = async (instance, parseResult, commandContext) =>
        {
            // インスタンスは既に生成済み
            var command = (MyCommand)instance;
            
            // 引数値を設定
            command.Name = parseResult.GetValue(nameArg)!;
            
            // コマンド実行
            await instance.ExecuteAsync(commandContext);
        };
        
        return (arguments, coreAction);
    };
}
```

**利点**:
- AOTフレンドリー（リフレクション不要）
- 完全な制御が可能

**欠点**:
- 記述が冗長

## デリゲートとコンテキスト

### CommandActionBuilderContext

```csharp
public sealed class CommandActionBuilderContext
{
    public Type CommandType { get; init; }
    public Command Command { get; init; }
    public IServiceProvider ServiceProvider { get; init; }
}
```

**役割**:
- Builderが必要とする情報を提供
- CommandType: コマンドの型情報
- Command: System.CommandLine.Commandインスタンス
- ServiceProvider: DIコンテナへのアクセス

### CommandActionDelegate

```csharp
public delegate ValueTask CommandActionDelegate(
    ICommandDefinition commandInstance,
    ParseResult parseResult,
    CommandContext commandContext);
```

**役割**:
- 引数値の設定とコマンド実行を行う
- インスタンスは呼び出し側が生成（引数として受け取る）
- CommandContextは呼び出し側が生成（引数として受け取る）
- FilterPipelineは呼び出し側が実行

### CommandActionBuilder

```csharp
public delegate (
    IReadOnlyList<Argument> Arguments,
    CommandActionDelegate Action
) CommandActionBuilder(CommandActionBuilderContext context);
```

**役割**:
- Argumentの生成
- CommandActionDelegateの生成
- 戻り値で両方を返す

## 将来の実装（Source Generator）

### 自動生成されるコード

Source Generatorが以下のコードを自動生成する予定：

```csharp
// [Generated]
namespace WorkCliHost.Generated
{
    public static partial class GeneratedActionBuilders
    {
        public static CommandActionBuilder CreateMyCommandActionBuilder()
        {
            return context =>
            {
                // 1. 引数生成（属性から）
                var nameArg = new Argument<string>("name")
                {
                    Description = "User name"
                };
                
                var arguments = new Argument[] { nameArg };
                
                // 2. コアアクション生成
                CommandActionDelegate action = async (instance, parseResult, commandContext) =>
                {
                    // 型安全なキャスト
                    var command = (MyCommand)instance;
                    
                    // 引数値設定（型安全）
                    command.Name = parseResult.GetValue(nameArg)!;
                    
                    // コマンド実行
                    await instance.ExecuteAsync(commandContext);
                };
                
                return (arguments, action);
            };
        }
    }
}
```

### Interceptorによる差し替え

C# 12のInterceptor機能を使用して、通常の`AddCommand`呼び出しを自動生成されたビルダーに差し替え：

```csharp
// ユーザーコード（そのまま）
builder.ConfigureCommands(commands =>
{
    commands.AddCommand<MyCommand>(); // ← Interceptorが差し替え
});

// Interceptorによる実際の呼び出し
[InterceptsLocation(...)]
static ICommandConfigurator AddCommand_Intercepted<TCommand>(
    this ICommandConfigurator commands)
{
    return commands.AddCommand<TCommand>(
        actionBuilder: GeneratedActionBuilders.CreateMyCommandActionBuilder()
    );
}
```

**結果**:
- ユーザーコードは変更不要
- リフレクション不使用
- AOTフレンドリー

## 設計の利点

### 1. 責任の明確な分離

```
┌─────────────────────────────────────┐
│ フレームワーク（共通処理）            │
├─────────────────────────────────────┤
│ • Command生成                        │
│ • Arguments.Add()                   │
│ • CommandContext生成                 │
│ • インスタンス生成（DI）             │
│ • FilterPipeline実行                 │
│ • SetAction()                       │
└─────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────┐
│ ActionBuilder（カスタマイズ可能）     │
├─────────────────────────────────────┤
│ • Argument生成                       │
│ • 引数値の設定                       │
│ • コマンド実行の委譲                 │
└─────────────────────────────────────┘
```

### 2. インスタンス生成の統一

**Before** (Builderが生成):
```csharp
CommandActionDelegate action = async (parseResult, commandContext) =>
{
    // Builder内でインスタンス生成（カスタマイズ困難）
    var instance = ActivatorUtilities.CreateInstance(...);
    // ...
};
```

**After** (呼び出し側が生成):
```csharp
CommandActionDelegate action = async (instance, parseResult, commandContext) =>
{
    // インスタンスは引数として受け取る（統一的に生成済み）
    var command = (MyCommand)instance;
    // ...
};
```

**利点**:
- すべてのコマンドで同じ方法でインスタンス生成
- DIコンテナの統一的な使用
- デバッグが容易

### 3. FilterPipelineの統一

**Before** (Builder内で実行):
```csharp
// Builder内でFilterPipelineを取得・実行（カスタマイズ困難）
var filterPipeline = context.ServiceProvider.GetRequiredService<FilterPipeline>();
return await filterPipeline.ExecuteAsync(...);
```

**After** (呼び出し側で実行):
```csharp
// 呼び出し側で統一的に実行
await filterPipeline.ExecuteAsync(...,
    async () => await coreAction(instance, parseResult, ctx));
```

**利点**:
- すべてのコマンドで同じ方法でフィルタ実行
- フィルタの動作が保証される
- ActionBuilderはフィルタを意識不要

### 4. CommandContextの統一

**Before** (Builder内で生成):
```csharp
// Builder内でCommandContextを生成（カスタマイズ困難）
var ctx = new CommandContext { ... };
```

**After** (呼び出し側で生成):
```csharp
// 呼び出し側で統一的に生成
var commandContext = new CommandContext
{
    CommandType = commandType,
    CancellationToken = cancellationToken
};
```

**利点**:
- すべてのコマンドで同じCommandContext生成
- フィルタとコマンドで同じコンテキスト
- 状態管理が統一

## 実装ロードマップ

### Phase 1: アーキテクチャ準備 ✅ **完了**

- [x] `CommandActionBuilderContext`の導入
- [x] `CommandActionDelegate`のシグネチャ設計
- [x] `CommandActionBuilder`デリゲートの定義
- [x] インスタンス生成を呼び出し側に移動
- [x] CommandContext生成を呼び出し側に移動
- [x] FilterPipeline実行を呼び出し側に移動
- [x] 統一的な実行フロー

### Phase 2: Source Generator開発（予定）

- [ ] Source Generator プロジェクトの作成
- [ ] 属性からコマンド構造を解析
- [ ] `CommandActionBuilder`実装の自動生成
- [ ] 継承階層の考慮
- [ ] サブコマンドのサポート

### Phase 3: Interceptor統合（予定）

- [ ] Interceptor機能の実装
- [ ] `AddCommand`呼び出しの差し替え
- [ ] ビルド時検証
- [ ] エラーレポート

### Phase 4: NuGetパッケージ化（予定）

- [ ] `WorkCliHost.Core` - 本体
- [ ] `WorkCliHost.SourceGenerator` - Source Generator
- [ ] サンプルとドキュメント

## ベストプラクティス

### 現在（リフレクションベース）

```csharp
// シンプルで十分な場合
builder.ConfigureCommands(commands =>
{
    commands.AddCommand<MyCommand>();
});
```

### AOT対応が必要な場合

```csharp
// カスタムアクションビルダーを使用
builder.ConfigureCommands(commands =>
{
    commands.AddCommand<MyCommand>(
        actionBuilder: MyActionBuilders.CreateMyCommandActionBuilder()
    );
});
```

### 将来（Source Generator使用時）

```csharp
// コードは変更不要（Interceptorが自動的に差し替え）
builder.ConfigureCommands(commands =>
{
    commands.AddCommand<MyCommand>();
});
```

## サンプルコード

### リフレクションベース（デフォルト）

```csharp
[CliCommand("greet", Description = "Greet someone")]
public sealed class GreetCommand : ICommandDefinition
{
    [CliArgument<string>("name")]
    public string Name { get; set; } = default!;
    
    public ValueTask ExecuteAsync(CommandContext context)
    {
        Console.WriteLine($"Hello, {Name}!");
        return ValueTask.CompletedTask;
    }
}

// 登録
builder.ConfigureCommands(commands =>
{
    commands.AddCommand<GreetCommand>(); // リフレクションで自動処理
});
```

### カスタムアクションビルダー

```csharp
// コマンド定義は同じ
[CliCommand("greet", Description = "Greet someone")]
public sealed class GreetCommand : ICommandDefinition
{
    [CliArgument<string>("name")]
    public string Name { get; set; } = default!;
    
    public ValueTask ExecuteAsync(CommandContext context)
    {
        Console.WriteLine($"Hello, {Name}!");
        return ValueTask.CompletedTask;
    }
}

// カスタムアクションビルダー
static CommandActionBuilder CreateGreetActionBuilder()
{
    return context =>
    {
        // 1. 引数作成
        var nameArg = new Argument<string>("name")
        {
            Description = "User name"
        };
        
        var arguments = new Argument[] { nameArg };
        
        // 2. コアアクション
        CommandActionDelegate action = async (instance, parseResult, commandContext) =>
        {
            var command = (GreetCommand)instance;
            command.Name = parseResult.GetValue(nameArg)!;
            await instance.ExecuteAsync(commandContext);
        };
        
        return (arguments, action);
    };
}

// 登録
builder.ConfigureCommands(commands =>
{
    commands.AddCommand<GreetCommand>(
        actionBuilder: CreateGreetActionBuilder() // AOTフレンドリー
    );
});
```

## まとめ

- ✅ 現在の実装でAOT対応の基盤は完成
- ✅ 責任が明確に分離（構造/生成/設定/実行）
- ✅ インスタンス生成は統一的に処理
- ✅ CommandContextは統一的に生成
- ✅ FilterPipeline実行は統一的に処理
- ✅ ActionBuilderは引数とアクションのみに集中
- ⏳ Source Generatorで自動生成を実現（今後）
- 🚀 Interceptorで透過的なAOT対応（今後）
- 📦 ユーザーは移行コスト不要
