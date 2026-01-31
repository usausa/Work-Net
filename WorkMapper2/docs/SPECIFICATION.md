# MapperLibrary 仕様設計書

## 1. 概要

MapperLibraryは、Source Generatorベースのオブジェクトマッパーライブラリです。
SourceオブジェクトのプロパティをDestinationオブジェクトのプロパティにコピーするコードを自動生成します。

## 2. 基本設計

### 2.1 基本的なマッピング定義

```csharp
[Mapper]
public static partial void Map(Source source, Destination destination);

[Mapper]
public static partial Destination Map(Source source);
```

- 同名・同型のプロパティは自動でマッピング
- `partial`メソッドに対してSource Generatorがコードを生成

## 3. 属性一覧

### 3.1 メソッドレベル属性

| 属性 | 用途 | 適用対象 |
|------|------|----------|
| `[Mapper]` | マッピングメソッドの指定 | Method |
| `[MapProperty]` | プロパティ間のマッピング指定 | Method |
| `[MapFrom]` | 複数プロパティからの合成 | Method |
| `[MapConstant]` | 固定値の設定 | Method |
| `[MapIgnore]` | マッピング除外 | Method |
| `[AfterMap]` | マッピング後の追加処理 | Method |
| `[BeforeMap]` | マッピング前の追加処理 | Method |

### 3.2 クラス/アセンブリレベル属性

| 属性 | 用途 | 適用対象 |
|------|------|----------|
| `[MapperConverter]` | カスタム型変換器の指定 | Class, Assembly |

## 4. 詳細仕様

### 4.1 異なるプロパティ名間のマッピング

```csharp
internal static partial class ObjectMapper
{
    [Mapper]
    [MapProperty("SourceName", "DestinationName")]
    [MapProperty("Code", "Id")]  // 複数指定可能
    public static partial void Map(Source source, Destination destination);
}
```

### 4.2 複数プロパティからの値合成

```csharp
internal static partial class ObjectMapper
{
    [Mapper]
    [MapFrom("FullName", nameof(CombineFullName))]
    public static partial void Map(Source source, Destination destination);
    
    // 合成用メソッド（同じクラス内に定義）
    private static string CombineFullName(Source source) 
        => $"{source.FirstName} {source.LastName}";
}
```

**生成コード例:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.FullName = CombineFullName(source);
    // 他のプロパティ...
}
```

### 4.3 追加処理（Before/After Map）

```csharp
internal static partial class ObjectMapper
{
    [Mapper]
    [BeforeMap(nameof(BeforeMapping))]
    [AfterMap(nameof(AfterMapping))]
    public static partial void Map(Source source, Destination destination);
    
    private static void BeforeMapping(Source source, Destination destination)
    {
        // マッピング前の処理
    }
    
    private static void AfterMapping(Source source, Destination destination)
    {
        // マッピング後の処理
    }
}
```

**生成コード例:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    BeforeMapping(source, destination);
    
    destination.Value1 = source.Value1;
    // ...
    
    AfterMapping(source, destination);
}
```

### 4.4 入れ子クラスのマッピング

#### パターンA: 自動検出（同じクラス内にマッパーが存在する場合）

```csharp
internal static partial class ObjectMapper
{
    [Mapper]
    public static partial void Map(Source source, Destination destination);
    
    [Mapper]
    public static partial void Map(NestedSource source, NestedDestination destination);
}
```

生成時に、`NestedSource` → `NestedDestination`のマッパーが同一クラス内にあれば自動的に使用。

#### パターンB: 明示的指定

```csharp
[Mapper]
[MapProperty("Nested", "Nested", MapperType = typeof(NestedObjectMapper))]
public static partial void Map(Source source, Destination destination);
```

### 4.5 型変換（カスタムコンバーター）

#### 4.5.1 デフォルト動作

- 組み込み型間: `Convert.ToXxx()` または暗黙的変換を使用
- `int` → `string`: `ToString()` を使用
- `string` → `int`: `int.Parse()` または `Convert.ToInt32()` を使用

#### 4.5.2 カスタムコンバーター定義

