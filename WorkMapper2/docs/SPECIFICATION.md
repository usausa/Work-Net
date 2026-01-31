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
| `[MapConstant<T>]` | 型指定の固定値設定（C# 11+） | Method |
| `[MapIgnore]` | マッピング除外 | Method |
| `[AfterMap]` | マッピング後の追加処理 | Method |
| `[BeforeMap]` | マッピング前の追加処理 | Method |
| `[MapCondition]` | マッピング全体の条件 | Method |
| `[MapPropertyCondition]` | プロパティレベルの条件 | Method |

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
    [MapProperty(nameof(Source.SourceName), nameof(Destination.DestinationName))]
    [MapProperty(nameof(Source.Code), nameof(Destination.Id))]  // 複数指定可能
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

| Phase | 機能 | 優先度 | 実装状況 |
|-------|------|--------|----------|
| 1 | 基本マッピング（同名プロパティ） | 必須 | ✅ 完了 |
| 1 | `[MapProperty]` 異なる名前のマッピング | 必須 | ✅ 完了 |
| 1 | `[MapIgnore]` 除外 | 必須 | ✅ 完了 |
| 2 | `[MapConstant]` 固定値 | 高 | ✅ 完了 |
| 2 | `[BeforeMap]`/`[AfterMap]` 追加処理 | 高 | ✅ 完了 |
| 2 | 基本的な型変換（組み込み型） | 高 | ✅ 完了 |
| - | `[MapProperty]` ドット記法（ネスト対応） | 高 | ✅ 完了 |
| 3 | `[MapFrom]` 合成 | 中 | - |
| 3 | `[MapperConverter]` カスタム変換 | 中 | - |
| 3 | 入れ子クラスのマッピング（自動検出） | 中 | - |
| 4 | コレクションのマッピング | 中 | - |
| 4 | Null処理オプション | 低 | - |
| 5 | Flatten/Unflatten | 低 | - |
| 5 | 条件付きマッピング | 低 | - |

## 7. ネストプロパティマッピング

`[MapProperty]`属性でドット記法を使用することで、ネストされたプロパティ間のマッピングが可能です。

### 7.1 フラットからネストへのマッピング（Unflatten）

```csharp
public class Source
{
    public int Value1 { get; set; }
    public int Value2 { get; set; }
}

public class DestinationChild
{
    public int Value { get; set; }
}

public class Destination
{
    public DestinationChild? Child1 { get; set; }
    public DestinationChild? Child2 { get; set; }
}

[Mapper]
[MapProperty("Value1", "Child1.Value")]
[MapProperty("Value2", "Child2.Value")]
public static partial void Map(Source source, Destination destination);
```

**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.Child1 ??= new DestinationChild();
    destination.Child2 ??= new DestinationChild();
    destination.Child1.Value = source.Value1;
    destination.Child2.Value = source.Value2;
}
```

### 7.2 ネストからフラットへのマッピング（Flatten）

```csharp
public class SourceChild
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Source
{
    public SourceChild? Child { get; set; }
}


public class Destination
{
    public int ChildId { get; set; }
    public string ChildName { get; set; }
}

[Mapper]
[MapProperty("Child.Id", "ChildId")]
[MapProperty("Child.Name", "ChildName")]
public static partial void Map(Source source, Destination destination);
```

**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    if (source.Child is not null)
    {
        destination.ChildId = source.Child.Id;
        destination.ChildName = source.Child.Name;
    }
}
```

### 7.3 深いネストのマッピング

```csharp
[Mapper]
[MapProperty("DeepValue", "Outer.Inner.Value")]
public static partial void Map(DeepSource source, DeepNestedDestination destination);
```

**生成コード:**

```csharp
public static partial void Map(DeepSource source, DeepNestedDestination destination)
{
    destination.Outer ??= new DeepNestedParent();
    destination.Outer.Inner ??= new DeepNestedChild();
    destination.Outer.Inner.Value = source.DeepValue;
}
```

