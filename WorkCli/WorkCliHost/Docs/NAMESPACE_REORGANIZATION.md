# 名前空間の変更 - 完了サマリー

## 実施内容

フォルダ構造に合わせて、すべてのクラスの名前空間を変更しました。

## 変更された名前空間

### Core フォルダ（`WorkCliHost` → `WorkCliHost.Core`）

以下のファイルの名前空間を `WorkCliHost.Core` に変更：

1. **ホストビルダー関連**
   - `CliHost.cs`
   - `CliHostBuilder.cs`
   - `CliHostBuilderExtensions.cs`
   - `ICliHostBuilder.cs`
   - `ICliHost.cs`

2. **コマンド定義関連**
   - `CliCommandAttribute.cs`
   - `CliArgumentAttribute.cs`
   - `ICommandDefinition.cs`
   - `ICommandGroup.cs`
   - `CommandContext.cs`

3. **フィルター機構関連**
   - `ICommandFilter.cs`
   - `CommandFilterAttribute.cs`
   - `CommandFilterOptions.cs`
   - `FilterPipeline.cs`

4. **内部実装**
   - `CommandConfigurators.cs`
   - `ServiceCollectionExtensions.cs`

### Samples フォルダ（`WorkCliHost` → `WorkCliHost.Samples`）

以下のファイルの名前空間を `WorkCliHost.Samples` に変更：

1. **エントリーポイント**
   - `Program.cs`
   - `Program_Minimal.cs.example`

2. **コマンド例**
   - `MessageCommand.cs`
   - `GreetCommand.cs`
   - `UserCommands.cs`
   - `ConfigCommands.cs`
   - `AdvancedCommandPatterns.cs`

3. **フィルター例**
   - `CommonFilters.cs`
   - `AdvancedFilters.cs`
   - `TestFilterCommands.cs`

## 変更例

### Before（変更前）

```csharp
// Core/CliHost.cs
namespace WorkCliHost;

public static class CliHost
{
    // ...
}
```

```csharp
// Samples/MessageCommand.cs
namespace WorkCliHost;

[CliCommand("message", Description = "Show message")]
public sealed class MessageCommand : ICommandDefinition
{
    // ...
}
```

### After（変更後）

```csharp
// Core/CliHost.cs
namespace WorkCliHost.Core;

public static class CliHost
{
    // ...
}
```

```csharp
// Samples/MessageCommand.cs
using WorkCliHost.Core;

namespace WorkCliHost.Samples;

[CliCommand("message", Description = "Show message")]
public sealed class MessageCommand : ICommandDefinition
{
    // ...
}
```

## 使用方法の変更

### ライブラリとして使用する場合

```csharp
using WorkCliHost.Core;

var builder = CliHost.CreateBuilder(args);

builder.ConfigureCommands(commands =>
{
    commands.AddCommand<YourCommand>();
});

var host = builder.Build();
return await host.RunAsync();
```

### サンプルコマンドを参照する場合

```csharp
using WorkCliHost.Core;
using WorkCliHost.Samples;

var builder = CliHost.CreateBuilder(args);

builder.ConfigureCommands(commands =>
{
    commands.AddGlobalFilter<TimingFilter>();
    commands.AddCommand<MessageCommand>();
});

var host = builder.Build();
return await host.RunAsync();
```

## 利点

### 1. 明確な分離

```
WorkCliHost.Core      # フレームワーク本体
WorkCliHost.Samples   # サンプル実装
```

名前空間がフォルダ構造と一致し、どのクラスがどこに属するか一目瞭然。

### 2. 名前の衝突回避

Core とSamples に同名のクラスがあっても、名前空間で区別可能：

```csharp
// フレームワークのフィルター
using WorkCliHost.Core;

// サンプルのフィルター実装
using WorkCliHost.Samples;

// 両方を使用しても衝突しない
var coreFilter = typeof(ICommandFilter);
var sampleFilter = typeof(TimingFilter);
```

### 3. IntelliSense の改善

Visual Studio や VS Code で、名前空間ごとにクラスが整理されて表示される：

```
WorkCliHost.Core
  ├─ CliHost
  ├─ CliHostBuilder
  ├─ ICommandDefinition
  └─ ...

WorkCliHost.Samples
  ├─ MessageCommand
  ├─ TimingFilter
  └─ ...
```

### 4. NuGetパッケージ化の準備

将来的に NuGetパッケージとして公開する場合、名前空間が適切に整理されている：

```
WorkCliHost.Core (NuGetパッケージ)
  namespace: WorkCliHost.Core

WorkCliHost.Samples (サンプルプロジェクト)
  namespace: WorkCliHost.Samples
```

## 動作確認

すべてのコマンドが正常に動作することを確認済み：

```sh
✅ dotnet run -- message "Namespaces updated!"
✅ dotnet run -- user role assign alice admin
✅ dotnet run -- --help
✅ dotnet run -- greet Alice
✅ dotnet run -- config set key value
```

ログ出力でも名前空間が正しく表示される：

```
info: WorkCliHost.Samples.MessageCommand[0]
      Show Namespaces updated!
```

## ビルド結果

```
✅ ビルド成功
✅ 警告なし
✅ エラーなし
```

## 変更ファイル数

- **Core**: 16ファイル
- **Samples**: 10ファイル
- **合計**: 26ファイル

## まとめ

✅ フォルダ構造と名前空間が一致
✅ Core と Samples が明確に分離
✅ すべての機能が正常に動作
✅ NuGetパッケージ化への準備が整った

フレームワークとしての品質がさらに向上し、保守性と拡張性が改善されました。
