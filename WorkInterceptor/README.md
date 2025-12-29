# WorkInterceptor.Library

C# Interceptorsを使用したSource Generatorライブラリのサンプル実装です。

## 機能

このライブラリは、`IBuilder.Execute<T>()`メソッドの呼び出しを自動的にinterceptし、`IBuilder.Execute<T>(Action action)`の呼び出しに置き換えます。Actionは、T型のCommandAttributeとOptionAttributeの情報をConsole出力するコードがビルド時に生成されます。

これにより、**実行時のリフレクションではなく、ビルド時にコンパイラが持つ型情報を使用して属性情報を抽出**できます。

## 使用方法

### 1. プロジェクトにライブラリを追加

```xml
<PropertyGroup>
  <!-- Interceptor機能を有効にする -->
  <EnableWorkInterceptor>true</EnableWorkInterceptor>
</PropertyGroup>

<Import Project="..\WorkInterceptor.Library.props" />

<ItemGroup>
  <ProjectReference Include="..\WorkInterceptor.Library.Generator\WorkInterceptor.Library.Generator.csproj" 
                    OutputItemType="analyzer" 
                    ReferenceOutputAssembly="false" />
  <ProjectReference Include="..\WorkInterceptor.Library\WorkInterceptor.Library.csproj" />
</ItemGroup>
```

### 2. コマンドクラスを定義

```csharp
using WorkInterceptor.Library;

[Command("test")]
public sealed class TestCommand
{
    [Option(1, "name")]
    public string? Name { get; set; }

    [Option(2, "count")]
    public int Count { get; set; }

    [Option("verbose")]
    public bool Verbose { get; set; }
}

[Command("advanced")]
public sealed class AdvancedCommand
{
    // ジェネリック版のOptionAttribute<T>を使用
    [Option<int>(1, "port", Values = new[] { 8080, 8443, 3000 })]
    public int Port { get; set; }

    // C# 12のコレクション式もサポート
    [Option<string>("mode", Values = ["debug", "release", "test"])]
    public string? Mode { get; set; }

    // float値のサフィックスも保持される
    [Option<float>("score", Values = [0.5f, 0.75f, 1.0f])]
    public float Score { get; set; }

    // 非ジェネリック版でもValuesを指定可能
    [Option("input", Values = new[] { "file1.txt", "file2.txt" })]
    public string? InputFile { get; set; }
}
```

### 3. コードを記述

```csharp
var builder = new Builder();

// このメソッド呼び出しが自動的にinterceptされます
builder.Execute<TestCommand>();
builder.Execute<AdvancedCommand>();
```

### 4. 実行結果

**EnableWorkInterceptor=true の場合:**
```
Execute
Type: TestCommand
Command: test
Options:
  Property: Name, Type: string, Order: 1, Name: name, Attribute: OptionAttribute
  Property: Count, Type: int, Order: 2, Name: count, Attribute: OptionAttribute
  Property: Verbose, Type: bool, Order: 2147483647, Name: verbose, Attribute: OptionAttribute

Execute
Type: AdvancedCommand
Command: advanced
Options:
  Property: Port, Type: int, Order: 1, Name: port, Attribute: OptionAttribute<int>, Values (int[]): [8080, 8443, 3000]
  Property: Mode, Type: string, Order: 2147483647, Name: mode, Attribute: OptionAttribute<string>, Values (string[]): ["debug", "release", "test"]
  Property: Score, Type: float, Order: 2147483647, Name: score, Attribute: OptionAttribute<float>, Values (float[]): [0.5f, 0.75f, 1.0f]
  Property: InputFile, Type: string, Order: 2147483647, Name: input, Attribute: OptionAttribute, Values (string[]): ["file1.txt", "file2.txt"]
```

**EnableWorkInterceptor=false の場合:**
```
Execute
```

## オプション設定

### EnableWorkInterceptor

Interceptor機能の有効/無効を制御します。

- `true`: `IBuilder.Execute<T>()`が`Execute<T>(Action)`に置き換えられ、属性情報が出力されます
- `false`: Interceptorは生成されず、通常のメソッド呼び出しが行われます
- デフォルト: `false`（設定されていない場合）

## 属性

### CommandAttribute

クラスに適用して、コマンド情報を定義します。

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class CommandAttribute : Attribute
{
    public string Name { get; }
    
    public CommandAttribute(string name)
    {
        Name = name;
    }
}
```

### OptionAttribute（非ジェネリック版）

プロパティに適用して、オプション情報を定義します。

```csharp
[AttributeUsage(AttributeTargets.Property)]
public sealed class OptionAttribute : Attribute
{
    public int Order { get; }
    public string Name { get; }
    public string[] Values { get; set; } = [];
    
    // Orderを省略（int.MaxValueになります）
    public OptionAttribute(string name)
    {
        Order = int.MaxValue;
        Name = name;
    }
    
    // Orderを指定
    public OptionAttribute(int order, string name)
    {
        Order = order;
        Name = name;
    }
}
```

### OptionAttribute<T>（ジェネリック版）

型安全な値を持つオプションを定義できます。

```csharp
[AttributeUsage(AttributeTargets.Property)]
public sealed class OptionAttribute<T> : Attribute
{
    public int Order { get; }
    public string Name { get; }
    public T[] Values { get; set; } = [];
    
