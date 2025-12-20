# Filters フォルダの削除について

## 質問
> CommandFilterInterfaces.csが単独のフォルダにいるのはなぜですか？

## 回答

`Filters/CommandFilterInterfaces.cs` が単独のフォルダにいた理由は、**設計の不一致**でした。

### 問題点

#### 1. 内容の混在

`Filters/CommandFilterInterfaces.cs` には2種類の内容が混在していました：

1. **フィルターインターフェース定義**
   - `ICommandExecutionFilter`
   - `IBeforeCommandFilter`
   - `IAfterCommandFilter`
   - `IExceptionFilter`
   - `CommandExecutionDelegate`

   → これらは**Core**に属するべき（フレームワークの一部）

2. **サンプルフィルター実装**
   - `LoggingFilter`
   - `TimingFilter`
   - `AuthorizationFilter`
   - `ValidationFilter`
   - など

   → これらは**Samples**に属するべき（使用例）

#### 2. 既存の重複

実は、`Core/ICommandFilter.cs` に既にすべてのフィルターインターフェースが定義されていました：

```csharp
// Core/ICommandFilter.cs に既に存在
public interface ICommandFilter { ... }
public interface ICommandExecutionFilter : ICommandFilter { ... }
public interface IBeforeCommandFilter : ICommandFilter { ... }
public interface IAfterCommandFilter : ICommandFilter { ... }
public interface IExceptionFilter : ICommandFilter { ... }
public delegate ValueTask CommandExecutionDelegate();
```

#### 3. 中途半端な分離

- フィルターの参考実装を置く場所として `Filters/` を用意
- しかし、基本インターフェースは `Core` にあり、拡張インターフェースとサンプル実装が `Filters/` に混在
- 結果として、どこに何があるか不明確な状態

## 解決策

### 実施した変更

1. **Filters フォルダの削除**
   - 中途半端な分離を解消

2. **インターフェースはすべて Core に統合**
   - `Core/ICommandFilter.cs` にすべてのフィルターインターフェースが存在
   - 追加のインターフェースも必要なら `Core/` に配置

3. **サンプル実装は Samples に移動**
   - `Samples/CommonFilters.cs` - 基本的なフィルター例
   - `Samples/AdvancedFilters.cs` - 高度なフィルター例

### 新しい構造

```
WorkCliHost/
├── Core/
│   └── ICommandFilter.cs         # すべてのフィルターインターフェース
│
└── Samples/
    ├── CommonFilters.cs          # TimingFilter, LoggingFilter, ExceptionHandlingFilter
    └── AdvancedFilters.cs        # AuthorizationFilter, ValidationFilter, TransactionFilter
```

## 利点

### 1. 明確な責任分離

- **Core**: フレームワークの一部としてのインターフェース定義
- **Samples**: フレームワークの使用例としてのフィルター実装

### 2. 再利用性の向上

`Core/` フォルダのみをコピーすれば、すべてのフィルター機能が使用可能：

```csharp
// Core にすべてのインターフェースが含まれる
public interface ICommandFilter { ... }
public interface ICommandExecutionFilter : ICommandFilter { ... }
// ...
```

### 3. 学習のしやすさ

- **初心者**: `Samples/CommonFilters.cs` を見れば、フィルターの実装方法がわかる
- **上級者**: `Core/ICommandFilter.cs` を見れば、フィルターの仕組みがわかる

### 4. 保守性の向上

- インターフェースとサンプル実装が明確に分離
- 変更の影響範囲が明確
- 重複がない

## 今後の拡張

将来的に、共通的なフィルター実装を別パッケージとして提供する場合：

```
WorkCliHost.Filters/              # オプショナルパッケージ（将来）
├── AuthenticationFilter.cs      # JWT/OAuth認証
├── AuthorizationFilter.cs       # ロールベース認可
├── ValidationFilter.cs           # FluentValidation統合
└── CachingFilter.cs              # 結果のキャッシング
```

ただし現時点では、サンプル実装として `Samples/` フォルダに配置するのが適切です。

## まとめ

**質問への回答**:
> CommandFilterInterfaces.csが単独のフォルダにいるのはなぜですか？

**回答**:
設計の不一致により、インターフェース定義とサンプル実装が混在し、既存の `Core/ICommandFilter.cs` と重複していました。

**解決**:
- Filters フォルダを削除
- インターフェースは `Core/ICommandFilter.cs` に統合（既存）
- サンプル実装は `Samples/AdvancedFilters.cs` に移動

**結果**:
- 明確な責任分離
- 重複の解消
- 再利用性と保守性の向上
