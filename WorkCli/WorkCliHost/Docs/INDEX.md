# ドキュメントインデックス

WorkCliHost プロジェクトのドキュメント一覧です。

## 📚 ドキュメント一覧

### 🎯 はじめに

| ドキュメント | 説明 | 対象読者 |
|-------------|------|----------|
| [README.md](../README.md) | プロジェクト概要、クイックスタート、基本的な使い方 | 全員 |

### 📖 技術解説

| ドキュメント | 説明 | 対象読者 |
|-------------|------|----------|
| [TECHNICAL_GUIDE.md](TECHNICAL_GUIDE.md) | Core ライブラリの技術解説、クラス一覧、実装詳細 | 開発者、コントリビューター |

### 🏗️ 設計ドキュメント

| ドキュメント | 説明 | 対象読者 |
|-------------|------|----------|
| [NEW_API_DESIGN.md](NEW_API_DESIGN.md) | 新しいAPI設計、責任分離、型安全性に関する設計思想 | アーキテクト、上級開発者 |
| [PROPERTY_BASED_API.md](PROPERTY_BASED_API.md) | プロパティベースAPI（Configuration、Services、Logging）の詳細 | 中級〜上級開発者 |

### 📂 構造・整理

| ドキュメント | 説明 | 対象読者 |
|-------------|------|----------|
| [FOLDER_STRUCTURE.md](FOLDER_STRUCTURE.md) | フォルダ構造の説明、Core/Samples/Docsの役割分担 | 全員 |
| [FOLDER_REORGANIZATION_SUMMARY.md](FOLDER_REORGANIZATION_SUMMARY.md) | フォルダ整理の経緯、変更内容のサマリー | コントリビューター |
| [NAMESPACE_REORGANIZATION.md](NAMESPACE_REORGANIZATION.md) | 名前空間の変更内容、WorkCliHost.Core/Samplesへの移行 | 開発者 |
| [FILTERS_FOLDER_CLEANUP.md](FILTERS_FOLDER_CLEANUP.md) | Filtersフォルダ削除の理由と経緯 | コントリビューター |

### 🔍 レビュー・問題解決

| ドキュメント | 説明 | 対象読者 |
|-------------|------|----------|
| [REVIEW_RESULTS.md](REVIEW_RESULTS.md) | コードレビューで発見された問題と解決策 | 開発者、コントリビューター |

---

## 📑 ドキュメント詳細

### README.md
**内容**: 
- プロジェクト概要
- 特徴一覧
- クイックスタート（最小構成版/フル機能版）
- コマンド・フィルターの定義方法
- サンプルコード
- パッケージ要件

**いつ読むべきか**: 
- プロジェクトを初めて使う時
- 基本的な使い方を知りたい時

---

### TECHNICAL_GUIDE.md
**内容**:
- Core ライブラリの全クラス・インターフェース一覧表
- 各クラスのステップ数、メソッド数
- アーキテクチャ図
- 各クラスの責務と実装詳細
- パフォーマンス特性
- メモリ使用量

**いつ読むべきか**:
- フレームワークの内部実装を理解したい時
- コントリビューションを検討している時
- 高度なカスタマイズが必要な時

**主要セクション**:
1. クラス・インターフェース一覧
2. アーキテクチャ（全体構成、データフロー）
3. 詳細解説
   - ホストビルダー（CliHost、CliHostBuilder、拡張メソッド）
   - コマンド定義（ICommandDefinition、CommandContext）
   - フィルター機構（各種フィルター、FilterPipeline）
   - 属性システム（CliCommand、CliArgument、CommandFilter）
   - 内部実装（Configurators、ServiceCollectionExtensions）
4. 付録（ステップ数、パフォーマンス、メモリ）

---

### NEW_API_DESIGN.md
**内容**:
- API設計の方針
- 責任分離の考え方（Services vs Commands）
- プロパティベースAPIの採用理由
- 型安全性の実現方法
- ASP.NET Coreとの比較

**いつ読むべきか**:
- なぜこの設計になっているのか理解したい時
- API設計の背景を知りたい時
- 他のフレームワークと比較したい時

**重要なポイント**:
- ConfigureServices() → Services プロパティ
- ConfigureLogging() → Logging プロパティ
- ConfigureCommands() による明確な分離

---

### PROPERTY_BASED_API.md
**内容**:
- `builder.Configuration` の使い方
- `builder.Environment` の情報取得
- `builder.Services` での DI 登録
- `builder.Logging` でのログ設定
- 各プロパティの詳細仕様

**いつ読むべきか**:
- Configuration、Services、Loggingの詳細を知りたい時
- ASP.NET Coreからの移行を考えている時
- 高度な設定が必要な時

**コード例が豊富**:
- JSON設定ファイル読み込み
- 環境変数設定
- ユーザーシークレット
- ログレベル設定
- DI登録パターン

---

### FOLDER_STRUCTURE.md
**内容**:
- フォルダ構造の全体像
- Core フォルダ（フレームワーク本体）の内容
- Samples フォルダ（サンプル実装）の内容
- Docs フォルダ（ドキュメント）の内容
- 各フォルダの役割と責務
- 分離の利点

**いつ読むべきか**:
- プロジェクトの構成を理解したい時
- どこに何があるか確認したい時
- コントリビューションでファイル配置を判断する時

**主要セクション**:
- フォルダツリー図
- Core の詳細（ホストビルダー、コマンド定義、フィルター機構）
- Samples の詳細（コマンド例、フィルター例）
- 使用方法（ライブラリとして、サンプル参照）
- 今後の拡張（NuGetパッケージ化）

---