    public OptionAttribute(string name)
    {
        Order = int.MaxValue;
        Name = name;
    }
    
    public OptionAttribute(int order, string name)
    {
        Order = order;
        Name = name;
    }
}
```

### Valuesプロパティ

`OptionAttribute`と`OptionAttribute<T>`の両方で、`Values`プロパティを使用して選択可能な値のリストを指定できます：

```csharp
// 非ジェネリック版（string[]）- 従来の配列初期化構文
[Option("mode", Values = new[] { "debug", "release", "test" })]
public string? Mode { get; set; }

// ジェネリック版（T[]）- C# 12のコレクション式
[Option<string>("mode", Values = ["debug", "release", "test"])]
public string? Mode { get; set; }

// ジェネリック版（int[]）
[Option<int>("port", Values = [8080, 8443, 3000])]
public int Port { get; set; }

// float値のサフィックスも保持
[Option<float>("score", Values = [0.5f, 0.75f, 1.0f])]
public float Score { get; set; }
```

**サポートされる配列記法:**
- `new[] { ... }` - 従来の暗黙的型付き配列初期化
- `[ ... ]` - C# 12のコレクション式

**値の表示形式:**
- string値: ダブルクォーテーション付き（`"debug"`, `"file.txt"`）
- 数値のサフィックス: 元のまま保持（`0.5f`, `1.0d`）
- その他の値: ソースコードに記述された通りに表示

## 技術詳細

### ビルド時リフレクション

このSource Generatorは、コンパイル時にRoslynのセマンティックモデルを使用して型情報と属性情報を解析します：

1. **型情報の取得**: `ITypeSymbol`から型名を取得
2. **属性の解析**: `GetAttributes()`でCommandAttributeを検索
3. **プロパティの解析**: `GetMembers()`でOptionAttribute/OptionAttribute<T>付きプロパティを検索
4. **ジェネリック型の判定**: `OriginalDefinition.ToDisplayString()`でジェネリック版を識別
5. **Valuesプロパティの抽出**: `AttributeSyntax`から元のソースコードの構文をそのまま取得
   - `ImplicitArrayCreationExpressionSyntax`（`new[] { ... }`）
   - `CollectionExpressionSyntax`（`[ ... ]`）
6. **コード生成**: 解析した情報をConsole出力するActionを生成

これにより、実行時のリフレクションコストを削減し、パフォーマンスを向上させることができます。

### 構文の保持

Values配列の各要素は、`Expression.ToString()`を使用してソースコードに記述された通りの形式で保持されます：

- string値: `"debug"` → ダブルクォーテーション付きで表示
- float値: `0.5f` → サフィックス付きで表示
- double値: `1.0` → そのまま表示

### ジェネリック属性のサポート

C# 11で導入されたジェネリック属性をサポートしており、以下の機能を提供します：

- **型安全性**: `OptionAttribute<int>`のように型を指定することで、Valuesの型が保証される
- **自動判別**: Source Generatorが非ジェネリック版とジェネリック版を自動的に判別
- **情報出力**: 生成されたコードで`OptionAttribute`か`OptionAttribute<T>`かを明示

### InterceptableLocation API

このSource Generatorは、C# 12/13の新しい`InterceptableLocation` APIを使用しています：

- `SemanticModel.GetInterceptableLocation()`: 呼び出し位置の正確な情報を取得
- `InterceptsLocationAttribute(int version, string data)`: チェックサムベースの位置指定

### プロパティファイルの内容

`WorkInterceptor.Library.props`には以下の設定が含まれています：

```xml
<Project>
  <PropertyGroup>
    <InterceptorsNamespaces>$(InterceptorsNamespaces);WorkInterceptor.Library.Generated</InterceptorsNamespaces>
  </PropertyGroup>

  <ItemGroup>
    <CompilerVisibleProperty Include="EnableWorkInterceptor" />
  </ItemGroup>
</Project>
```

### サポートされる呼び出しパターン

以下のすべてのパターンで正しく動作します：

1. **直接呼び出し**: `builder.Execute<T>()`
2. **インターフェース型変数**: `IBuilder builder = ...; builder.Execute<T>()`
3. **実装クラス型変数**: `Builder builder = ...; builder.Execute<T>()`
4. **拡張メソッド内**: 拡張メソッドの中で呼び出されるメソッドもinterceptされます

## プロジェクト構成

- `WorkInterceptor.Library`: IBuilderインターフェース、Builderクラス、CommandAttribute、OptionAttribute、OptionAttribute<T>
- `WorkInterceptor.Library.Generator`: Source Generator実装
- `WorkInterceptor.Library.props`: 共通の設定ファイル
- `Develop`: サンプル使用例

## 要件

- .NET 8.0 以上
- C# 12 以上（Interceptors機能を使用）
- C# 11 以上推奨（ジェネリック属性のサポート）
- C# 12 以上推奨（コレクション式のサポート）
