# API設計ガイド

## 目次

- [基本設計思想](#基本設計思想)
- [ホストビルダーAPI](#ホストビルダーapi)
- [コマンド定義API](#コマンド定義api)
- [フィルター機構](#フィルター機構)
- [引数定義](#引数定義)
- [使用例](#使用例)

---

## 基本設計思想

WorkCliHost.Coreは、ASP.NET Coreの設計思想を取り入れたCLIフレームワークです。

### 設計原則

1. **明確な責任分離**
   - アプリケーションサービスの設定 (`Services`)
   - CLI固有の設定 (`ConfigureCommands`)

2. **型安全性**
   - 専用のconfigurator interface経由でのみアクセス
   - IntelliSenseによる発見可能性

3. **プロパティベースAPI**
   - ASP.NET Coreの`HostApplicationBuilder`に倣った設計
   - `Configuration`, `Services`, `Logging`への直接アクセス

4. **オプトイン方式**
   - 最小構成から開始
   - 必要な機能のみを追加

---

## ホストビルダーAPI

### ファクトリメソッド

```csharp
public static class CliHost
{
    // 最小構成版（推奨）
    public static ICliHostBuilder CreateBuilder(string[] args);
    
    // フル機能版
    public static ICliHostBuilder CreateDefaultBuilder(string[] args);
}
```

#### CreateBuilder (最小構成版) ⭐推奨⭐

**特徴**:
- Console loggingのみ
- 高速起動（50-100ms高速化）
- シンプルなCLIアプリに最適

**使用例**:
```csharp
var builder = CliHost.CreateBuilder(args);

builder.ConfigureCommands(commands =>
{
    commands.AddCommand<SimpleCommand>();
});

var host = builder.Build();
return await host.RunAsync();
```

#### CreateDefaultBuilder (フル機能版)

**特徴**:
- `appsettings.json` 自動読み込み
- 環境変数サポート
- Configuration logging
- 従来のフル機能が必要な場合に使用

**使用例**:
```csharp
var builder = CliHost.CreateDefaultBuilder(args);

// デフォルトで以下が設定済み:
// - appsettings.json
// - appsettings.{Environment}.json
// - 環境変数
// - Console + Configuration logging

builder.Services.AddDbContext<AppDbContext>();
builder.ConfigureCommands(commands =>
{
    commands.AddCommand<ComplexCommand>();
});

var host = builder.Build();
return await host.RunAsync();
```

### プロパティベースAPI

```csharp
public interface ICliHostBuilder
{
    ConfigurationManager Configuration { get; }
    IHostEnvironment Environment { get; }
    IServiceCollection Services { get; }
    ILoggingBuilder Logging { get; }
    
    void ConfigureContainer<TContainerBuilder>(
        IServiceProviderFactory<TContainerBuilder> factory,
        Action<TContainerBuilder>? configure = null)
        where TContainerBuilder : notnull;
    
    ICliHostBuilder ConfigureCommands(Action<ICommandConfigurator> configure);
    ICliHostBuilder Build();
}
```

**使用例**:
```csharp
var builder = CliHost.CreateBuilder(args);

// Configuration設定
builder.Configuration.AddJsonFile("config.json");

// サービス登録
builder.Services.AddDbContext<MyDbContext>();
builder.Services.AddSingleton<IEmailService, EmailService>();

// Logging設定
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// カスタムDIコンテナの設定（オプション）
// builder.ConfigureContainer(new AutofacServiceProviderFactory(), container =>
// {
//     container.RegisterType<MyService>().As<IMyService>();
// });

// コマンド設定（分離）
builder.ConfigureCommands(commands =>
{
    commands.AddCommand<MyCommand>();
});
```

### ConfigureContainer（カスタムDIコンテナ）

```csharp
void ConfigureContainer<TContainerBuilder>(
    IServiceProviderFactory<TContainerBuilder> factory,
    Action<TContainerBuilder>? configure = null)
    where TContainerBuilder : notnull;
```

**用途**:
- デフォルトの`ServiceProvider`を別のDIコンテナに置き換える
- Autofac、DryIoc、Grace等のサードパーティDIコンテナを使用

**使用例（Autofac）**:
```csharp
using Autofac;
using Autofac.Extensions.DependencyInjection;

var builder = CliHost.CreateBuilder(args);

// Autofacコンテナを使用
builder.ConfigureContainer(new AutofacServiceProviderFactory(), container =>
{
    // Autofac固有の登録
    container.RegisterType<MyService>()
        .As<IMyService>()
        .SingleInstance();
    
    // モジュールの登録
    container.RegisterModule<MyAutofacModule>();
    
    // アセンブリスキャン
    container.RegisterAssemblyTypes(typeof(Program).Assembly)
        .Where(t => t.Name.EndsWith("Repository"))
        .AsImplementedInterfaces();
});

builder.ConfigureCommands(commands =>
{
    commands.AddCommand<MyCommand>();
});

var host = builder.Build();
return await host.RunAsync();
```

**使用例（カスタムファクトリ）**:
```csharp
var builder = CliHost.CreateBuilder(args);

// カスタムファクトリを使用
builder.ConfigureContainer(new CustomServiceProviderFactory(), container =>
{
    // カスタムコンテナの設定
    container.AddSingleton<ICustomService, CustomService>();
});
```

### 拡張メソッド

```csharp
public static class CliHostBuilderExtensions
{
    // 標準設定セット
    public static ICliHostBuilder UseDefaults(this ICliHostBuilder builder);
    public static ICliHostBuilder UseDefaultConfiguration(this ICliHostBuilder builder);
    public static ICliHostBuilder UseDefaultLogging(this ICliHostBuilder builder);

    // Configuration
    public static ICliHostBuilder AddJsonFile(this ICliHostBuilder builder, 
        string path, bool optional = true, bool reloadOnChange = true);
    public static ICliHostBuilder AddEnvironmentVariables(this ICliHostBuilder builder, 
        string? prefix = null);
    public static ICliHostBuilder AddUserSecrets<T>(this ICliHostBuilder builder) 
        where T : class;

    // Logging
    public static ICliHostBuilder SetMinimumLogLevel(this ICliHostBuilder builder, 
        LogLevel level);
    public static ICliHostBuilder AddLoggingFilter(this ICliHostBuilder builder, 
        Func<string, LogLevel, bool> filter);
    public static ICliHostBuilder AddDebugLogging(this ICliHostBuilder builder);
}
```

**使用例**:
```csharp
var builder = CliHost.CreateBuilder(args)
    .AddJsonFile("settings.json", optional: true)
    .AddEnvironmentVariables("MYAPP_")
    .SetMinimumLogLevel(LogLevel.Warning)
    .AddDebugLogging();

// または、まとめて設定
var builder = CliHost.CreateBuilder(args)
    .UseDefaults(); // = UseDefaultConfiguration() + UseDefaultLogging()
```

### 重要な設計決定

#### ConfigurationManager.AddCommandLine を使用しない

**理由**:
- `System.CommandLine`との競合
- `AddCommandLine(args)`は全ての引数を設定値として解釈
- 例: `app user add john` → `user`, `add`, `john`を設定キーとして解釈

**解決策**:
- `AddCommandLine()`は呼び出さない
- コマンドライン引数は`System.CommandLine`で処理
- 設定はJSON/環境変数で行う

---

## コマンド定義API

### コマンド設定

```csharp
public interface ICommandConfigurator
{
    ICommandConfigurator AddCommand<TCommand>(
        Action<ISubCommandConfigurator>? configure = null) 
        where TCommand : class;
    
    ICommandConfigurator AddGlobalFilter<TFilter>(int order = 0) 
        where TFilter : class, ICommandFilter;
    
    ICommandConfigurator AddGlobalFilter(Type filterType, int order = 0);
    
    ICommandConfigurator ConfigureRootCommand(
        Action<IRootCommandConfigurator> configure);
    
    ICommandConfigurator ConfigureFilterOptions(
        Action<CommandFilterOptions> configure);
}
```

### 基本的なコマンド登録

```csharp
builder.ConfigureCommands(commands =>
{
    // シンプルなコマンド
    commands.AddCommand<MessageCommand>();
    
    // 階層的なコマンド
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
    
    // グローバルフィルタ
    commands.AddGlobalFilter<TimingFilter>(order: -100);
    commands.AddGlobalFilter<LoggingFilter>();
    commands.AddGlobalFilter<ExceptionHandlingFilter>(order: int.MaxValue);
    
    // RootCommand設定
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("My Application")
            .WithName("myapp");
    });
});
```

### グループコマンド vs 実行可能コマンド

#### グループコマンド（サブコマンドのみ）

```csharp
// ICommandDefinition非実装 = グループコマンド
[CliCommand("user", Description = "User management")]
public sealed class UserCommand
{
    // 空のクラス - サブコマンドのみ
}

// 実行: app user → ヘルプ表示
```

#### 実行可能コマンド

```csharp
// ICommandDefinition実装 = 実行可能
[CliCommand("greet", Description = "Greet someone")]
public sealed class GreetCommand : ICommandDefinition
{
    [CliArgument<string>("name")]
    public string Name { get; set; } = default!;
    
    public ValueTask ExecuteAsync(CommandContext context)
    {
        Console.WriteLine($"Hello, {Name}!");
        return ValueTask.CompletedTask;
    }
}

// 実行: app greet Alice → "Hello, Alice!"
```

#### ハイブリッド（実行可能 + サブコマンド）

```csharp
[CliCommand("git", Description = "Git operations")]
public sealed class GitCommand : ICommandDefinition
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        Console.WriteLine("Git version 2.0");
        return ValueTask.CompletedTask;
    }
}

// 登録
commands.AddCommand<GitCommand>(git =>
{
    git.AddSubCommand<GitCloneCommand>();
    git.AddSubCommand<GitCommitCommand>();
});

// 実行:
// app git           → "Git version 2.0"
// app git clone ... → GitCloneCommand実行
```

### RootCommand設定

```csharp
public interface IRootCommandConfigurator
{
    IRootCommandConfigurator WithDescription(string description);
    IRootCommandConfigurator WithName(string name);
    IRootCommandConfigurator UseCustomRootCommand(RootCommand rootCommand);
    IRootCommandConfigurator Configure(Action<RootCommand> configure);
}
```

**使用例**:
```csharp
commands.ConfigureRootCommand(root =>
{
    root.WithDescription("My CLI Application")
        .WithName("mycli")
        .Configure(cmd =>
        {
            // System.CommandLine.RootCommandへの直接アクセス
            cmd.AddValidator(result => { /* ... */ });
        });
});
```

---

## フィルター機構

### ICommandExecutionFilter

コマンド実行の完全な制御を提供する統一フィルターインターフェース。

```csharp
public interface ICommandExecutionFilter : ICommandFilter
{
    ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next);
}
```

このフィルターは以下の全ての機能を提供します：
- **実行前処理**: `next()`呼び出し前のコードで実現
- **実行後処理**: `next()`呼び出し後のコードで実現
- **例外処理**: `try-catch`で`next()`をラップ
- **ショートサーキット**: `next()`を呼ば ずにreturn

#### 実行前処理の例

```csharp
public sealed class AuthorizationFilter : ICommandExecutionFilter
{
    public int Order => -1000;

    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        // 実行前: 認可チェック
        if (!IsAuthorized())
        {
            context.ExitCode = 403;
            return; // next()を呼ばない = ショートサーキット
        }

        await next(context); // 認可OKなら次へ
    }
}
```

#### 実行後処理の例

```csharp
public sealed class CleanupFilter : ICommandExecutionFilter
{
    public int Order => 1000;

    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        await next(context); // コマンド実行

        // 実行後: クリーンアップ
        if (context.Items.TryGetValue("TempFiles", out var files))
        {
            DeleteTempFiles((List<string>)files!);
        }
    }
}
```

#### 例外処理の例

```csharp
public sealed class ExceptionHandlingFilter : ICommandExecutionFilter
{
    private readonly ILogger _logger;

    public int Order => int.MaxValue;

    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Command failed");
            
            context.ExitCode = exception switch
            {
                ArgumentException => 400,
                FileNotFoundException => 404,
                UnauthorizedAccessException => 403,
                _ => 500
            };
            
            Console.Error.WriteLine($"❌ {exception.Message}");
        }
    }
}
```

#### 実行時間計測の例

```csharp
public sealed class TimingFilter : ICommandExecutionFilter
{
    public int Order => -100;

    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        var sw = Stopwatch.StartNew();
        
        await next(context); // 実行前後でラップ
        
        sw.Stop();
        Console.WriteLine($"⏱  {sw.ElapsedMilliseconds}ms");
    }
}
```

### フィルターの適用方法

#### グローバルフィルタ（全コマンドに適用）

```csharp
builder.ConfigureCommands(commands =>
{
    commands.AddGlobalFilter<TimingFilter>(order: -100);
    commands.AddGlobalFilter<LoggingFilter>(order: 0);
    commands.AddGlobalFilter<ExceptionHandlingFilter>(order: int.MaxValue);
});
```

#### コマンド固有のフィルタ

```csharp
[CommandFilter<AuthorizationFilter>(Order = -1000)]
[CommandFilter<ValidationFilter>(Order = -500)]
[CliCommand("secure", Description = "Secure command")]
public sealed class SecureCommand : ICommandDefinition
{
    // このコマンドのみに適用される
}
```

#### 基底クラスでの適用（継承）

```csharp
[CommandFilter<LoggingFilter>]
[CommandFilter<ExceptionHandlingFilter>(Order = int.MaxValue)]
public abstract class AuditedCommandBase : ICommandDefinition
{
    public abstract ValueTask ExecuteAsync(CommandContext context);
}

[CommandFilter<ValidationFilter>(Order = -500)]
[CliCommand("update", Description = "Update resource")]
public sealed class UpdateCommand : AuditedCommandBase
{
    // 基底クラスのフィルタも自動適用
    public override ValueTask ExecuteAsync(CommandContext context)
    {
        // ...
        return ValueTask.CompletedTask;
    }
}
```

### CommandContext

フィルタとコマンド間でデータを共有するためのコンテキスト。

```csharp
public sealed class CommandContext
{
    public Type CommandType { get; }                    // コマンド型
    public ICommandDefinition Command { get; }          // コマンドインスタンス
    public int ExitCode { get; set; }                   // 終了コード
    public Dictionary<string, object?> Items { get; }   // データ共有用
    public CancellationToken CancellationToken { get; set; } // キャンセル通知
}
```

**使用例**:
```csharp
// フィルタでデータを設定
public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
{
    context.Items["StartTime"] = DateTime.UtcNow;
    context.Items["CorrelationId"] = Guid.NewGuid();
    
    await next();
    
    var elapsed = DateTime.UtcNow - (DateTime)context.Items["StartTime"]!;
    Console.WriteLine($"Duration: {elapsed.TotalMilliseconds}ms");
}

// コマンドでデータを参照
public ValueTask ExecuteAsync(CommandContext context)
{
    var correlationId = context.Items["CorrelationId"];
    _logger.LogInformation("Processing with ID: {Id}", correlationId);
    
    // 処理...
    
    return ValueTask.CompletedTask;
}
```

### フィルター実行順序

**Order値による順序**:
- 小さい値が先に実行される
- デフォルトは `0`
- 負の値で優先度を上げる（例: `-100`）
- `int.MaxValue` で最後に実行

**実行フロー**:
```
Before処理: Order昇順（-1000 → 0 → 1000）
  ↓
コマンド実行
  ↓
After処理: Order降順（1000 → 0 → -1000）= Before の逆順
```

**典型的な組み合わせ**:
```csharp
commands.AddGlobalFilter<CorrelationIdFilter>(order: -2000);  // 最初
commands.AddGlobalFilter<AuthorizationFilter>(order: -1000);
commands.AddGlobalFilter<ValidationFilter>(order: -500);
commands.AddGlobalFilter<TimingFilter>(order: -100);
commands.AddGlobalFilter<LoggingFilter>(order: 0);
commands.AddGlobalFilter<ExceptionHandlingFilter>(order: int.MaxValue);  // 最後
```

### ショートサーキット

`next(context)`を呼ばずにreturnすることで、以降の処理をスキップできます。

```csharp
public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
{
    if (!IsAuthorized())
    {
        context.ExitCode = 403;
        Console.Error.WriteLine("Access denied");
        return; // next(context)を呼ばない = ショートサーキット
    }
    
    await next(context); // 認可OKなら続行
}
```

**動作**:
- AuthorizationFilterが`next(context)`を呼ばずにreturn
- コマンド実行とそれ以降のフィルタはスキップされる

**ポイント**:
- フラグ不要: `next(context)`を呼ぶか呼ばないかで制御
- ASP.NET Coreミドルウェアと同じパターン

---

## 引数定義

### CliArgumentAttribute

```csharp
// ジェネリック版（推奨）
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class CliArgumentAttribute<T> : Attribute
{
    public const int AutoPosition = -1;
    
    // Position省略可能
    public CliArgumentAttribute(string name);
    
    // Position明示指定
    public CliArgumentAttribute(int position, string name);
    
    public string Name { get; }
    public int Position { get; }
    public string? Description { get; set; }
    public bool IsRequired { get; set; } = true;
    public T? DefaultValue { get; set; }
}
```

### Position自動決定 ⭐推奨⭐

Position パラメータを省略すると、継承階層とプロパティ定義順に基づいて自動決定されます。

#### 基本例

```csharp
[CliCommand("create", Description = "Create resource")]
public sealed class CreateCommand : ICommandDefinition
{
    // Position: 0（自動）
    [CliArgument<string>("name", Description = "Resource name")]
    public string Name { get; set; } = default!;

    // Position: 1（自動）
    [CliArgument<string>("type", Description = "Resource type", DefaultValue = "default")]
    public string Type { get; set; } = default!;

    public ValueTask ExecuteAsync(CommandContext context)
    {
        Console.WriteLine($"Creating {Type} resource: {Name}");
        return ValueTask.CompletedTask;
    }
}
```

**使用例**:
```bash
app create myresource          # name=myresource, type=default
app create myresource custom   # name=myresource, type=custom
```

#### 継承での使用

```csharp
public abstract class DeploymentCommandBase : ICommandDefinition
{
    // Position: 0（自動）
    [CliArgument<string>("application", Description = "Application name")]
    public string Application { get; set; } = default!;

    // Position: 1（自動）
    [CliArgument<string>("version", Description = "Application version")]
    public string Version { get; set; } = default!;

    public abstract ValueTask ExecuteAsync(CommandContext context);
}

[CliCommand("deploy", Description = "Deploy application")]
public sealed class DeployCommand : DeploymentCommandBase
{
    // Position: 2（自動 - 基底クラスの後）
    [CliArgument<string>("target", Description = "Deployment target", DefaultValue = "staging")]
    public string Target { get; set; } = default!;

    // Position: 3（自動）
    [CliArgument<bool>("force", Description = "Force deployment", DefaultValue = false)]
    public bool Force { get; set; }

    public override ValueTask ExecuteAsync(CommandContext context)
    {
        Console.WriteLine($"Deploying {Application} v{Version} to {Target}");
        return ValueTask.CompletedTask;
    }
}
```

**使用例**:
```bash
app deploy MyApp 1.2.3                    # target=staging, force=false
app deploy MyApp 1.2.3 production true    # target=production, force=true
```

### Position決定のルール

1. **明示的Position指定** - 最優先
2. **継承階層** - 基底クラスのプロパティが先
3. **定義順** - 同一クラス内ではプロパティ定義順

### Position明示指定と自動の混在

特定の引数だけを後ろに配置したい場合：

```csharp
[CliCommand("search", Description = "Search resources")]
public sealed class SearchCommand : ICommandDefinition
{
    // Position: 0（自動）
    [CliArgument<string>("query", Description = "Search query")]
    public string Query { get; set; } = default!;

    // Position: 1（自動）
    [CliArgument<int>("limit", Description = "Result limit", DefaultValue = 10)]
    public int Limit { get; set; }

    // Position: 10（明示 - 最後に配置）
    [CliArgument<bool>(10, "verbose", Description = "Verbose output", DefaultValue = false)]
    public bool Verbose { get; set; }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        Console.WriteLine($"Searching '{Query}' (limit: {Limit}, verbose: {Verbose})");
        return ValueTask.CompletedTask;
    }
}
```

**結果**: `<query> <limit> ... <verbose>`

### デフォルト値とオプション引数

```csharp
// 必須引数
[CliArgument<string>("name", Description = "User name")]
public string Name { get; set; } = default!;

// オプション引数（デフォルト値あり）
[CliArgument<string>("role", Description = "User role", DefaultValue = "user", IsRequired = false)]
public string Role { get; set; } = default!;

// オプション引数（デフォルト値なし）
[CliArgument<string>("email", Description = "Email address", IsRequired = false)]
public string? Email { get; set; }
```

### ベストプラクティス

✅ **推奨**:
1. 基本的にPosition省略を使用
2. 継承を活用して共通引数を基底クラスに定義
3. プロパティの定義順 = 引数の順序

⚠️ **注意**:
1. 特定の引数を最後に配置したい場合は明示的Position指定
2. 非連続なPositionが必要な場合は明示的指定

---

## 使用例

### 最小構成のシンプルなCLI

```csharp
using WorkCliHost.Core;

var builder = CliHost.CreateBuilder(args);

builder.ConfigureCommands(commands =>
{
    commands.AddCommand<GreetCommand>();
});

var host = builder.Build();
return await host.RunAsync();

[CliCommand("greet", Description = "Greet someone")]
public sealed class GreetCommand : ICommandDefinition
{
    [CliArgument<string>("name")]
    public string Name { get; set; } = default!;
    
    public ValueTask ExecuteAsync(CommandContext context)
    {
        Console.WriteLine($"Hello, {Name}!");
        return ValueTask.CompletedTask;
    }
}
```

### フル機能のエンタープライズCLI

```csharp
using WorkCliHost.Core;
using Microsoft.Extensions.Logging;

var builder = CliHost.CreateDefaultBuilder(args);

// Configuration
builder.Configuration.AddJsonFile("appsettings.json");
builder.Configuration.AddEnvironmentVariables("MYAPP_");

// Services
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddHttpClient<IApiClient, ApiClient>();
builder.Services.AddSingleton<IEmailService, EmailService>();

// Logging
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Commands
builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("Enterprise CLI Application")
            .WithName("entapp");
    });
    
    // Global filters
    commands.AddGlobalFilter<CorrelationIdFilter>(order: -2000);
    commands.AddGlobalFilter<AuthorizationFilter>(order: -1000);
    commands.AddGlobalFilter<ValidationFilter>(order: -500);
    commands.AddGlobalFilter<TimingFilter>(order: -100);
    commands.AddGlobalFilter<LoggingFilter>(order: 0);
    commands.AddGlobalFilter<ExceptionHandlingFilter>(order: int.MaxValue);
    
    // Commands with hierarchy
    commands.AddCommand<UserCommand>(user =>
    {
        user.AddSubCommand<UserListCommand>();
        user.AddSubCommand<UserAddCommand>();
        user.AddSubCommand<UserUpdateCommand>();
        user.AddSubCommand<UserDeleteCommand>();
        user.AddSubCommand<UserRoleCommand>(role =>
        {
            role.AddSubCommand<UserRoleAssignCommand>();
            role.AddSubCommand<UserRoleRemoveCommand>();
            role.AddSubCommand<UserRoleListCommand>();
        });
    });
    
    commands.AddCommand<DataCommand>(data =>
    {
        data.AddSubCommand<DataImportCommand>();
        data.AddSubCommand<DataExportCommand>();
        data.AddSubCommand<DataMigrateCommand>();
    });
});

var host = builder.Build();
return await host.RunAsync();
```

### カスタム設定のCLI

```csharp
var builder = CliHost.CreateBuilder(args);

// 必要な機能だけを追加
builder
    .AddJsonFile("settings.json", optional: true)
    .AddEnvironmentVariables("MYAPP_")
    .SetMinimumLogLevel(LogLevel.Warning)
    .AddDebugLogging();

// カスタムサービス
builder.Services.AddSingleton<IMyService, MyService>();

builder.ConfigureCommands(commands =>
{
    commands.AddGlobalFilter<TimingFilter>(order: -100);
    commands.AddCommand<MyCommand>();
});

var host = builder.Build();
return await host.RunAsync();
```

---

## まとめ

WorkCliHost.Core のAPI設計により:

- ✅ **明確な責任分離**: Services vs Commands
- ✅ **型安全性**: 専用configurator interface
- ✅ **プロパティベースAPI**: Configuration/Services/Loggingへの直接アクセス
- ✅ **オプトイン方式**: 最小構成から開始、必要な機能のみ追加
- ✅ **Position自動決定**: シンプルで保守性の高いコード
- ✅ **統一フィルター機構**: ICommandExecutionFilterによる柔軟な実装
- ✅ **発見可能性**: IntelliSenseによる優れた開発体験
- ✅ **拡張性**: 新機能の追加が容易

ASP.NET Core開発者にとって馴染みのある設計により、学習コストを最小限に抑えながら強力な機能を提供します。
