# フィルタ機構 - 実装完了

ASP.NET Coreライクなフィルタ機構が実装されました。

## 実装された機能

### ✅ 完了した機能

1. **4種類のフィルタインターフェース**
   - `ICommandExecutionFilter` - 前後処理
   - `IBeforeCommandFilter` - 前処理のみ
   - `IAfterCommandFilter` - 後処理のみ
   - `IExceptionFilter` - 例外処理

2. **CommandContext**
   - フィルタとコマンド間でのデータ共有
   - `Items` - キー/値のコレクション
   - `ExitCode` - 終了コード
   - `IsShortCircuited` - 実行の中断

3. **フィルタ適用方法**
   - コマンド個別: `[CommandFilter<T>]` 属性
   - グローバル: `services.AddGlobalCommandFilter<T>()`
   - 基底クラス: 継承による自動適用

4. **Order制御**
   - 明示的なOrder指定
   - 基底クラス→派生クラスの自動順序
   - 前処理は昇順、後処理は降順

5. **自動DI登録**
   - 使用されているフィルタを自動検出してDI登録

## 使用方法

### 1. フィルタの定義

```csharp
public sealed class LoggingFilter : ICommandExecutionFilter
{
    private readonly ILogger<LoggingFilter> _logger;

    public LoggingFilter(ILogger<LoggingFilter> logger)
    {
        _logger = logger;
    }

    public int Order => 0;

    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        _logger.LogInformation("Before: {CommandType}", context.CommandType.Name);
        await next();
        _logger.LogInformation("After: {CommandType}", context.CommandType.Name);
    }
}
```

### 2. コマンドへの適用

```csharp
[CommandFilter<TimingFilter>(Order = -100)]
[CommandFilter<LoggingFilter>]
[CliCommand("process", Description = "Process data")]
public sealed class ProcessCommand : ICommandDefinition
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        // フィルタデータへのアクセス
        if (context.Items.TryGetValue("StartTime", out var startTime))
        {
            Console.WriteLine($"Started at: {startTime}");
        }
        
        // 処理
        Console.WriteLine("Processing...");
        
        return ValueTask.CompletedTask;
    }
}
```

### 3. グローバルフィルタの登録

```csharp
// Program.cs
builder.ConfigureServices(services =>
{
    services.AddGlobalCommandFilter<TimingFilter>(order: -100);
    services.AddGlobalCommandFilter<ExceptionHandlingFilter>(order: int.MaxValue);
    
    services.AddCliCommand<ProcessCommand>();
});
```

## 実装済みの共通フィルタ

### TimingFilter
- Order: -100
- 実行時間を計測して表示

### LoggingFilter
- Order: 0
- コマンド実行の前後でログ出力

### ExceptionHandlingFilter
- Order: int.MaxValue
- 例外を捕捉してログ出力、適切なExitCodeを設定

## 実行フロー例

```
[グローバル] TimingFilter.Before (Order: -100)
  [コマンド] LoggingFilter.Before (Order: 0)
    ★ コマンド実行
  [コマンド] LoggingFilter.After
[グローバル] TimingFilter.After
```

例外発生時:
```
[グローバル] ExceptionHandlingFilter.OnException (Order: int.MaxValue)
```

## テストコマンド

### test-filter
フィルタの基本動作をテスト

```bash
dotnet run -- test-filter "Hello!"
```

### test-exception
例外フィルタの動作をテスト

```bash
dotnet run -- test-exception argument
dotnet run -- test-exception file
dotnet run -- test-exception unauthorized
```

## 実装の特徴

1. **ASP.NET Core互換の設計**
   - インターフェースと属性の命名
   - Orderによる実行順序制御
   - グローバルフィルタのサポート

2. **自動化**
   - フィルタの自動DI登録
   - 継承階層の自動検出

3. **柔軟性**
   - 4種類のフィルタインターフェース
   - グローバルとコマンド個別の両対応
   - ショートサーキット機能

4. **型安全性**
   - ジェネリック属性による型チェック
   - DIによる依存関係管理

## 次のステップ

さらに高度なフィルタを実装できます：

- **AuthorizationFilter** - 認証・認可
- **ValidationFilter** - バリデーション
- **TransactionFilter** - トランザクション管理
- **CachingFilter** - キャッシング
- **RateLimitFilter** - レート制限
- **CorrelationIdFilter** - 相関ID管理

これらはFilters/CommandFilterInterfaces.csに実装例があります。
