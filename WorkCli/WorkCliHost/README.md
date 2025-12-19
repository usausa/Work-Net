# CLI Host Framework

System.CommandLineを使用した、属性ベースのCLIホストフレームワークです。

## 特徴

- ✅ **属性ベースの宣言的なコマンド定義**
- ✅ **階層的なコマンド構造のサポート**（サブサブコマンドまで無制限）
- ✅ **依存性注入（DI）のサポート**
- ✅ **型安全なジェネリック属性**
- ✅ **デフォルト値のサポート**
- ✅ **自動ヘルプ生成**
- ✅ **グループコマンドの自動ヘルプ表示**
- ✅ **共通引数の柔軟な定義パターン**
- ✅ **Position自動決定（省略可能）**

## 基本的な使い方

### 1. シンプルなコマンド

実行可能なコマンドは`ICommandDefinition`を実装します：

```csharp
[CliCommand("message", Description = "Show message")]
public sealed class MessageCommand : ICommandDefinition
{
    private readonly ILogger<MessageCommand> _logger;

    public MessageCommand(ILogger<MessageCommand> logger)
    {
        _logger = logger;
    }

    [CliArgument<string>(0, "text", Description = "Text to show", IsRequired = true)]
    public string Text { get; set; } = default!;

    public ValueTask ExecuteAsync()
    {
        _logger.LogInformation("Show {Text}", Text);
        Console.WriteLine(Text);
        return ValueTask.CompletedTask;
    }
}
```

### 2. グループコマンド（サブコマンドのみ）

サブコマンドのみを持つグループコマンドは`ICommandGroup`を実装します。
サブコマンドを指定せずに実行すると、自動的にヘルプが表示されます：

```csharp
[CliCommand("user", Description = "User management commands")]
public sealed class UserCommand : ICommandGroup
{
    // 実装不要 - サブコマンドのみのグループコマンド
}
```

### 3. 階層的なコマンド構造の登録

```csharp
var builder = CliHost.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    // シンプルなコマンド
    services.AddCliCommand<MessageCommand>();
    
    // 階層的なコマンド構造
    services.AddCliCommand<UserCommand>(user =>
    {
        user.AddSubCommand<UserListCommand>();
        user.AddSubCommand<UserAddCommand>();
        user.AddSubCommand<UserRoleCommand>(role =>
        {
            role.AddSubCommand<UserRoleAssignCommand>();
            role.AddSubCommand<UserRoleRemoveCommand>();
        });
    });
});

var host = builder.Build();
return await host.RunAsync();
```

### 4. 引数とデフォルト値

ジェネリック属性を使用して型安全な引数を定義できます：

```csharp
[CliCommand("greet", Description = "Greet someone")]
public sealed class GreetCommand : ICommandDefinition
{
    [CliArgument<string>(0, "name", Description = "Name to greet", IsRequired = true)]
    public string Name { get; set; } = default!;

    [CliArgument<string>(1, "greeting", Description = "Greeting message", IsRequired = false, DefaultValue = "Hello")]
    public string Greeting { get; set; } = default!;

    [CliArgument<int>(2, "count", Description = "Number of times to greet", IsRequired = false, DefaultValue = 1)]
    public int Count { get; set; }

    public ValueTask ExecuteAsync()
    {
        for (int i = 0; i < Count; i++)
        {
            Console.WriteLine($"{Greeting}, {Name}!");
        }
        return ValueTask.CompletedTask;
    }
}
```

## Position自動決定（NEW!）

`CliArgumentAttribute`の`Position`パラメータを省略できます。省略した場合、以下の順序で自動的に決定されます：

### Position省略の基本

```csharp
[CliCommand("set", Description = "Set configuration value")]
public sealed class ConfigSetCommand : ICommandDefinition
{
    // Position省略 - プロパティ定義順で自動決定される
    [CliArgument<string>("key", Description = "Configuration key")]
    public string Key { get; set; } = default!;

    [CliArgument<string>("value", Description = "Configuration value")]
    public string Value { get; set; } = default!;

    [CliArgument<string>("environment", Description = "Target environment", IsRequired = false, DefaultValue = "development")]
    public string Environment { get; set; } = default!;

    public ValueTask ExecuteAsync()
    {
        Console.WriteLine($"Set {Key}={Value} for environment '{Environment}'");
        return ValueTask.CompletedTask;
    }
}
```

**使用例**:
```bash
app config set database.host localhost
# Key=database.host, Value=localhost, Environment=development (デフォルト)

app config set database.host localhost production
# Key=database.host, Value=localhost, Environment=production
```

### Position明示指定と自動の混在

