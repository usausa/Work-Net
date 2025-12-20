# フォルダ構造

## 概要

プロジェクトは以下のフォルダ構造で整理されています：

```
WorkCliHost/
├── Core/                          # ライブラリコア（フレームワーク本体）
│   ├── CliHost.cs                # ファクトリメソッド
│   ├── CliHostBuilder.cs         # ビルダー実装
│   ├── CliHostBuilderExtensions.cs # ビルダー拡張メソッド
│   ├── ICliHostBuilder.cs        # ビルダーインターフェース
│   ├── ICliHost.cs               # ホストインターフェース
│   ├── CliCommandAttribute.cs    # コマンド属性
│   ├── CliArgumentAttribute.cs   # 引数属性
│   ├── ICommandDefinition.cs     # コマンド定義インターフェース
│   ├── ICommandGroup.cs          # グループコマンドインターフェース
│   ├── CommandContext.cs         # コマンド実行コンテキスト
│   ├── ICommandFilter.cs         # フィルターインターフェース（全種類）
│   ├── CommandFilterAttribute.cs # フィルター属性
│   ├── CommandFilterOptions.cs   # フィルターオプション
│   ├── FilterPipeline.cs         # フィルターパイプライン実装
│   ├── CommandConfigurators.cs   # コマンド設定クラス群
│   └── ServiceCollectionExtensions.cs # サービス拡張（非推奨）
│
├── Samples/                       # サンプル実装
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
└── Docs/                          # ドキュメント
    ├── NEW_API_DESIGN.md         # 新しいAPI設計
    ├── PROPERTY_BASED_API.md     # プロパティベースAPI
    ├── REVIEW_RESULTS.md         # レビュー結果
    ├── FOLDER_STRUCTURE.md       # このファイル
    └── FOLDER_REORGANIZATION_SUMMARY.md # 整理サマリー
```

## Core（ライブラリコア）

フレームワークの中核となる機能を提供します。

### ホストビルダー関連
- `CliHost.cs` - `CreateBuilder()`, `CreateDefaultBuilder()` などのファクトリメソッド
- `CliHostBuilder.cs` - ビルダーの実装
- `CliHostBuilderExtensions.cs` - 拡張メソッド（`UseDefaults()` など）
- `ICliHostBuilder.cs` - ビルダーのインターフェース
- `ICliHost.cs` - ホストのインターフェース

### コマンド定義関連
- `CliCommandAttribute.cs` - コマンドを定義する属性
- `CliArgumentAttribute.cs` - 引数を定義する属性
- `ICommandDefinition.cs` - 実行可能コマンドのインターフェース
- `ICommandGroup.cs` - グループコマンドのインターフェース
- `CommandContext.cs` - コマンド実行時のコンテキスト

### フィルター機構関連
- `ICommandFilter.cs` - フィルターインターフェース（すべての種類を含む）
  - `ICommandFilter` - 基本インターフェース
  - `ICommandExecutionFilter` - 実行前後のフィルター
  - `IBeforeCommandFilter` - 実行前フィルター
  - `IAfterCommandFilter` - 実行後フィルター
  - `IExceptionFilter` - 例外ハンドリングフィルター
  - `CommandExecutionDelegate` - パイプラインデリゲート
- `CommandFilterAttribute.cs` - コマンドにフィルターを適用する属性
- `CommandFilterOptions.cs` - フィルターのオプション設定
- `FilterPipeline.cs` - フィルターパイプラインの実装

### 内部実装
- `CommandConfigurators.cs` - コマンド設定の内部クラス群
- `ServiceCollectionExtensions.cs` - 非推奨の拡張メソッド（後方互換性のため残存）

## Samples（サンプル実装）

フレームワークの使い方を示すサンプルコードです。

### エントリーポイント
- `Program.cs` - メインのサンプルアプリケーション
- `Program_Minimal.cs.example` - 最小構成版のサンプル