### FOLDER_REORGANIZATION_SUMMARY.md
**内容**:
- フォルダ整理の実施内容
- 新しいフォルダ構造
- 移動されたファイル一覧（Core、Samples、Docs）
- 変更されたファイル（csprojなど）
- 分離の利点
- 動作確認結果

**いつ読むべきか**:
- フォルダ整理の経緯を知りたい時
- 変更履歴を確認したい時
- プロジェクトの歴史を理解したい時

**整理前後の比較**:
- Before: すべて同じフォルダ
- After: Core/Samples/Docs に分離

---

### NAMESPACE_REORGANIZATION.md
**内容**:
- 名前空間の変更内容
- Core → WorkCliHost.Core
- Samples → WorkCliHost.Samples
- 変更されたファイル一覧（26ファイル）
- 使用方法の変更
- 利点（分離、衝突回避、IntelliSense改善）

**いつ読むべきか**:
- 名前空間の変更を理解したい時
- using文の書き方を確認したい時
- NuGetパッケージ化の準備を理解したい時

**重要な変更**:
```csharp
// Before
using WorkCliHost;

// After
using WorkCliHost.Core;
using WorkCliHost.Samples; // サンプル参照時
```

---

### FILTERS_FOLDER_CLEANUP.md
**内容**:
- Filtersフォルダが存在した理由
- 削除した理由（内容の混在、重複、中途半端な分離）
- 解決策（Core に統合、Samples に移動）
- 新しい構造
- 利点

**いつ読むべきか**:
- なぜFiltersフォルダがないのか疑問に思った時
- フィルター実装の配置場所を知りたい時
- プロジェクトの整理過程を理解したい時

**問題点**:
- インターフェース定義とサンプル実装が混在
- Core/ICommandFilter.cs と重複
- フォルダの目的が不明確

**解決**:
- すべてのインターフェース → Core/ICommandFilter.cs
- サンプル実装 → Samples/AdvancedFilters.cs

---

### REVIEW_RESULTS.md
**内容**:
- コードレビューで発見された問題
- 各問題の詳細説明
- 解決策と実装
- Before/After コード比較

**いつ読むべきか**:
- プロジェクトの品質改善過程を知りたい時
- 同様の問題に直面した時
- ベストプラクティスを学びたい時

**主な問題と解決**:
- 非ジェネリック属性の削除
- Position自動決定の実装
- フィルタAPIの改善
- 責任分離の明確化

---

## 🗺️ 学習パス

### 初心者向け
1. [README.md](../README.md) - 基本を学ぶ
2. [FOLDER_STRUCTURE.md](FOLDER_STRUCTURE.md) - 構成を理解
3. Samples フォルダのコードを見る

### 中級者向け
1. [PROPERTY_BASED_API.md](PROPERTY_BASED_API.md) - 詳細なAPI仕様
2. [NEW_API_DESIGN.md](NEW_API_DESIGN.md) - 設計思想
3. [TECHNICAL_GUIDE.md](TECHNICAL_GUIDE.md) - 実装詳細（概要部分）

### 上級者・コントリビューター向け
1. [TECHNICAL_GUIDE.md](TECHNICAL_GUIDE.md) - 完全な技術解説
2. [REVIEW_RESULTS.md](REVIEW_RESULTS.md) - 品質改善の過程
3. [NAMESPACE_REORGANIZATION.md](NAMESPACE_REORGANIZATION.md) - 整理の経緯
4. Core フォルダのソースコードを読む

---

## 🔗 関連リンク

### 外部ドキュメント
- [System.CommandLine](https://github.com/dotnet/command-line-api) - 基盤ライブラリ
- [Microsoft.Extensions.DependencyInjection](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection) - DI解説
- [Microsoft.Extensions.Configuration](https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration) - Configuration解説
- [Microsoft.Extensions.Logging](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging) - Logging解説

### プロジェクト内リンク
- [Core フォルダ](../Core/) - フレームワーク本体
- [Samples フォルダ](../Samples/) - サンプル実装
- [WorkCliHost.csproj](../WorkCliHost.csproj) - プロジェクトファイル

---

## 📝 ドキュメント更新履歴

| 日付 | ドキュメント | 変更内容 |
|------|-------------|---------|
| 2024/12 | TECHNICAL_GUIDE.md | 新規作成 - Core ライブラリの技術解説 |
| 2024/12 | NAMESPACE_REORGANIZATION.md | 新規作成 - 名前空間変更のサマリー |
| 2024/12 | FILTERS_FOLDER_CLEANUP.md | 新規作成 - Filtersフォルダ削除の説明 |
| 2024/12 | FOLDER_REORGANIZATION_SUMMARY.md | 新規作成 - フォルダ整理のサマリー |
| 2024/12 | FOLDER_STRUCTURE.md | 新規作成 - フォルダ構造の詳細説明 |
| 2024/12 | PROPERTY_BASED_API.md | 既存 - プロパティベースAPI解説 |
| 2024/12 | NEW_API_DESIGN.md | 既存 - 新API設計の解説 |
| 2024/12 | REVIEW_RESULTS.md | 既存 - レビュー結果の記録 |

---

## 🤝 コントリビューション

ドキュメントの改善提案やバグ報告は、GitHubのIssueでお願いします。

### ドキュメントの追加ガイドライン
1. **対象読者を明確に**: 初心者/中級者/上級者
2. **コード例を含める**: 実用的な例を提供
3. **構造化する**: 見出し、リスト、表を活用
4. **このインデックスを更新**: 新しいドキュメントを追加したら、このファイルも更新

---

## 📧 お問い合わせ

質問や提案がある場合は、GitHubのDiscussionsまたはIssueでお知らせください。
