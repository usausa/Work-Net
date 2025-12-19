# Position自動決定機能

## 概要

`CliArgumentAttribute`の`Position`パラメータを省略できるようになりました。省略した場合、継承階層とプロパティ定義順に基づいて自動的に決定されます。

## 基本的な使い方

### Before（Position明示指定）

```csharp
[CliCommand("set", Description = "Set configuration value")]
public sealed class ConfigSetCommand : ICommandDefinition
{
    [CliArgument<string>(0, "key", Description = "Configuration key")]
    public string Key { get; set; } = default!;

    [CliArgument<string>(1, "value", Description = "Configuration value")]
    public string Value { get; set; } = default!;

    [CliArgument<string>(2, "environment", Description = "Target environment", IsRequired = false, DefaultValue = "development")]
    public string Environment { get; set; } = default!;
}
```

### After（Position省略）⭐推奨⭐

```csharp
[CliCommand("set", Description = "Set configuration value")]
public sealed class ConfigSetCommand : ICommandDefinition
{
    [CliArgument<string>("key", Description = "Configuration key")]
    public string Key { get; set; } = default!;

    [CliArgument<string>("value", Description = "Configuration value")]
    public string Value { get; set; } = default!;

    [CliArgument<string>("environment", Description = "Target environment", IsRequired = false, DefaultValue = "development")]
    public string Environment { get; set; } = default!;
}
```

## Position決定のルール

### 優先順位

1. **明示的なPosition指定** - 最優先
2. **継承階層** - 基底クラスのプロパティが先
3. **定義順** - 同一クラス内ではプロパティ定義順

### 具体例

```csharp
public abstract class BaseCommand : ICommandDefinition
{
    // 自動Position: 0
    [CliArgument<string>("base-arg1", Description = "Base argument 1")]
    public string BaseArg1 { get; set; } = default!;

    // 自動Position: 1
    [CliArgument<string>("base-arg2", Description = "Base argument 2")]
    public string BaseArg2 { get; set; } = default!;

    public abstract ValueTask ExecuteAsync();
}

public sealed class DerivedCommand : BaseCommand
{
    // 明示的Position: 10
    [CliArgument<string>(10, "explicit", Description = "Explicit position")]
    public string Explicit { get; set; } = default!;

    // 自動Position: 2（基底クラスの後）
    [CliArgument<string>("derived-arg1", Description = "Derived argument 1")]
    public string DerivedArg1 { get; set; } = default!;

    // 自動Position: 3
    [CliArgument<string>("derived-arg2", Description = "Derived argument 2")]
    public string DerivedArg2 { get; set; } = default!;

    public override ValueTask ExecuteAsync()
    {
        Console.WriteLine($"Args: {BaseArg1}, {BaseArg2}, {DerivedArg1}, {DerivedArg2}, {Explicit}");
        return ValueTask.CompletedTask;
    }
}
```

**結果の引数順序**:
```
<base-arg1> <base-arg2> <derived-arg1> <derived-arg2> ... <explicit>
   (0)          (1)          (2)            (3)              (10)
```

## 実用例

### 例1: シンプルなコマンド

```csharp
[CliCommand("create", Description = "Create a new resource")]
public sealed class CreateCommand : ICommandDefinition
{
    [CliArgument<string>("name", Description = "Resource name")]
    public string Name { get; set; } = default!;

    [CliArgument<string>("type", Description = "Resource type", DefaultValue = "default")]
    public string Type { get; set; } = default!;

    public ValueTask ExecuteAsync()
    {
        Console.WriteLine($"Creating {Type} resource: {Name}");
        return ValueTask.CompletedTask;
    }
}
```

**使用例**:
```bash
app create myresource
# name=myresource, type=default

app create myresource custom
# name=myresource, type=custom
```

### 例2: 継承を使った共通引数