### 7.4 注意事項

- **ターゲット側のネスト**: 中間オブジェクトが`null`の場合、自動的にインスタンスが生成されます（`??= new`）
- **ソース側のネスト**: 中間オブジェクトがnullableの場合、nullチェックが追加され、nullの場合はマッピングがスキップされます
- 既存のインスタンスは保持され、プロパティのみが更新されます

## 8. Null処理

### 8.1 Nullableプロパティの動作

| ソース型 | ターゲット型 | 動作 |
|----------|-------------|------|
| `T?` | `T?` | そのままコピー（nullも含む） |
| `T?` | `T` (末端) | `default!` を代入 |
| `T` | `T?` | そのままコピー |
| `T` | `T` | そのままコピー |

### 8.2 ネストプロパティのnull処理

ソース側のネストプロパティの**中間パス**がnullableの場合、処理をスキップします：

```csharp
// Source.Child? がnullableの場合
public static partial void Map(Source source, Destination destination)
{
    // 非ネストプロパティは常にコピー
    destination.DirectValue = source.DirectValue;
    
    // ネストプロパティは中間要素のnullチェック付き
    if (source.Child is not null)
    {
        destination.ChildId = source.Child.Id;
        destination.ChildName = source.Child.Name;
    }
}
```

### 8.3 末端要素の nullable → non-nullable マッピング

末端の要素がnullの場合は `default!` を代入します：

```csharp
public class Source
{
    public string? Name { get; set; }
    public int? IntValue { get; set; }
}

public class Destination
{
    public string Name { get; set; } = "default";
    public string IntValue { get; set; } = "default";
}
```


**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    // string? -> string: null-forgiving operator を使用
    destination.Name = source.Name!;
    
    // int? -> string: null-coalescing で default! を使用
    destination.IntValue = source.IntValue?.ToString() ?? default!;
}
```

## 9. 実装済み型変換一覧

### 9.1 文字列への変換

すべての型から `string` への変換は `ToString()` メソッドを使用します。
nullable型の場合は `?.ToString()` を使用し、ターゲットがnon-nullableなら `?? default!` を追加します。

### 9.2 文字列からの変換

| 変換先 | 変換方法 |
|--------|----------|
| `int` | `int.Parse()` |
| `long` | `long.Parse()` |
| `short` | `short.Parse()` |
| `byte` | `byte.Parse()` |
| `uint`, `ulong`, `ushort` | 対応する `Parse()` |
| `float` | `float.Parse()` |
| `double` | `double.Parse()` |
| `decimal` | `decimal.Parse()` |
| `bool` | `bool.Parse()` |
| `DateTime` | `DateTime.Parse()` |
| `DateTimeOffset` | `DateTimeOffset.Parse()` |
| `DateOnly` | `DateOnly.Parse()` |
| `TimeOnly` | `TimeOnly.Parse()` |
| `TimeSpan` | `TimeSpan.Parse()` |
| `Guid` | `Guid.Parse()` |

### 9.3 数値型間の変換

数値型（`int`, `long`, `short`, `byte`, `float`, `double`, `decimal` 等）間の変換は、明示的なキャストを使用します。

### 9.4 日時型の変換

| 変換元 | 変換先 | 変換方法 |
|--------|--------|----------|
| `DateTime` | `DateTimeOffset` | `new DateTimeOffset(value)` |
| `DateTimeOffset` | `DateTime` | `.DateTime` |
| `DateTime` | `DateOnly` | `DateOnly.FromDateTime()` |
| `DateTime` | `TimeOnly` | `TimeOnly.FromDateTime()` |

## 10. カスタムパラメータ

マッパーメソッドに追加のパラメータを指定し、`BeforeMap`/`AfterMap`などのカスタムメソッドで使用できます。

### 10.1 パラメータの識別ルール

| パターン | Source | Destination | カスタムパラメータ |
|---------|--------|-------------|-------------------|
| `void Map(A, B, C, D)` | 第1引数 | 第2引数 | 第3引数以降 |
| `B Map(A, C, D)` | 第1引数 | 戻り値 | 第2引数以降 |

### 10.2 使用例

```csharp
// カスタムコンテキストを渡すパターン
[Mapper]
[BeforeMap(nameof(OnBeforeMap))]
[AfterMap(nameof(OnAfterMap))]
public static partial void Map(Source source, Destination destination, IServiceProvider services);

