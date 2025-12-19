# コマンドフィルタ機構

ASP.NET Coreライクなフィルタ機構の設計と実装イメージ。

## 概要

コマンド実行の前後で処理を挿入できるフィルタ機構を提供します。ASP.NET Coreのフィルタと同様の設計です。

## フィルタの種類

### 1. ICommandExecutionFilter

コマンド実行の前後で処理を行うフィルタ。最も柔軟性が高い。

```csharp
public interface ICommandExecutionFilter : ICommandFilter
{
    ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next);
}
```

**使用例**:
```csharp
public sealed class LoggingFilter : ICommandExecutionFilter
{
    private readonly ILogger<LoggingFilter> _logger;
    
    public int Order => 0;

    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        _logger.LogInformation("Before command: {CommandType}", context.CommandType.Name);
        
        await next(); // 次のフィルタまたはコマンド実行
        
        _logger.LogInformation("After command: {CommandType}", context.CommandType.Name);
    }
}
```

### 2. IBeforeCommandFilter

コマンド実行前にのみ処理を行うフィルタ。

```csharp
public interface IBeforeCommandFilter : ICommandFilter
{
    ValueTask OnBeforeExecutionAsync(CommandContext context);
}
```

**使用例**:
```csharp
public sealed class AuthorizationFilter : IBeforeCommandFilter
{
    public int Order => -1000;

    public ValueTask OnBeforeExecutionAsync(CommandContext context)
    {
        if (!IsAuthorized())
        {
            context.IsShortCircuited = true;
            context.ExitCode = 403;
        }
        return ValueTask.CompletedTask;
    }
}
```

### 3. IAfterCommandFilter

コマンド実行後にのみ処理を行うフィルタ。

```csharp
public interface IAfterCommandFilter : ICommandFilter
{
    ValueTask OnAfterExecutionAsync(CommandContext context);
}
```

**使用例**:
```csharp
public sealed class CleanupFilter : IAfterCommandFilter
{
    public int Order => 1000;

    public ValueTask OnAfterExecutionAsync(CommandContext context)
    {
        // クリーンアップ処理
        return ValueTask.CompletedTask;
    }
}
```

### 4. IExceptionFilter

例外発生時に処理を行うフィルタ。

```csharp
public interface IExceptionFilter : ICommandFilter
{
    ValueTask OnExceptionAsync(CommandContext context, Exception exception);
}
```

**使用例**:
```csharp
public sealed class ExceptionHandlingFilter : IExceptionFilter
{
    public int Order => int.MaxValue;

    public ValueTask OnExceptionAsync(CommandContext context, Exception exception)
    {
        context.ExitCode = exception switch
        {
            ArgumentException => 400,
            UnauthorizedAccessException => 403,
            FileNotFoundException => 404,
            _ => 500
        };
        
        Console.Error.WriteLine($"Error: {exception.Message}");
        return ValueTask.CompletedTask;
    }
}
```

## CommandContext

フィルタとコマンド間でデータを共有するためのコンテキスト。

```csharp
public sealed class CommandContext
{
    // データ共有用の辞書
    public IDictionary<string, object?> Items { get; }
    
    // 実行中のコマンド情報
    public ICommandDefinition Command { get; }
    public Type CommandType { get; }
    
    // キャンセルトークン
    public CancellationToken CancellationToken { get; }
    
    // 終了コード
    public int ExitCode { get; set; }
    
    // 実行の中断フラグ
    public bool IsShortCircuited { get; set; }
}
```

## フィルタの適用方法

### 1. 個別のコマンドに適用

```csharp
[CommandFilter<TimingFilter>(Order = -100)]
[CommandFilter<LoggingFilter>(Order = 0)]
[CommandFilter<CleanupFilter>(Order = 1000)]
[CliCommand("process", Description = "Process data")]
public sealed class ProcessCommand : ICommandDefinition
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        // コマンド処理
        return ValueTask.CompletedTask;
    }
}
```

### 2. グローバルフィルタ（全コマンドに適用）

```csharp
// Program.cs
builder.ConfigureServices(services =>
{
    services.AddGlobalCommandFilter<TimingFilter>(order: -100);
    services.AddGlobalCommandFilter<LoggingFilter>(order: 0);
    services.AddGlobalCommandFilter<ExceptionHandlingFilter>(order: int.MaxValue);
});
```