```csharp
// コンバータークラス
public static class CustomConverters
{
    public static string IntToFormattedString(int value) 
        => value.ToString("N0");
    
    public static DateTime StringToDateTime(string value) 
        => DateTime.Parse(value);
}
```

#### 4.5.3 コンバーター指定方法

**メソッドレベル（特定のプロパティ）:**

```csharp
[Mapper]
[MapProperty("Value", "FormattedValue", Converter = nameof(CustomConverters.IntToFormattedString))]
public static partial void Map(Source source, Destination destination);
```

**クラスレベル（型ペアに対して）:**

```csharp
[MapperConverter(typeof(int), typeof(string), typeof(CustomConverters), nameof(CustomConverters.IntToFormattedString))]
internal static partial class ObjectMapper
{
    [Mapper]
    public static partial void Map(Source source, Destination destination);
}
```

**アセンブリレベル（グローバル）:**

```csharp
[assembly: MapperConverter(typeof(int), typeof(string), typeof(CustomConverters), nameof(CustomConverters.IntToFormattedString))]
```

### 4.6 固定値の設定

```csharp
[Mapper]
[MapConstant("Status", "Active")]
[MapConstant("Version", 1)]
[MapConstant("CreatedAt", Expression = "DateTime.Now")]  // 式として評価
public static partial void Map(Source source, Destination destination);
```

**生成コード例:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.Status = "Active";
    destination.Version = 1;
    destination.CreatedAt = DateTime.Now;
    // ...
}
```

### 4.7 マッピング除外

```csharp
[Mapper]
[MapIgnore("InternalId")]
[MapIgnore("TempValue")]
public static partial void Map(Source source, Destination destination);
```

## 5. 追加検討機能

### 5.1 Null処理の指定

```csharp
[Mapper(NullHandling = NullHandling.SetNull)]  // または ThrowException, SkipProperty
public static partial void Map(Source source, Destination destination);
```

### 5.2 コレクションのマッピング

```csharp
// List<SourceItem> → List<DestItem> の自動マッピング
[Mapper]
public static partial void Map(Source source, Destination destination);

// ItemのマッパーがあればListも自動対応
[Mapper]  
public static partial DestItem Map(SourceItem source);
```

### 5.3 条件付きマッピング

```csharp
[Mapper]
[MapWhen("Premium", nameof(IsPremiumUser))]  // 条件が真の場合のみマップ
public static partial void Map(Source source, Destination destination);

private static bool IsPremiumUser(Source source) => source.UserType == "Premium";
```

### 5.4 継承のサポート

```csharp
[Mapper]
[MapIncludeBase]  // 基底クラスのプロパティも含める
public static partial void Map(DerivedSource source, DerivedDestination destination);
```

### 5.5 Flattening / Unflattening

```csharp
// Source.Address.City → Destination.AddressCity (Flatten)
[Mapper]
[MapFlatten("Address")]
public static partial void Map(Source source, Destination destination);

// Source.AddressCity → Destination.Address.City (Unflatten)  
[Mapper]
[MapUnflatten("Address")]
public static partial void Map(Source source, Destination destination);
```

## 6. 実装優先度

| Phase | 機能 | 優先度 |
|-------|------|--------|
| 1 | 基本マッピング（同名プロパティ） | 必須 |
| 1 | `[MapProperty]` 異なる名前のマッピング | 必須 |
| 1 | `[MapIgnore]` 除外 | 必須 |
| 2 | `[MapConstant]` 固定値 | 高 |
| 2 | `[BeforeMap]`/`[AfterMap]` 追加処理 | 高 |
| 2 | 基本的な型変換（組み込み型） | 高 |
| 3 | `[MapFrom]` 合成 | 中 |
| 3 | `[MapperConverter]` カスタム変換 | 中 |
| 3 | 入れ子クラスのマッピング | 中 |
| 4 | コレクションのマッピング | 中 |
| 4 | Null処理オプション | 低 |
| 5 | Flatten/Unflatten | 低 |
| 5 | 条件付きマッピング | 低 |