// カスタムパラメータを受け取るコールバック
private static void OnBeforeMap(Source source, Destination destination, IServiceProvider services)
{
    // servicesを使った処理
}

// カスタムパラメータなしのコールバック（後方互換）
private static void OnAfterMap(Source source, Destination destination)
{
    // 基本的な処理
}
```

**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination, IServiceProvider services)
{
    OnBeforeMap(source, destination, services);  // カスタムパラメータを渡す
    destination.Name = source.Name;
    // ...
    OnAfterMap(source, destination);  // カスタムパラメータなしで呼び出し
}
```

### 10.3 戻り値パターン

```csharp
[Mapper]
[AfterMap(nameof(OnAfterMap))]
public static partial Destination Map(Source source, CustomContext context);

private static void OnAfterMap(Source source, Destination destination, CustomContext context)
{
    context.MappingComplete = true;
}
```

### 10.4 制約事項

- **同じ型の複数パラメータは禁止**: カスタムパラメータに同じ型を複数指定するとコンパイルエラー（ML0003）

```csharp
// NG: 同じ型 (string) が複数ある
[Mapper]
public static partial void Map(Source source, Destination destination, string param1, string param2);
// → ML0003: Custom parameters must have unique types
```

### 10.5 BeforeMap/AfterMap のシグネチャ

| BeforeMap/AfterMapのシグネチャ | 動作 |
|------------------------------|------|
| `(Source, Destination)` | カスタムパラメータなしで呼び出し |
| `(Source, Destination, ...customParams)` | カスタムパラメータを渡して呼び出し |
| シグネチャ不一致 | コンパイルエラー（ML0004/ML0005） |

カスタムパラメータを持つバージョンが優先されます。

### 10.6 Converter でのカスタムパラメータ

`MapProperty`の`Converter`プロパティで指定したカスタムコンバーターでもカスタムパラメータを使用できます。

```csharp
[Mapper]
[MapProperty(nameof(Source.Value), nameof(Destination.ConvertedValue), Converter = nameof(ConvertWithContext))]
public static partial void Map(Source source, Destination destination, CustomContext context);

// カスタムパラメータなしのコンバーター
private static string ConvertIntToString(int value)
{
    return $"Value: {value}";
}

// カスタムパラメータありのコンバーター（優先される）
private static string ConvertWithContext(int value, CustomContext context)
{
    return $"Value: {value}, Context: {context.Value}";
}
```

**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination, CustomContext context)
{
    destination.ConvertedValue = ConvertWithContext(source.Value, context);
}
```

| Converterのシグネチャ | 動作 |
|---------------------|------|
| `(SourceType)` | カスタムパラメータなしで呼び出し |
| `(SourceType, ...customParams)` | カスタムパラメータを渡して呼び出し |
| シグネチャ不一致 | コンパイルエラー（ML0006） |

## 11. 条件付きマッピング

### 11.1 グローバル条件

マッピング全体に条件を適用します。

```csharp
[Mapper]
[MapCondition(nameof(ShouldMap))]
public static partial void Map(Source source, Destination destination);

private static bool ShouldMap(Source source, Destination destination)
{
    return source.IsActive;
}
```

**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    if (ShouldMap(source, destination))
    {
        destination.Value = source.Value;
        destination.Name = source.Name;
    }
}
```