```csharp
[CliCommand("get", Description = "Get configuration value")]
public sealed class ConfigGetCommand : ICommandDefinition
{
    // Position明示指定 - 0番目
    [CliArgument<string>(0, "key", Description = "Configuration key")]
    public string Key { get; set; } = default!;

    // Position省略 - 明示的なPositionの後に自動配置される
    [CliArgument<string>("environment", Description = "Target environment", IsRequired = false, DefaultValue = "development")]
    public string Environment { get; set; } = default!;

    public ValueTask ExecuteAsync()
    {
        Console.WriteLine($"Getting {Key} for environment '{Environment}'");
        return ValueTask.CompletedTask;
    }
}
```

### 継承階層での自動Position決定

基底クラスのプロパティは派生クラスより先に配置されます：

```csharp
public abstract class DeploymentCommandBase : ICommandDefinition
{
    // 基底クラスのプロパティは先に来る（Position 0, 1相当）
    [CliArgument<string>("application", Description = "Application name")]
    public string Application { get; set; } = default!;

    [CliArgument<string>("version", Description = "Application version")]
    public string Version { get; set; } = default!;

    public abstract ValueTask ExecuteAsync();
}

[CliCommand("deploy", Description = "Deploy application")]
public sealed class DeployCommand : DeploymentCommandBase
{
    // 派生クラスのプロパティは基底クラスの後に来る（Position 2, 3相当）
    [CliArgument<string>("target", Description = "Deployment target", IsRequired = false, DefaultValue = "staging")]
    public string Target { get; set; } = default!;

    [CliArgument<bool>("force", Description = "Force deployment", IsRequired = false, DefaultValue = false)]
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
# application=MyApp, version=1.2.3, target=staging (デフォルト), force=false (デフォルト)

app deploy MyApp 1.2.3 production true
# application=MyApp, version=1.2.3, target=production, force=true
```

### Position決定のルール

1. **明示的なPosition指定がある引数**: 指定されたPositionで配置
2. **Position省略の引数**: 以下の順序で自動配置
   - 基底クラスのプロパティが優先（継承階層の上位から）
   - 同一クラス内ではプロパティ定義順

**例**:
```csharp
public abstract class BaseCommand : ICommandDefinition
{
    [CliArgument<string>("base-arg1", Description = "Base argument 1")]  // Position: 自動 → 0
    public string BaseArg1 { get; set; } = default!;

    [CliArgument<string>("base-arg2", Description = "Base argument 2")]  // Position: 自動 → 1
    public string BaseArg2 { get; set; } = default!;
}

public class DerivedCommand : BaseCommand
{
    [CliArgument<string>(10, "explicit", Description = "Explicit position")]  // Position: 明示 → 10
    public string Explicit { get; set; } = default!;

    [CliArgument<string>("derived-arg1", Description = "Derived argument 1")]  // Position: 自動 → 2
    public string DerivedArg1 { get; set; } = default!;

    [CliArgument<string>("derived-arg2", Description = "Derived argument 2")]  // Position: 自動 → 3
    public string DerivedArg2 { get; set; } = default!;
}
```

**結果の順序**: base-arg1(0), base-arg2(1), derived-arg1(2), derived-arg2(3), explicit(10)

## 共通引数の定義パターン

複数のコマンドで同じ引数を使用する場合、以下のパターンが利用できます。

### パターン1: 抽象基底クラス（推奨）

最もシンプルで型安全なアプローチ：

```csharp
/// <summary>
/// Base class for user role commands with common username and role arguments.
/// </summary>
public abstract class UserRoleCommandBase : ICommandDefinition
{
    [CliArgument<string>(0, "username", Description = "Username")]
    public string Username { get; set; } = default!;

    [CliArgument<string>(1, "role", Description = "Role name")]
    public string Role { get; set; } = default!;

    public abstract ValueTask ExecuteAsync();
}

[CliCommand("assign", Description = "Assign role to user")]
public sealed class UserRoleAssignCommand : UserRoleCommandBase
{
    private readonly ILogger<UserRoleAssignCommand> _logger;

    public UserRoleAssignCommand(ILogger<UserRoleAssignCommand> logger)
    {
        _logger = logger;
    }

    public override ValueTask ExecuteAsync()
    {
        _logger.LogInformation("Assigning role '{Role}' to user '{Username}'", Role, Username);
        Console.WriteLine($"Successfully assigned role '{Role}' to user '{Username}'");
        return ValueTask.CompletedTask;
    }
}
```

**Position省略版**（推奨）:
```csharp
public abstract class UserRoleCommandBase : ICommandDefinition
{
    // Position省略 - 基底クラスなので最初に来る
    [CliArgument<string>("username", Description = "Username")]
    public string Username { get; set; } = default!;

    [CliArgument<string>("role", Description = "Role name")]
    public string Role { get; set; } = default!;

    public abstract ValueTask ExecuteAsync();
}
```

### パターン2: インターフェース（ミックスイン）

複数の共通引数セットを組み合わせたい場合：

**注意**: C#ではインターフェースの属性は実装クラスに継承されないため、実装クラスで明示的に属性を付ける必要があります。インターフェースは型の契約と共通プロパティ名を定義する役割を果たします。