### 3. 基底クラスでの適用（継承）

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
    public override ValueTask ExecuteAsync(CommandContext context)
    {
        // 基底クラスのフィルタも自動適用される
        return ValueTask.CompletedTask;
    }
}
```

## 実行順序

### Order値による順序決定

1. **明示的なOrder指定**: `Order`プロパティの値（小さい方が先）
2. **Order未指定**: 基底クラス→派生クラスの順
3. **同じOrder値**: 定義順（属性の記述順）

### 実行フロー

```
┌─────────────────────────────────────────────────────────────┐
│ グローバルフィルタ (Order: -2000)                              │
│   CorrelationIdFilter.Before                                │
├─────────────────────────────────────────────────────────────┤
│ グローバルフィルタ (Order: -1000)                              │
│   AuthorizationFilter.Before                                │
├─────────────────────────────────────────────────────────────┤
│ コマンドフィルタ (Order: -500)                                 │
│   ValidationFilter.Before                                   │
├─────────────────────────────────────────────────────────────┤
│ グローバルフィルタ (Order: -100)                               │
│   TimingFilter.Before                                       │
├─────────────────────────────────────────────────────────────┤
│ グローバルフィルタ (Order: 0)                                  │
│   LoggingFilter.Before                                      │
├─────────────────────────────────────────────────────────────┤
│ ★ コマンド実行 (ExecuteAsync)                                 │
├─────────────────────────────────────────────────────────────┤
│ グローバルフィルタ (Order: 0)                                  │
│   LoggingFilter.After                                       │
├─────────────────────────────────────────────────────────────┤
│ グローバルフィルタ (Order: -100)                               │
│   TimingFilter.After                                        │
├─────────────────────────────────────────────────────────────┤
│ コマンドフィルタ (Order: -500)                                 │
│   (AfterFilterなし)                                         │
├─────────────────────────────────────────────────────────────┤
│ グローバルフィルタ (Order: -1000)                              │
│   (AfterFilterなし)                                         │
├─────────────────────────────────────────────────────────────┤
│ グローバルフィルタ (Order: -2000)                              │
│   CorrelationIdFilter.After                                 │
└─────────────────────────────────────────────────────────────┘
```

**ポイント**:
- Before処理: Order昇順（小→大）
- After処理: Order降順（大→小）= Before の逆順
- 例外が発生した場合: ExceptionFilterが実行される

## ショートサーキット

フィルタで`context.IsShortCircuited = true`を設定すると、以降の処理をスキップできます。

```csharp
public ValueTask OnBeforeExecutionAsync(CommandContext context)
{
    if (!IsAuthorized())
    {
        context.IsShortCircuited = true;
        context.ExitCode = 403;
        return ValueTask.CompletedTask;
    }
    return ValueTask.CompletedTask;
}
```

**動作**:
1. AuthorizationFilter が `IsShortCircuited = true` を設定
2. 以降のBeforeフィルタとコマンド実行はスキップ
3. Afterフィルタは実行される（逆順）

## データ共有の例

### フィルタでデータを設定

```csharp
public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
{
    var correlationId = Guid.NewGuid().ToString();
    context.Items["CorrelationId"] = correlationId;
    context.Items["StartTime"] = DateTime.UtcNow;
    
    await next();
    
    var duration = DateTime.UtcNow - (DateTime)context.Items["StartTime"]!;
    context.Items["Duration"] = duration;
}
```

### コマンドでデータを参照

```csharp
public ValueTask ExecuteAsync(CommandContext context)
{
    var correlationId = context.Items["CorrelationId"]?.ToString();
    _logger.LogInformation("Processing with correlation ID: {CorrelationId}", correlationId);
    
    // コマンド処理
    
    // フィルタ用にデータを設定
    context.Items["ProcessedRecords"] = 100;
    
    return ValueTask.CompletedTask;
}
```

## 典型的なフィルタの組み合わせ

### 基本セット（推奨）

```csharp
services.AddGlobalCommandFilter<CorrelationIdFilter>(order: -2000);
services.AddGlobalCommandFilter<TimingFilter>(order: -100);
services.AddGlobalCommandFilter<LoggingFilter>(order: 0);
services.AddGlobalCommandFilter<ExceptionHandlingFilter>(order: int.MaxValue);
```

### セキュリティ重視

```csharp
services.AddGlobalCommandFilter<AuthorizationFilter>(order: -1000);
services.AddGlobalCommandFilter<ValidationFilter>(order: -500);
services.AddGlobalCommandFilter<RateLimitFilter>(order: -800);
services.AddGlobalCommandFilter<ExceptionHandlingFilter>(order: int.MaxValue);
```

### トランザクション対応

```csharp
services.AddGlobalCommandFilter<TransactionFilter>(order: -200);
services.AddGlobalCommandFilter<ExceptionHandlingFilter>(order: int.MaxValue);
```

## コマンド定義の変更

フィルタ機構の導入により、`ICommandDefinition`のシグネチャが変更されます：

### Before
```csharp
public interface ICommandDefinition
{
    ValueTask ExecuteAsync();
}
```

### After
```csharp
public interface ICommandDefinition
{
    ValueTask ExecuteAsync(CommandContext context);
}
```

### 移行例

```csharp
// Before
public ValueTask ExecuteAsync()
{
    _logger.LogInformation("Executing command");
    Console.WriteLine("Hello");
    return ValueTask.CompletedTask;
}

// After
public ValueTask ExecuteAsync(CommandContext context)
{
    var correlationId = context.Items["CorrelationId"];
    _logger.LogInformation("Executing command with ID: {CorrelationId}", correlationId);
    Console.WriteLine("Hello");
    
    context.ExitCode = 0; // 明示的に終了コードを設定可能
    return ValueTask.CompletedTask;
}
```

## まとめ

この設計により：

- ✅ ASP.NET Coreと同様のフィルタ機構
- ✅ 柔軟なOrder指定による実行順序制御
- ✅ グローバルフィルタとコマンド個別フィルタの両方をサポート
- ✅ 継承階層でのフィルタ適用
- ✅ CommandContextによるデータ共有
- ✅ ショートサーキットによる処理の中断
- ✅ 例外ハンドリングのサポート

横断的関心事（ロギング、認証、バリデーション、トランザクション等）を統一的に扱えるようになります。