```csharp
public abstract class DeploymentCommandBase : ICommandDefinition
{
    [CliArgument<string>("application", Description = "Application name")]
    public string Application { get; set; } = default!;

    [CliArgument<string>("version", Description = "Application version")]
    public string Version { get; set; } = default!;

    public abstract ValueTask ExecuteAsync();
}

[CliCommand("deploy", Description = "Deploy application")]
public sealed class DeployCommand : DeploymentCommandBase
{
    [CliArgument<string>("target", Description = "Deployment target", DefaultValue = "staging")]
    public string Target { get; set; } = default!;

    [CliArgument<bool>("force", Description = "Force deployment", DefaultValue = false)]
    public bool Force { get; set; }

    public override ValueTask ExecuteAsync()
    {
        Console.WriteLine($"Deploying {Application} v{Version} to {Target}{(Force ? " (forced)" : "")}");
        return ValueTask.CompletedTask;
    }
}
```

**使用例**:
```bash
app deploy MyApp 1.2.3
# application=MyApp, version=1.2.3, target=staging, force=false

app deploy MyApp 1.2.3 production true
# application=MyApp, version=1.2.3, target=production, force=true
```

### 例3: Position明示指定と自動の混在

特定の引数だけを後ろに配置したい場合：

```csharp
[CliCommand("search", Description = "Search resources")]
public sealed class SearchCommand : ICommandDefinition
{
    // 自動Position: 0
    [CliArgument<string>("query", Description = "Search query")]
    public string Query { get; set; } = default!;

    // 自動Position: 1
    [CliArgument<int>("limit", Description = "Result limit", DefaultValue = 10)]
    public int Limit { get; set; }

    // 明示的Position: 10（最後に配置）
    [CliArgument<bool>(10, "verbose", Description = "Verbose output", DefaultValue = false)]
    public bool Verbose { get; set; }

    public ValueTask ExecuteAsync()
    {
        Console.WriteLine($"Searching '{Query}' (limit: {Limit}, verbose: {Verbose})");
        return ValueTask.CompletedTask;
    }
}
```

**結果**: `<query> <limit> ... <verbose>`

## ベストプラクティス

### ✅ 推奨

1. **基本的にPosition省略を使用**
   ```csharp
   [CliArgument<string>("name", Description = "Name")]
   public string Name { get; set; } = default!;
   ```

2. **継承を活用**
   - 基底クラスに共通引数を定義
   - Position省略で自動的に正しい順序になる

3. **プロパティの定義順に注意**
   - 引数として表示したい順序でプロパティを定義

### ⚠️ 注意

1. **明示的Position指定が必要な場合**
   - 特定の引数を最後に配置したい
   - 非連続なPositionが必要
   ```csharp
   [CliArgument<string>(0, "first", Description = "First")]
   public string First { get; set; } = default!;

   [CliArgument<bool>(100, "last", Description = "Last")]
   public bool Last { get; set; }
   ```

2. **既存コードの移行**
   - Position明示指定は引き続きサポート
   - 段階的な移行が可能

## トラブルシューティング

### 問題: 引数の順序が期待と異なる

**原因**: プロパティの定義順が意図と異なる

**解決策**:
1. プロパティの定義順を変更する
2. または、明示的にPositionを指定する

### 問題: 基底クラスと派生クラスの順序

**確認**: 基底クラスのプロパティは常に派生クラスより先に来る

**期待する動作**:
```csharp
// 基底クラス
[CliArgument<string>("base-arg")]  // Position: 0
public string BaseArg { get; set; } = default!;

// 派生クラス
[CliArgument<string>("derived-arg")]  // Position: 1
public string DerivedArg { get; set; } = default!;
```

## まとめ

Position自動決定機能により：

- ✅ **コードが簡潔に**: Position番号の管理が不要
- ✅ **保守性向上**: プロパティの追加・削除が容易
- ✅ **直感的**: 定義順＝引数順
- ✅ **継承と相性良好**: 基底クラスの共通引数が自然に先に来る
- ✅ **後方互換性**: 明示的Position指定も引き続きサポート

**推奨**: 新規コードではPosition省略を使用し、必要な場合のみ明示的に指定する。