```csharp
public interface IUserTargetArguments
{
    string Username { get; set; }
}

public interface IRoleArguments
{
    string Role { get; set; }
}

[CliCommand("grant", Description = "Grant permission to user")]
public sealed class UserPermissionGrantCommand : ICommandDefinition, IUserTargetArguments, IRoleArguments
{
    // 属性を明示的に付ける
    [CliArgument<string>(0, "username", Description = "Username")]
    public string Username { get; set; } = default!;

    [CliArgument<string>(1, "role", Description = "Role name")]
    public string Role { get; set; } = default!;

    [CliArgument<string>(2, "permission", Description = "Permission to grant")]
    public string Permission { get; set; } = default!;

    public ValueTask ExecuteAsync()
    {
        Console.WriteLine($"Granted '{Permission}' in role '{Role}' to user '{Username}'");
        return ValueTask.CompletedTask;
    }
}
```

### パターン3: ジェネリック基底クラス

ロガーの型も含めて汎用化したい場合：

```csharp
public abstract class CommandWithUserAndRole<TLogger> : ICommandDefinition
{
    protected readonly ILogger<TLogger> Logger;

    protected CommandWithUserAndRole(ILogger<TLogger> logger)
    {
        Logger = logger;
    }

    [CliArgument<string>(0, "username", Description = "Username")]
    public string Username { get; set; } = default!;

    [CliArgument<string>(1, "role", Description = "Role name")]
    public string Role { get; set; } = default!;

    public abstract ValueTask ExecuteAsync();
}

[CliCommand("verify", Description = "Verify user role")]
public sealed class UserRoleVerifyCommand : CommandWithUserAndRole<UserRoleVerifyCommand>
{
    public UserRoleVerifyCommand(ILogger<UserRoleVerifyCommand> logger) : base(logger)
    {
    }

    public override ValueTask ExecuteAsync()
    {
        Logger.LogInformation("Verifying role '{Role}' for user '{Username}'", Role, Username);
        Console.WriteLine($"Verifying if user '{Username}' has role '{Role}'");
        return ValueTask.CompletedTask;
    }
}
```

### パターン比較

| パターン | メリット | デメリット | 使用場面 |
|---------|---------|-----------|----------|
| 抽象基底クラス | シンプル、型安全、IntelliSense良好、Position省略可 | 単一継承のみ | 関連するコマンドグループ |
| インターフェース | 複数の引数セットを組み合わせ可能 | プロパティ実装が必要、属性手動 | 横断的な共通引数 |
| ジェネリック基底 | ロガーも共通化、高い再利用性 | やや複雑 | 大規模プロジェクト |

## コマンドライン使用例

```bash
# シンプルなコマンド
app message "Hello, World!"

# デフォルト値を使用
app greet Alice
# 出力: Hello, Alice!

# カスタム値を指定
app greet Bob Hi 3
# 出力: Hi, Bob! (3回)

# グループコマンド（自動的にヘルプが表示される）
app user
# 出力: ヘルプメッセージ

# サブコマンド
app user list 5

# サブサブコマンド（共通引数を使用）
app user role assign john admin
app user role remove bob editor

# Position省略のコマンド
app config set database.host localhost
app config set database.host localhost production
app deploy MyApp 1.2.3
app deploy MyApp 1.2.3 production true

# ヘルプの表示
app --help
app user --help
app user role --help
```

## インターフェース

### ICommandDefinition

実行可能なコマンドを定義するインターフェース：

```csharp
public interface ICommandDefinition
{
    ValueTask ExecuteAsync();
}
```

### ICommandGroup

サブコマンドのみを持つグループコマンドを定義するマーカーインターフェース：

```csharp
public interface ICommandGroup
{
}
```

## 属性

### CliCommandAttribute

コマンドを定義する属性：

```csharp
[CliCommand("command-name", Description = "Command description")]
```

### CliArgumentAttribute<T>

型安全な引数を定義するジェネリック属性：

```csharp
// Position明示指定
[CliArgument<T>(position, "argument-name", 
    Description = "Argument description", 
    IsRequired = true,
    DefaultValue = defaultValue)]

// Position省略（推奨）
[CliArgument<T>("argument-name", 
    Description = "Argument description", 
    IsRequired = true,
    DefaultValue = defaultValue)]
```

## 設計の利点

1. **簡潔性**: グループコマンドに不要な実装を書く必要がない
2. **自動化**: サブコマンド未指定時のヘルプ表示が自動
3. **拡張性**: 無制限の階層深度に対応
4. **型安全性**: ジェネリック属性による型チェック
5. **保守性**: 明確な役割分担（ICommandDefinition vs ICommandGroup）
6. **DRY原則**: 共通引数の再利用パターンが豊富
7. **使いやすさ**: Position省略で定義が簡潔に
