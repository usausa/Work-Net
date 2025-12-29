# WorkInterceptor.Library

C# Interceptorsを使用したSource Generatorライブラリのサンプル実装です。

## 機能

このライブラリは、`IBuilder.Execute<T>()`メソッドの呼び出しを自動的にinterceptし、`IBuilder.Execute<T>(typeof(T))`の呼び出しに置き換えます。

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

**注意:** `WorkInterceptor.Library.props`をインポートすることで、必要な設定（`InterceptorsNamespaces`と`CompilerVisibleProperty`）が自動的に適用されます。

### 2. コードを記述

```csharp
using WorkInterceptor.Library;

var builder = new Builder();

// このメソッド呼び出しが自動的にinterceptされます（EnableWorkInterceptor=trueの場合）
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

**EnableWorkInterceptor=true の場合:**
```
Execute System.String
Execute System.Int32
Execute System.Object
Execute System.DateTime
Execute Develop.Data
```

**EnableWorkInterceptor=false の場合:**
```
Execute
Execute
Execute
Execute
Execute
```

## オプション設定

### EnableWorkInterceptor

Interceptor機能の有効/無効を制御します。

- `true`: `IBuilder.Execute<T>()`の呼び出しが`Execute<T>(typeof(T))`に置き換えられます
- `false`: Interceptorは生成されず、通常のメソッド呼び出しが行われます
- デフォルト: `false`（設定されていない場合）

**設定例:**
```xml
<PropertyGroup>
  <EnableWorkInterceptor>true</EnableWorkInterceptor>
</PropertyGroup>
```

このプロパティは`WorkInterceptor.Library.props`をインポートすることで、自動的にSource Generatorから読み取れるようになります。

## 技術詳細

### プロパティファイルの内容

`WorkInterceptor.Library.props`には以下の設定が含まれています：

```xml
<Project>
  <PropertyGroup>
    <!-- Interceptorの名前空間を有効化 -->
    <InterceptorsNamespaces>$(InterceptorsNamespaces);WorkInterceptor.Library.Generated</InterceptorsNamespaces>
  </PropertyGroup>

  <ItemGroup>
    <!-- Source GeneratorからEnableWorkInterceptorプロパティを読み取れるようにする -->
    <CompilerVisibleProperty Include="EnableWorkInterceptor" />
  </ItemGroup>
</Project>
```

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

### サポートされる呼び出しパターン

以下のすべてのパターンで正しく動作します：

1. **直接呼び出し**: `builder.Execute<T>()`
2. **インターフェース型変数**: `IBuilder builder = ...; builder.Execute<T>()`
3. **実装クラス型変数**: `Builder builder = ...; builder.Execute<T>()`
4. **拡張メソッド内**: 拡張メソッドの中で呼び出されるメソッドもinterceptされます

## プロジェクト構成

- `WorkInterceptor.Library`: IBuilderインターフェースとBuilderクラス
- `WorkInterceptor.Library.Generator`: Source Generator実装
- `WorkInterceptor.Library.props`: 共通の設定ファイル
- `Develop`: サンプル使用例

## 要件

- .NET 8.0 以上
- C# 12 以上（Interceptors機能を使用）
