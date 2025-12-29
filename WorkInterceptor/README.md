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
```

### 3. コードを記述

```csharp
var builder = new Builder();

// このメソッド呼び出しが自動的にinterceptされます
builder.Execute<TestCommand>();
```

### 4. 実行結果

**EnableWorkInterceptor=true の場合:**
```
Execute
Type: TestCommand
Command: test
Options:
  Property: Name, Type: string, Order: 1, Name: name
  Property: Count, Type: int, Order: 2, Name: count
  Property: Verbose, Type: bool, Order: 2147483647, Name: verbose
```

生成されるコード（イメージ）:
```csharp
[InterceptsLocation(1, @"...")]
internal static void Execute_Interceptor_0<T>(this Builder builder)
{
    void Action_0()
    {
        Console.WriteLine("Type: TestCommand");
        Console.WriteLine("Command: test");
        Console.WriteLine("Options:");
        Console.WriteLine("  Property: Name, Type: string, Order: 1, Name: name");
        Console.WriteLine("  Property: Count, Type: int, Order: 2, Name: count");
        Console.WriteLine("  Property: Verbose, Type: bool, Order: 2147483647, Name: verbose");
    }

    builder.Execute<T>(Action_0);
}
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

### OptionAttribute

プロパティに適用して、オプション情報を定義します。

```csharp
[AttributeUsage(AttributeTargets.Property)]
public sealed class OptionAttribute : Attribute
{
    public int Order { get; }
    public string Name { get; }
    
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

## 技術詳細

### ビルド時リフレクション

このSource Generatorは、コンパイル時にRoslynのセマンティックモデルを使用して型情報と属性情報を解析します：

1. **型情報の取得**: `ITypeSymbol`から型名を取得
2. **属性の解析**: `GetAttributes()`でCommandAttributeを検索
3. **プロパティの解析**: `GetMembers()`でOptionAttribute付きプロパティを検索
4. **コード生成**: 解析した情報をConsole出力するActionを生成

これにより、実行時のリフレクションコストを削減し、パフォーマンスを向上させることができます。

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

- `WorkInterceptor.Library`: IBuilderインターフェース、Builderクラス、CommandAttribute、OptionAttribute
- `WorkInterceptor.Library.Generator`: Source Generator実装
- `WorkInterceptor.Library.props`: 共通の設定ファイル
- `Develop`: サンプル使用例

## 要件

- .NET 8.0 以上
- C# 12 以上（Interceptors機能を使用）
