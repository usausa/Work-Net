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
| 抽象基底クラス | シンプル、型安全、IntelliSense良好 | 単一継承のみ | 関連するコマンドグループ |
| インターフェース | 複数の引数セットを組み合わせ可能 | プロパティ実装が必要 | 横断的な共通引数 |
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
[CliArgument<T>(position, "argument-name", 
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
