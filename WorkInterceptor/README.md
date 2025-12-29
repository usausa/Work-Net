# template-library-with-generator

````````

This is the description of what the code block changes:
READMEを更新して使用方法と技術詳細を記載

This is the code block that represents the suggested code change:

````````markdown
# WorkInterceptor.Library

C# Interceptorsを使用したSource Generatorライブラリのサンプル実装です。

## 機能

このライブラリは、`IBuilder.Execute<T>()`メソッドの呼び出しを自動的にinterceptし、`IBuilder.Execute<T>(typeof(T))`の呼び出しに置き換えます。

## 使用方法

### 1. プロジェクトにライブラリを追加

```xml
<ItemGroup>
  <ProjectReference Include="..\WorkInterceptor.Library.Generator\WorkInterceptor.Library.Generator.csproj" 
                    OutputItemType="analyzer" 
                    ReferenceOutputAssembly="false" />
  <ProjectReference Include="..\WorkInterceptor.Library\WorkInterceptor.Library.csproj" />
</ItemGroup>

<Import Project="..\WorkInterceptor.Library.props" />
```

### 2. コードを記述

```csharp
using WorkInterceptor.Library;

var builder = new Builder();

// このメソッド呼び出しが自動的にinterceptされます
builder.Execute<string>();  // → builder.Execute<string>(typeof(string)) に置き換えられる
builder.Execute<int>();     // → builder.Execute<int>(typeof(int)) に置き換えられる
```

### 3. 実行結果

```
Execute System.String
Execute System.Int32
```

## 技術詳細

### InterceptableLocation API

このSource Generatorは、C# 12/13の新しい`InterceptableLocation` APIを使用しています：

- `SemanticModel.GetInterceptableLocation()`: 呼び出し位置の正確な情報を取得
- `InterceptsLocationAttribute(int version, string data)`: チェックサムベースの位置指定

これにより、以下の利点があります：
- 移植性の向上（異なるマシンでもコンパイル可能）
- 将来のエンコーディング変更に対する透明性
- 手動での行番号・文字位置計算が不要

### 名前空間の設定

Interceptorは`InterceptorsGenerated`名前空間に生成されます。この名前空間は`WorkInterceptor.Library.props`ファイルで自動的に有効化されるため、個別のプロジェクトで設定する必要はありません。

```xml
<!-- WorkInterceptor.Library.props -->
<PropertyGroup>
  <InterceptorsNamespaces>$(InterceptorsNamespaces);InterceptorsGenerated</InterceptorsNamespaces>
</PropertyGroup>
```

## プロジェクト構成

- `WorkInterceptor.Library`: IBuilderインターフェースとBuilderクラス
- `WorkInterceptor.Library.Generator`: Source Generator実装
- `Develop`: サンプル使用例

## 要件

- .NET 8.0 以上
- C# 12 以上（Interceptors機能を使用）

````````

This is the description of what the code block changes:
READMEを更新して名前空間の変更と拡張メソッドのサポートを記載

This is the code block that represents the suggested code change:

````````markdown
# WorkInterceptor.Library

C# Interceptorsを使用したSource Generatorライブラリのサンプル実装です。

## 機能

このライブラリは、`IBuilder.Execute<T>()`メソッドの呼び出しを自動的にinterceptし、`IBuilder.Execute<T>(typeof(T))`の呼び出しに置き換えます。

## 使用方法

### 1. プロジェクトにライブラリを追加

```xml
<ItemGroup>
  <ProjectReference Include="..\WorkInterceptor.Library.Generator\WorkInterceptor.Library.Generator.csproj" 
                    OutputItemType="analyzer" 
                    ReferenceOutputAssembly="false" />
  <ProjectReference Include="..\WorkInterceptor.Library\WorkInterceptor.Library.csproj" />
</ItemGroup>

<Import Project="..\WorkInterceptor.Library.props" />
```

### 2. コードを記述

```csharp
using WorkInterceptor.Library;

var builder = new Builder();

// このメソッド呼び出しが自動的にinterceptされます
builder.Execute<string>();  // → builder.Execute<string>(typeof(string)) に置き換えられる
builder.Execute<int>();     // → builder.Execute<int>(typeof(int)) に置き換えられる

// 拡張メソッド内の呼び出しもinterceptされます
public static class Extensions
{
    public static void AddExecutes(this IBuilder builder)
    {
        builder.Execute<DateTime>();  // → builder.Execute<DateTime>(typeof(DateTime)) に置き換えられる
        builder.Execute<Data>();      // → builder.Execute<Data>(typeof(Data)) に置き換えられる
    }
}
```

### 3. 実行結果

```
Execute System.String
Execute System.Int32
Execute System.Object
Execute System.DateTime
Execute Develop.Data
```

## 技術詳細

### InterceptableLocation API

このSource Generatorは、C# 12/13の新しい`InterceptableLocation` APIを使用しています：

- `SemanticModel.GetInterceptableLocation()`: 呼び出し位置の正確な情報を取得
- `InterceptsLocationAttribute(int version, string data)`: チェックサムベースの位置指定