### 11.2 プロパティレベル条件

特定のプロパティのマッピングに条件を適用します。

```csharp
[Mapper]
[MapPropertyCondition(nameof(Destination.Name), nameof(ShouldMapName))]
public static partial void Map(Source source, Destination destination);

private static bool ShouldMapName(string? name)
{
    return !string.IsNullOrEmpty(name);
}
```

**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.Value = source.Value;
    if (ShouldMapName(source.Name))
    {
        destination.Name = source.Name;
    }
}
```

### 11.3 カスタムパラメータ付き条件

```csharp
[Mapper]
[MapCondition(nameof(ShouldMap))]
public static partial void Map(Source source, Destination destination, MappingContext context);

private static bool ShouldMap(Source source, Destination destination, MappingContext context)
{
    return context.ShouldMap && source.IsActive;
}
```

## 12. Generic MapConstant

C# 11以降のGeneric Attribute機能を使用して、型安全な定数マッピングができます。

```csharp
[Mapper]
[MapConstant<int>("Version", 1)]
[MapConstant<string>("Status", "Active")]
[MapConstant<bool>("IsEnabled", true)]
public static partial void Map(Source source, Destination destination);
```

**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.Name = source.Name;
    destination.Version = 1;
    destination.Status = "Active";
    destination.IsEnabled = true;
}
```

従来の非Generic版も引き続き使用できます：

```csharp
[MapConstant("Status", "Active")]
[MapConstant("Version", 1)]
[MapConstant("CreatedAt", null, Expression = "System.DateTime.Now")]
```

## 13. 診断メッセージ

| コード | 説明 |
|--------|------|
| ML0001 | Mapperメソッドは static partial である必要があります |
| ML0002 | Mapperメソッドのパラメータ数が不正です |
| ML0003 | カスタムパラメータに同じ型が複数指定されています |
| ML0004 | BeforeMapメソッドのシグネチャが一致しません |
| ML0005 | AfterMapメソッドのシグネチャが一致しません |
| ML0006 | Converterメソッドのシグネチャが一致しません |
| ML0007 | 条件メソッドのシグネチャが一致しません |
| ML0008 | プロパティ条件メソッドのシグネチャが一致しません |

## 14. 実装ステータス

### 14.1 実装済み機能

| 機能 | ステータス |
|------|----------|
| 基本マッピング（同名プロパティ） | ✅ 実装済み |
| 異なるプロパティ名のマッピング | ✅ 実装済み |
| MapIgnore | ✅ 実装済み |
| MapConstant | ✅ 実装済み |
| MapConstant<T> (Generic) | ✅ 実装済み |
| BeforeMap / AfterMap | ✅ 実装済み |
| 型変換（数値、文字列、日時等） | ✅ 実装済み |
| Flatten（ネストソース→フラット） | ✅ 実装済み |
| Unflatten（フラット→ネストデスティネーション） | ✅ 実装済み |
| 多段ネスト対応 | ✅ 実装済み |
| Null安全処理 | ✅ 実装済み |
| カスタムパラメータ | ✅ 実装済み |
| Converter | ✅ 実装済み |
| 条件付きマッピング（グローバル） | ✅ 実装済み |
| 条件付きマッピング（プロパティレベル） | ✅ 実装済み |

### 14.2 未実装機能

| 機能 | 説明 |
|------|------|
| MapFrom（複数プロパティからの合成） | 複数のソースプロパティを1つのデスティネーションに合成 |
| MapperConverter（カスタム型変換器） | クラス/アセンブリレベルでのカスタム変換器定義 |
| コレクション対応 | List, Array等のコレクションマッピング |
| 継承対応 | 派生クラスのマッピング |
| 双方向マッピング | SourceとDestination両方向のマッピング生成 |
| インクルードマッピング | 別のマッパーを呼び出してネストオブジェクトをマッピング |
| NullSubstitute | null値の代替値設定 |

