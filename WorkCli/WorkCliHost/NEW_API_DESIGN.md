# 新しいAPI設計 - 責任分離と型安全性

## 設計の動機

従来の設計では、`ConfigureServices`で以下の両方を行っていました：

1. アプリケーションサービスの登録（DBコンテキスト、HTTPクライアント等）
2. コマンドとフィルタの登録

これにより、以下の問題がありました：

- 責任が不明確
- `IServiceCollection`への直接アクセスによる型安全性の欠如
- コマンド関連の設定が散在
- RootCommandの設定方法が不統一

## 新しい設計

### 明確な責任分離

```csharp
var builder = CliHost.CreateDefaultBuilder(args);

// 1. アプリケーションサービス（コマンド以外）
builder.ConfigureServices(services =>
{
    services.AddDbContext<AppDbContext>();
    services.AddHttpClient<IApiClient, ApiClient>();
    services.AddSingleton<IEmailService, EmailService>();
});

// 2. コマンド関連の設定
builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root => { });
    commands.AddGlobalFilter<TimingFilter>();
    commands.AddCommand<MyCommand>();
});
```

### インターフェース階層

```
ICliHostBuilder
├── ConfigureServices(Action<IServiceCollection>)
└── ConfigureCommands(Action<ICommandConfigurator>)

ICommandConfigurator
├── AddCommand<T>(Action<ISubCommandConfigurator>?)
├── AddGlobalFilter<T>(int order)
├── ConfigureRootCommand(Action<IRootCommandConfigurator>)
└── ConfigureFilterOptions(Action<CommandFilterOptions>)

ISubCommandConfigurator
└── AddSubCommand<T>(Action<ISubCommandConfigurator>?)

IRootCommandConfigurator
├── WithDescription(string)
├── WithName(string)
├── UseCustomRootCommand(RootCommand)
└── Configure(Action<RootCommand>)
```

## 利点

### 1. 明確な責任分離

**Before**:
```csharp
builder.ConfigureServices(services =>
{
    // アプリケーションサービス
    services.AddDbContext<AppDbContext>();
    
    // コマンド設定（混在）
    services.AddGlobalCommandFilter<TimingFilter>();
    services.AddCliCommand<MessageCommand>();
});

builder.ConfigureCommands(root =>
{
    // RootCommandの設定だけ
    root.Description = "My CLI";
});
```

**After**:
```csharp
builder.ConfigureServices(services =>
{
    // アプリケーションサービスのみ
    services.AddDbContext<AppDbContext>();
});

builder.ConfigureCommands(commands =>
{
    // コマンド関連の設定すべて
    commands.ConfigureRootCommand(root => 
        root.WithDescription("My CLI"));
    commands.AddGlobalFilter<TimingFilter>();
    commands.AddCommand<MessageCommand>();
});
```

### 2. 型安全性の向上

**Before**:
```csharp
// IServiceCollectionへの直接アクセス
builder.ConfigureServices(services =>
{
    services.AddCliCommand<MessageCommand>();
    // 誤って別のメソッドを呼ぶ可能性
    services.AddSingleton<MessageCommand>(); // ❌ 重複登録
});
```

**After**:
```csharp
// ICommandConfiguratorを通してのみアクセス
builder.ConfigureCommands(commands =>
{
    commands.AddCommand<MessageCommand>();
    // IntelliSenseでコマンド関連のメソッドのみ表示
});
```

### 3. 一貫性

すべてのコマンド関連の設定が同じスコープ：

```csharp
builder.ConfigureCommands(commands =>
{
    // すべて同じレベルで設定
    commands.ConfigureRootCommand(/* ... */);
    commands.AddGlobalFilter<TimingFilter>();
    commands.AddCommand<MyCommand>();
    commands.ConfigureFilterOptions(/* ... */);
});
```

### 4. 発見可能性

IntelliSenseで利用可能なメソッドが明確：

```csharp
builder.ConfigureCommands(commands =>
{
    commands. // ← ここでIntelliSenseを表示
    // - AddCommand
    // - AddGlobalFilter
    // - ConfigureRootCommand
    // - ConfigureFilterOptions
});
```

### 5. 拡張性

新しいコマンド関連の機能を追加しやすい：