### コマンド例
- `MessageCommand.cs` - 最もシンプルなコマンド
- `GreetCommand.cs` - デフォルト値を持つコマンド
- `UserCommands.cs` - 階層的なコマンド構造（user → role → assign）
- `ConfigCommands.cs` - Position自動決定の例
- `AdvancedCommandPatterns.cs` - 基底クラスを使った共通引数の例

### フィルター例
- `CommonFilters.cs` - `TimingFilter`, `LoggingFilter`, `ExceptionHandlingFilter`
- `AdvancedFilters.cs` - `AuthorizationFilter`, `ValidationFilter`, `TransactionFilter`, `CleanupFilter`
- `TestFilterCommands.cs` - フィルターのテストコマンド

## Docs（ドキュメント）

設計ドキュメントやガイドを配置するフォルダです。

- `NEW_API_DESIGN.md` - 責任分離と型安全性に関する設計
- `PROPERTY_BASED_API.md` - プロパティベースAPIの詳細
- `REVIEW_RESULTS.md` - レビューで発見された問題と解決策
- `FOLDER_STRUCTURE.md` - このフォルダ構造の説明
- `FOLDER_REORGANIZATION_SUMMARY.md` - フォルダ整理のサマリー

## 使用方法

### ライブラリとして使用する場合

`Core/` フォルダ配下のファイルのみを使用します：

```csharp
using WorkCliHost;

var builder = CliHost.CreateBuilder(args);

builder.ConfigureCommands(commands =>
{
    commands.AddCommand<YourCommand>();
});

var host = builder.Build();
return await host.RunAsync();
```

### サンプルを参考にする場合

`Samples/` フォルダ配下のファイルを参照して、実装方法を学びます：

- シンプルなコマンド: `MessageCommand.cs`
- 階層的なコマンド: `UserCommands.cs`
- フィルターの実装: `CommonFilters.cs`, `AdvancedFilters.cs`
- 最小構成版: `Program_Minimal.cs.example`

## 分離の利点

1. **明確な責任分離**
   - Core: フレームワーク本体（17ファイル）
   - Samples: 使い方の例（10ファイル）
   - Docs: ドキュメント（5ファイル）

2. **再利用性**
   - Core フォルダのみを別プロジェクトにコピーして使用可能
   - すべてのフィルターインターフェースが Core に含まれる

3. **保守性**
   - フレームワーク本体とサンプルが混在しない
   - 変更の影響範囲が明確

4. **学習のしやすさ**
   - Samples を見れば使い方がわかる
   - Core を見ればフレームワークの仕組みがわかる

## Filters フォルダの削除について

以前は `Filters/` フォルダがありましたが、以下の理由で削除されました：

### 問題点
- **内容の混在**: インターフェース定義（Core に属する）とサンプル実装（Samples に属する）が混在
- **重複**: `ICommandFilter.cs` に既に全てのフィルターインターフェースが定義されていた
- **中途半端な分離**: フィルターの参考実装という目的が不明確

### 解決策
- **Core に統合**: すべてのフィルターインターフェースは `Core/ICommandFilter.cs` に集約
- **Samples に移動**: フィルターの実装例は `Samples/AdvancedFilters.cs` として配置
- **明確な分離**: Core = インターフェース、Samples = 実装例

## 今後の拡張

### ライブラリ化する場合

1. `Core/` フォルダを別プロジェクトとして分離
2. NuGetパッケージとして公開
3. `Samples/` を別のサンプルプロジェクトとして配置

### オプショナルパッケージ

将来的に、共通的なフィルター実装を別パッケージとして提供することも検討：

```
WorkCliHost.Filters/          # オプショナルパッケージ
├── AuthenticationFilter.cs  # JWT/OAuth認証
├── AuthorizationFilter.cs   # ロールベース認可
├── ValidationFilter.cs       # FluentValidation統合
├── CachingFilter.cs          # 結果のキャッシング
└── ...
```

ただし現時点では、サンプル実装として `Samples/` フォルダに配置するのが適切です。
