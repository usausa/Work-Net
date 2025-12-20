# ドキュメントインデックス

WorkCliHost.Core のドキュメント一覧です。

## 📚 必須ドキュメント

### 1. [README.md](../README.md)
**場所**: プロジェクト直下  
**内容**: 
- プロジェクト概要
- クイックスタート
- 主要機能の紹介
- ドキュメントへのリンク

**対象読者**: 全員（初めての方はここから）

---

### 2. [TECHNICAL_GUIDE.md](TECHNICAL_GUIDE.md)
**内容**:
- クラス・インターフェース一覧（詳細な表）
- アーキテクチャ全体像
- 各コンポーネントの技術的詳細
- 内部実装の解説
- パフォーマンス特性
- 設計上の重要な決定事項

**対象読者**: 開発者、コントリビューター、フレームワークの内部を理解したい方

**主要セクション**:
- サマリー表（ファイル名、型名、種類、メンバー数、ステップ数）
- ホストビルダーの実装詳細
- コマンド定義の仕組み
- フィルター機構の実装
- 属性システムの詳細
- 内部クラスの役割

---

### 3. [API_DESIGN.md](API_DESIGN.md)
**内容**:
- API設計思想と使い方
- ホストビルダーAPI
- コマンド定義API
- フィルター機構の使い方
- 引数定義（Position自動決定含む）
- 実用的な使用例

**対象読者**: フレームワーク利用者、API設計に興味がある方

**主要セクション**:
- 基本設計思想（責任分離、型安全性、プロパティベースAPI）
- ファクトリメソッド（CreateBuilder vs CreateDefaultBuilder）
- コマンド定義（グループ vs 実行可能）
- フィルター機構（4種類のフィルター + CommandContext）
- Position自動決定機能
- 実用例（シンプル、エンタープライズ、カスタム）

---

## 📂 補助ドキュメント

### 4. [INDEX.md](INDEX.md)
**内容**:
- ドキュメントインデックス
- 学習パス
- トピック別ガイド

**対象読者**: 全員

---

## 🗺️ 学習パス

### 初めての方

1. **[README.md](../README.md)** - プロジェクト概要とクイックスタート
2. **[API_DESIGN.md](API_DESIGN.md)** - 基本的な使い方を学ぶ
3. **[Samples/](../Samples/)** - サンプルコードを参照

### 開発者・コントリビューター

1. **[README.md](../README.md)** - 全体像を把握
2. **[API_DESIGN.md](API_DESIGN.md)** - API設計思想を理解
3. **[TECHNICAL_GUIDE.md](TECHNICAL_GUIDE.md)** - 内部実装を深く理解

### アーキテクト

1. **[API_DESIGN.md](API_DESIGN.md)** - 設計思想
2. **[TECHNICAL_GUIDE.md](TECHNICAL_GUIDE.md)** - アーキテクチャと実装詳細

---

## 📝 ドキュメントの役割分担

| ドキュメント | 目的 | 詳細度 |
|-------------|------|--------|
| **README.md** | 導入・概要・プロジェクト構造 | ⭐ |
| **API_DESIGN.md** | 使い方・設計思想 | ⭐⭐⭐ |
| **TECHNICAL_GUIDE.md** | 技術詳細・内部実装・フォルダ構造・名前空間 | ⭐⭐⭐⭐⭐ |
| **INDEX.md** | ドキュメントナビゲーション | ⭐ |

---

## 🔍 トピック別ガイド

### ホストビルダーについて知りたい

- **API**: [API_DESIGN.md](API_DESIGN.md) - ホストビルダーAPI
- **実装**: [TECHNICAL_GUIDE.md](TECHNICAL_GUIDE.md) - ホストビルダーの実装詳細

### コマンド定義について知りたい

- **API**: [API_DESIGN.md](API_DESIGN.md) - コマンド定義API
- **実装**: [TECHNICAL_GUIDE.md](TECHNICAL_GUIDE.md) - コマンド定義の仕組み
- **サンプル**: [Samples/UserCommands.cs](../Samples/UserCommands.cs) - 階層的なコマンド例

### フィルター機構について知りたい

- **API**: [API_DESIGN.md](API_DESIGN.md) - フィルター機構（使い方）
- **実装**: [TECHNICAL_GUIDE.md](TECHNICAL_GUIDE.md) - フィルター機構の実装
- **サンプル**: 
  - [Samples/CommonFilters.cs](../Samples/CommonFilters.cs) - 基本的なフィルター
  - [Samples/AdvancedFilters.cs](../Samples/AdvancedFilters.cs) - 高度なフィルター

### Position自動決定について知りたい

- **API**: [API_DESIGN.md](API_DESIGN.md) - 引数定義（Position自動決定）
- **実装**: [TECHNICAL_GUIDE.md](TECHNICAL_GUIDE.md) - CollectPropertiesWithArguments
- **サンプル**: [Samples/ConfigCommands.cs](../Samples/ConfigCommands.cs) - Position省略例

### プロジェクト構造について知りたい

- **概要**: [README.md](../README.md) - プロジェクト構造の概要
- **詳細**: [TECHNICAL_GUIDE.md](TECHNICAL_GUIDE.md) - プロジェクト構造（フォルダ構成、名前空間、分離の利点）

---

## 🎯 まとめ

WorkCliHost.Coreのドキュメント構成：

1. **README.md** - 導入、クイックスタート、プロジェクト構造概要
2. **API_DESIGN.md** - API設計と使い方（全APIの使用方法を網羅）
3. **TECHNICAL_GUIDE.md** - 技術詳細、内部実装、フォルダ構造詳細、名前空間
4. **INDEX.md** - このファイル（ドキュメントナビゲーション）

必要な情報は上記4つのドキュメントに集約されています。