```csharp
public interface ICommandConfigurator
{
    // 既存
    ICommandConfigurator AddCommand<T>(...);
    ICommandConfigurator AddGlobalFilter<T>(...);
    
    // 将来追加可能
    ICommandConfigurator AddCommandValidator<T>(...);
    ICommandConfigurator AddCommandMiddleware<T>(...);
    ICommandConfigurator ConfigureCommandConventions(...);
}
```

## 使用例

### 基本的な構成

```csharp
var builder = CliHost.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    services.AddDbContext<AppDbContext>();
});

builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("My Application")
            .WithName("myapp");
    });
    
    commands.AddCommand<MessageCommand>();
});

var host = builder.Build();
return await host.RunAsync();
```

### フィルタ付き

```csharp
builder.ConfigureCommands(commands =>
{
    // グローバルフィルタ
    commands.AddGlobalFilter<TimingFilter>(order: -100);
    commands.AddGlobalFilter<LoggingFilter>(order: 0);
    commands.AddGlobalFilter<ExceptionHandlingFilter>(order: int.MaxValue);
    
    // フィルタオプション
    commands.ConfigureFilterOptions(options =>
    {
        options.IncludeBaseClassFilters = true;
        options.DefaultFilterOrder = 0;
    });
    
    // コマンド
    commands.AddCommand<ProcessCommand>();
});
```

### 階層的なコマンド

```csharp
builder.ConfigureCommands(commands =>
{
    commands.AddCommand<UserCommand>(user =>
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
```

### カスタムRootCommand

```csharp
builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        var customRoot = new RootCommand("Custom root description");
        // カスタム設定...
        root.UseCustomRootCommand(customRoot);
    });
});
```

## 移行ガイド

### Before → After

#### コマンド登録

```csharp
// Before
services.AddCliCommand<MessageCommand>();

// After
commands.AddCommand<MessageCommand>();
```

#### サブコマンド登録

```csharp
// Before
services.AddCliCommand<UserCommand>(user =>
{
    user.AddSubCommand<UserListCommand>();
});

// After
commands.AddCommand<UserCommand>(user =>
{
    user.AddSubCommand<UserListCommand>();
});
```

#### グローバルフィルタ

```csharp
// Before
services.AddGlobalCommandFilter<TimingFilter>(order: -100);

// After
commands.AddGlobalFilter<TimingFilter>(order: -100);
```

#### RootCommand設定

```csharp
// Before
builder.ConfigureCommands(root =>
{
    root.Description = "My CLI";
});

// After
commands.ConfigureRootCommand(root =>
{
    root.WithDescription("My CLI");
});
```

## 実装の詳細

### CommandConfigurator

```csharp
internal sealed class CommandConfigurator : ICommandConfigurator
{
    private readonly IServiceCollection _services;
    private readonly List<CommandRegistration> _commandRegistrations = new();
    private readonly CommandFilterOptions _filterOptions = new();
    
    public ICommandConfigurator AddCommand<TCommand>(...)
    {
        // DIに登録
        _services.AddTransient<TCommand>();
        
        // コマンド登録情報を保存
        _commandRegistrations.Add(...);
        
        return this;
    }
    
    public ICommandConfigurator AddGlobalFilter<TFilter>(...)
    {
        // フィルタオプションに追加
        _filterOptions.GlobalFilters.Add(...);
        
        // DIに登録
        _services.AddTransient<TFilter>();
        
        return this;
    }
}
```

### 自動化されている処理

1. **フィルタの自動DI登録**
   - `AddGlobalFilter`で自動的にDI登録
   - コマンド属性からフィルタを検出して自動登録

2. **コマンドの自動DI登録**
   - `AddCommand`で自動的にDI登録
   - `ICommandDefinition`を実装している場合のみ

3. **フィルタオプションの統合**
   - `CommandConfigurator`で収集したオプションを自動的に`IOptions<CommandFilterOptions>`に登録

## まとめ

新しいAPI設計により：

- ✅ 責任が明確（サービス vs コマンド）
- ✅ 型安全性が向上（専用configurator）
- ✅ 一貫性のある設定（1か所に集約）
- ✅ 発見しやすい（IntelliSense）
- ✅ 拡張しやすい（新メソッド追加が容易）
- ✅ ASP.NET Coreと同様の設計思想

これにより、大規模なCLIアプリケーションでも保守性の高いコードを維持できます。
