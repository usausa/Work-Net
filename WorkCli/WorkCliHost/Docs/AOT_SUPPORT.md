# AOT対応ガイド

## 概要

WorkCliHost.Coreは将来的なAOT（Ahead-of-Time）コンパイル対応を見据えた設計になっています。

## 現在の実装

### リフレクションベース（デフォルト）

現在の実装はリフレクションを使用してコマンドを構築します：

```csharp
builder.ConfigureCommands(commands =>
{
    // リフレクションで自動的にコマンドを構築
    commands.AddCommand<MyCommand>();
});
```

**利点**:
- シンプルな記述
- 属性から自動的にコマンド構造を生成

**欠点**:
- AOTコンパイル時にリフレクションメタデータが必要
- Native AOTでは動作しない可能性

### カスタムビルダー（AOTフレンドリー）

リフレクションを使わずにコマンドを構築する方法も提供：

```csharp
builder.ConfigureCommands(commands =>
{
    // カスタムビルダーを指定
    commands.AddCommand<MyCommand>(
        builder: CreateMyCommandBuilder()
    );
});

// カスタムビルダーの実装
static CommandBuilder CreateMyCommandBuilder()
{
    return (commandType, serviceProvider) =>
    {
        var command = new Command("my", "My command");
        
        // 引数を手動で追加
        var arg = new Argument<string>("name");
        command.AddArgument(arg);
        
        // アクションを設定
        command.SetAction(async parseResult =>
        {
            var instance = (MyCommand)ActivatorUtilities
                .CreateInstance(serviceProvider, commandType);
            
            instance.Name = parseResult.GetValue(arg)!;
            
            var filterPipeline = serviceProvider
                .GetRequiredService<FilterPipeline>();
            
            return await filterPipeline.ExecuteAsync(
                commandType, instance, CancellationToken.None);
        });
        
        return command;
    };
}
```

**利点**:
- AOTフレンドリー（リフレクション不要）
- 完全な制御が可能

**欠点**:
- 記述が冗長

## 将来の実装（Source Generator）

### 自動生成されるコード

Source Generatorが以下のコードを自動生成する予定：

```csharp
// [Generated]
namespace WorkCliHost.Generated
{
    public static partial class GeneratedCommandBuilders
    {
        public static CommandBuilder CreateMyCommandBuilder()
        {
            return (commandType, serviceProvider) =>
            {
                var command = new Command("my", "My command");
                var arg = new Argument<string>("name", "User name");
                command.AddArgument(arg);
                
                command.SetAction(async parseResult =>
                {
                    var instance = (MyCommand)ActivatorUtilities
                        .CreateInstance(serviceProvider, commandType);
                    instance.Name = parseResult.GetValue(arg)!;
                    
                    var filterPipeline = serviceProvider
                        .GetRequiredService<FilterPipeline>();
                    return await filterPipeline.ExecuteAsync(
                        commandType, instance, CancellationToken.None);
                });
                
                return command;
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
static ICommandConfigurator AddCommand_Intercepted(
    this ICommandConfigurator commands)
{
    return commands.AddCommand<MyCommand>(
        builder: GeneratedCommandBuilders.CreateMyCommandBuilder()
    );
}
```

**結果**:
- ユーザーコードは変更不要
- リフレクション不使用
- AOTフレンドリー

## 実装ロードマップ

### Phase 1: アーキテクチャ準備 ✅ **完了**

- [x] `CommandBuilder`デリゲートの導入
- [x] `AddCommand`/`AddSubCommand`のオーバーロード
- [x] `CommandBuilderHelpers`の提供

### Phase 2: Source Generator開発（予定）

- [ ] Source Generator プロジェクトの作成
- [ ] 属性からコマンド構造を解析
- [ ] `CommandBuilder`実装の自動生成
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
// カスタムビルダーを使用
builder.ConfigureCommands(commands =>
{
    commands.AddCommand<MyCommand>(
        builder: MyCommandBuilders.CreateMyCommandBuilder()
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

## まとめ

- ✅ 現在の実装でAOT対応の基盤は完成
- ⏳ Source Generatorで自動生成を実現（今後）
- 🚀 Interceptorで透過的なAOT対応（今後）
- 📦 ユーザーは移行コスト不要
