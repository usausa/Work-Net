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

# サブサブコマンド
app user role assign john admin

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