これにより、以下の利点があります：
- 移植性の向上（異なるマシンでもコンパイル可能）
- 将来のエンコーディング変更に対する透明性
- 手動での行番号・文字位置計算が不要

### 名前空間の設定

Interceptorは`WorkInterceptor.Library.Generated`名前空間に生成されます。この名前空間は`WorkInterceptor.Library.props`ファイルで自動的に有効化されるため、個別のプロジェクトで設定する必要はありません。

```xml
<!-- WorkInterceptor.Library.props -->
<PropertyGroup>
  <InterceptorsNamespaces>$(InterceptorsNamespaces);WorkInterceptor.Library.Generated</InterceptorsNamespaces>
</PropertyGroup>
```

### サポートされる呼び出しパターン

以下のすべてのパターンで正しく動作します：

1. **直接呼び出し**: `builder.Execute<T>()`
2. **インターフェース型変数**: `IBuilder builder = ...; builder.Execute<T>()`
3. **実装クラス型変数**: `Builder builder = ...; builder.Execute<T>()`
4. **拡張メソッド内**: 拡張メソッドの中で呼び出されるメソッドもinterceptされます

## プロジェクト構成

- `WorkInterceptor.Library`: IBuilderインターフェースとBuilderクラス
- `WorkInterceptor.Library.Generator`: Source Generator実装
- `Develop`: サンプル使用例

## 要件

- .NET 8.0 以上
- C# 12 以上（Interceptors機能を使用）

````````


# Response
````````markdown
# WorkInterceptor.Library

C# Interceptorsを使用したSource Generatorライブラリのサンプル実装です。

## 機能

このライブラリは、`IBuilder.Execute<T>()`メソッドの呼び出しを自動的にinterceptし、`IBuilder.Execute<T>(typeof(T))`の呼び出しに置き換えます。

## 使用方法

### 1. プロジェクトにライブラリを追加

```xml
<ItemGroup>
  <ProjectReference Include="..\WorkInterceptor.Library.Generator\WorkInterceptor.Library.Generator.csproj" 
                    OutputItemType="analyzer" 
                    ReferenceOutputAssembly="false" />
  <ProjectReference Include="..\WorkInterceptor.Library\WorkInterceptor.Library.csproj" />
</ItemGroup>

<Import Project="..\WorkInterceptor.Library.props" />
```

### 2. コードを記述

```csharp
using WorkInterceptor.Library;

var builder = new Builder();

// このメソッド呼び出しが自動的にinterceptされます
builder.Execute<string>();  // → builder.Execute<string>(typeof(string)) に置き換えられる
builder.Execute<int>();     // → builder.Execute<int>(typeof(int)) に置き換えられる

// 拡張メソッド内の呼び出しもinterceptされます
public static class Extensions
{
    public static void AddExecutes(this IBuilder builder)
    {
        builder.Execute<DateTime>();  // → builder.Execute<DateTime>(typeof(DateTime)) に置き換えられる
        builder.Execute<Data>();      // → builder.Execute<Data>(typeof(Data)) に置き換えられる
    }
}
```

### 3. 実行結果

```
Execute System.String
Execute System.Int32
Execute System.Object
Execute System.DateTime
Execute Develop.Data
```

## 技術詳細

### InterceptableLocation API

このSource Generatorは、C# 12/13の新しい`InterceptableLocation` APIを使用しています：

- `SemanticModel.GetInterceptableLocation()`: 呼び出し位置の正確な情報を取得
- `InterceptsLocationAttribute(int version, string data)`: チェックサムベースの位置指定

これにより、以下の利点があります：
- 移植性の向上（異なるマシンでもコンパイル可能）
- 将来のエンコーディング変更に対する透明性
- 手動での行番号・文字位置計算が不要

### 名前空間の設定

Interceptorは`WorkInterceptor.Library.Generated`名前空間に生成されます。この名前空間は`WorkInterceptor.Library.props`ファイルで自動的に有効化されるため、個別のプロジェクトで設定する必要はありません。

```xml
<!-- WorkInterceptor.Library.props -->
<PropertyGroup>
  <InterceptorsNamespaces>$(InterceptorsNamespaces);WorkInterceptor.Library.Generated</InterceptorsNamespaces>
</PropertyGroup>
```

### サポートされる呼び出しパターン

以下のすべてのパターンで正しく動作します：

1. **直接呼び出し**: `builder.Execute<T>()`
2. **インターフェース型変数**: `IBuilder builder = ...; builder.Execute<T>()`
3. **実装クラス型変数**: `Builder builder = ...; builder.Execute<T>()`
4. **拡張メソッド内**: 拡張メソッドの中で呼び出されるメソッドもinterceptされます

## プロジェクト構成

- `WorkInterceptor.Library`: IBuilderインターフェースとBuilderクラス
- `WorkInterceptor.Library.Generator`: Source Generator実装
- `Develop`: サンプル使用例

## 要件

- .NET 8.0 以上
- C# 12 以上（Interceptors機能を使用）
