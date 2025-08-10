namespace WorkMiddleware2;

using System.Diagnostics;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal class Program
{
    static void Main()
    {
        BenchmarkRunner.Run<PipelineBenchmarks>();
    }
}

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class PipelineBenchmarks
{
    private Func<PipelineContext, Task> _singletonPipeline = default!;
    private Func<PipelineContext, Task> _scopedPipeline = default!;
    private PipelineContext _ctx = default!;

    //[Params(1, 10, 100)]
    //public int MiddlewareWork { get; set; } // ダミー負荷 (BusinessLogic 内で使用してもいいが簡素化)

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection()
            .AddLogging(_ => { }) // ログ出力カットでオーバーヘッド最小化
            .AddSingleton<ExceptionHandlingMiddleware>()
            .AddSingleton<LoggingMiddleware>()
            .AddSingleton<ValidationMiddleware>()
            .AddSingleton<ShortCircuitMiddleware>()
            .AddSingleton<BusinessLogicMiddleware>()
            .BuildServiceProvider();

        _singletonPipeline = PipelineBuilderFactory
            .Create(services, PipelineLifetimeMode.SingletonPreResolved)
            .UseMiddleware<ExceptionHandlingMiddleware>()
            .UseMiddleware<ValidationMiddleware>()   // ログをベンチには含めたくなければ外しても可
            .UseMiddleware<ShortCircuitMiddleware>()
            //.Use(async (ctx, next) =>
            //{
            //    // 擬似的な負荷
            //    int sum = 0;
            //    for (int i = 0; i < MiddlewareWork; i++)
            //        sum += i;
            //    ctx.Items["sum"] = sum;
            //    await next(ctx);
            //})
            .UseMiddleware<BusinessLogicMiddleware>()
            .Run(_ => Task.CompletedTask)
            .Build();

        _scopedPipeline = PipelineBuilderFactory
            .Create(services, PipelineLifetimeMode.ScopedPerExecution)
            .UseMiddleware<ExceptionHandlingMiddleware>()
            .UseMiddleware<ValidationMiddleware>()
            .UseMiddleware<ShortCircuitMiddleware>()
            //.Use(async (ctx, next) =>
            //{
            //    int sum = 0;
            //    for (int i = 0; i < MiddlewareWork; i++)
            //        sum += i;
            //    ctx.Items["sum"] = sum;
            //    await next(ctx);
            //})
            .UseMiddleware<BusinessLogicMiddleware>()
            .Run(_ => Task.CompletedTask)
            .Build();

        _ctx = new PipelineContext { Input = "hello" };
    }

    [IterationSetup]
    public void IterSetup()
    {
        // 毎イテレーションでコンテキスト再作成（副作用を除外）
        _ctx = new PipelineContext { Input = "hello" };
    }

    [Benchmark(Baseline = true)]
    public Task SingletonPipeline() => _singletonPipeline(_ctx);

    [Benchmark]
    public Task ScopedPipeline() => _scopedPipeline(_ctx);
}

//--------------------------------------------------------------------------------

public sealed class PipelineContext
{
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    // デモ用途の簡易 I/O
    public string? Input { get; set; }
    public string? Output { get; set; }

    public bool IsTerminated { get; set; }

    // Scoped モード時: 実行スコープの ServiceProvider
    // Singleton モード時: ルートプロバイダ (任意)
    public IServiceProvider? Services { get; internal set; }
}

public enum PipelineLifetimeMode
{
    // 実行ごとに DI スコープを作成し、各ミドルウェアをスコープから解決
    ScopedPerExecution = 0,
    // Build() 時にミドルウェア(シングルトン)を一括解決し、実行時は delegate のみ
    SingletonPreResolved = 1
}

public delegate Task MiddlewareDelegate(PipelineContext context);

// どちらのモードでも利用する共通インターフェイス
public interface IMiddleware
{
    Task InvokeAsync(PipelineContext context, MiddlewareDelegate next);
}

//--------------------------------------------------------------------------------

public interface IPipelineBuilder
{
    IPipelineBuilder Use(Func<PipelineContext, MiddlewareDelegate, Task> middleware);
    IPipelineBuilder UseMiddleware<TMiddleware>() where TMiddleware : class, IMiddleware;
    IPipelineBuilder UseWhen(Func<PipelineContext, bool> predicate, Action<IPipelineBuilder> configureBranch);
    IPipelineBuilder Map(Func<PipelineContext, bool> predicate, Action<IPipelineBuilder> configureBranch);
    IPipelineBuilder Run(MiddlewareDelegate terminal);
    Func<PipelineContext, Task> Build();
}

// 内部ビルダで使う構造
internal enum EntryKind
{
    Delegate,
    MiddlewareType
}

internal sealed record Entry(
    EntryKind Kind,
    Func<MiddlewareDelegate, MiddlewareDelegate>? Factory,
    Type? MiddlewareType
);

// Build 時にシングルトンミドルウェアを解決し delegate チェーンを固定
internal sealed class SingletonPipelineBuilder : IPipelineBuilder
{
    private readonly IServiceProvider _provider;
    private readonly List<Entry> _entries = new();

    public SingletonPipelineBuilder(IServiceProvider provider) => _provider = provider;

    public IPipelineBuilder Use(Func<PipelineContext, MiddlewareDelegate, Task> middleware)
    {
        _entries.Add(new Entry(
            EntryKind.Delegate,
            next => ctx => middleware(ctx, next),
            null));
        return this;
    }

    public IPipelineBuilder UseMiddleware<TMiddleware>() where TMiddleware : class, IMiddleware
    {
        _entries.Add(new Entry(EntryKind.MiddlewareType, null, typeof(TMiddleware)));
        return this;
    }

    public IPipelineBuilder UseWhen(Func<PipelineContext, bool> predicate, Action<IPipelineBuilder> configureBranch)
    {
        _entries.Add(new Entry(
            EntryKind.Delegate,
            next =>
            {
                var branchBuilder = new SingletonPipelineBuilder(_provider);
                configureBranch(branchBuilder);
                var branch = branchBuilder.BuildDelegate(next);
                return async ctx =>
                {
                    if (predicate(ctx))
                        await branch(ctx);
                    else
                        await next(ctx);
                };
            },
            null));
        return this;
    }

    public IPipelineBuilder Map(Func<PipelineContext, bool> predicate, Action<IPipelineBuilder> configureBranch)
    {
        _entries.Add(new Entry(
            EntryKind.Delegate,
            next =>
            {
                var branchBuilder = new SingletonPipelineBuilder(_provider);
                configureBranch(branchBuilder);
                var branch = branchBuilder.BuildDelegate(_ => Task.CompletedTask);
                return async ctx =>
                {
                    if (predicate(ctx))
                        await branch(ctx);
                    else
                        await next(ctx);
                };
            },
            null));
        return this;
    }

    public IPipelineBuilder Run(MiddlewareDelegate terminal)
    {
        _entries.Add(new Entry(EntryKind.Delegate, _ => terminal, null));
        return this;
    }

    public Func<PipelineContext, Task> Build()
    {
        // 一括解決（シングルトン前提）
        var resolved = _entries
            .Where(e => e.Kind == EntryKind.MiddlewareType)
            .Select(e => (e.MiddlewareType!, (IMiddleware)_provider.GetRequiredService(e.MiddlewareType!)))
            .ToDictionary(t => t.Item1, t => t.Item2);

        MiddlewareDelegate terminal = _ => Task.CompletedTask;
        var list = _entries;

        MiddlewareDelegate app = terminal;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var entry = list[i];
            if (entry.Kind == EntryKind.Delegate)
            {
                app = entry.Factory!(app);
            }
            else
            {
                var mw = resolved[entry.MiddlewareType!];
                var nextCaptured = app;
                app = ctx => mw.InvokeAsync(ctx, nextCaptured);
            }
        }

        // 実行時は context.Services に root をセット（任意）
        return ctx =>
        {
            if (ctx.Services is null)
                ctx.Services = _provider;
            return app(ctx);
        };
    }

    internal MiddlewareDelegate BuildDelegate(MiddlewareDelegate terminal)
    {
        var resolved = _entries
            .Where(e => e.Kind == EntryKind.MiddlewareType)
            .Select(e => (e.MiddlewareType!, (IMiddleware)_provider.GetRequiredService(e.MiddlewareType!)))
            .ToDictionary(t => t.Item1, t => t.Item2);

        MiddlewareDelegate app = terminal;
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            if (entry.Kind == EntryKind.Delegate)
            {
                app = entry.Factory!(app);
            }
            else
            {
                var mw = resolved[entry.MiddlewareType!];
                var nextCaptured = app;
                app = ctx => mw.InvokeAsync(ctx, nextCaptured);
            }
        }
        return app;
    }
}

// 実行ごとにスコープ作成し、そのスコープからミドルウェアを解決
internal sealed class ScopedPipelineBuilder : IPipelineBuilder
{
    private readonly IServiceProvider _root;
    private readonly List<Entry> _entries = new();

    public ScopedPipelineBuilder(IServiceProvider root) => _root = root;

    public IPipelineBuilder Use(Func<PipelineContext, MiddlewareDelegate, Task> middleware)
    {
        _entries.Add(new Entry(
            EntryKind.Delegate,
            next => ctx => middleware(ctx, next),
            null));
        return this;
    }

    public IPipelineBuilder UseMiddleware<TMiddleware>() where TMiddleware : class, IMiddleware
    {
        _entries.Add(new Entry(EntryKind.MiddlewareType, null, typeof(TMiddleware)));
        return this;
    }

    public IPipelineBuilder UseWhen(Func<PipelineContext, bool> predicate, Action<IPipelineBuilder> configureBranch)
    {
        _entries.Add(new Entry(
            EntryKind.Delegate,
            next =>
            {
                var branchBuilder = new ScopedPipelineBuilder(_root);
                configureBranch(branchBuilder);
                var branch = branchBuilder.BuildDelegate(next);
                return async ctx =>
                {
                    if (predicate(ctx))
                        await branch(ctx);
                    else
                        await next(ctx);
                };
            },
            null));
        return this;
    }

    public IPipelineBuilder Map(Func<PipelineContext, bool> predicate, Action<IPipelineBuilder> configureBranch)
    {
        _entries.Add(new Entry(
            EntryKind.Delegate,
            next =>
            {
                var branchBuilder = new ScopedPipelineBuilder(_root);
                configureBranch(branchBuilder);
                var branch = branchBuilder.BuildDelegate(_ => Task.CompletedTask);
                return async ctx =>
                {
                    if (predicate(ctx))
                        await branch(ctx);
                    else
                        await next(ctx);
                };
            },
            null));
        return this;
    }

    public IPipelineBuilder Run(MiddlewareDelegate terminal)
    {
        _entries.Add(new Entry(EntryKind.Delegate, _ => terminal, null));
        return this;
    }

    public Func<PipelineContext, Task> Build()
    {
        // チェーン構築（ミドルウェア型は実行時に解決）
        MiddlewareDelegate terminal = _ => Task.CompletedTask;
        MiddlewareDelegate app = terminal;
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            if (entry.Kind == EntryKind.Delegate)
            {
                app = entry.Factory!(app);
            }
            else
            {
                var type = entry.MiddlewareType!;
                var nextCaptured = app;
                app = async ctx =>
                {
                    var mw = ctx.Services!.GetRequiredService(type) as IMiddleware
                             ?? throw new InvalidOperationException($"Middleware {type.Name} not found.");
                    await mw.InvokeAsync(ctx, nextCaptured);
                };
            }
        }

        return async ctx =>
        {
            using var scope = _root.CreateScope();
            ctx.Services = scope.ServiceProvider;
            await app(ctx);
        };
    }

    internal MiddlewareDelegate BuildDelegate(MiddlewareDelegate terminal)
    {
        MiddlewareDelegate app = terminal;
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            if (entry.Kind == EntryKind.Delegate)
            {
                app = entry.Factory!(app);
            }
            else
            {
                var type = entry.MiddlewareType!;
                var nextCaptured = app;
                app = async ctx =>
                {
                    var mw = ctx.Services!.GetRequiredService(type) as IMiddleware
                             ?? throw new InvalidOperationException($"Middleware {type.Name} not found.");
                    await mw.InvokeAsync(ctx, nextCaptured);
                };
            }
        }
        return app;
    }
}

public static class PipelineBuilderFactory
{
    public static IPipelineBuilder Create(IServiceProvider provider, PipelineLifetimeMode mode) =>
        mode switch
        {
            PipelineLifetimeMode.ScopedPerExecution => new ScopedPipelineBuilder(provider),
            PipelineLifetimeMode.SingletonPreResolved => new SingletonPipelineBuilder(provider),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
}

//--------------------------------------------------------------------------------
public sealed class ExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger) => _logger = logger;

    public async Task InvokeAsync(PipelineContext context, MiddlewareDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Output = "Error: " + ex.Message;
            context.IsTerminated = true;
        }
    }
}

public sealed class LoggingMiddleware : IMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;
    public LoggingMiddleware(ILogger<LoggingMiddleware> logger) => _logger = logger;

    public async Task InvokeAsync(PipelineContext context, MiddlewareDelegate next)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Start Input={Input}", context.Input);
        await next(context);
        sw.Stop();
        _logger.LogInformation("End Elapsed={Elapsed}ms Output={Output}", sw.ElapsedMilliseconds, context.Output);
    }
}

public sealed class ValidationMiddleware : IMiddleware
{
    private readonly ILogger<ValidationMiddleware> _logger;

    public ValidationMiddleware(ILogger<ValidationMiddleware> logger) => _logger = logger;

    public Task InvokeAsync(PipelineContext context, MiddlewareDelegate next)
    {
        if (string.IsNullOrWhiteSpace(context.Input))
        {
            _logger.LogWarning("Invalid input -> short circuit");
            context.Output = "Invalid Input";
            context.IsTerminated = true;
            return Task.CompletedTask;
        }
        return next(context);
    }
}

public sealed class ShortCircuitMiddleware : IMiddleware
{
    private readonly ILogger<ShortCircuitMiddleware> _logger;

    public ShortCircuitMiddleware(ILogger<ShortCircuitMiddleware> logger) => _logger = logger;

    public Task InvokeAsync(PipelineContext context, MiddlewareDelegate next)
    {
        if (context.IsTerminated)
        {
            _logger.LogDebug("Terminated -> stop");
            return Task.CompletedTask;
        }
        return next(context);
    }
}

public sealed class BusinessLogicMiddleware : IMiddleware
{
    private readonly ILogger<BusinessLogicMiddleware> _logger;

    public BusinessLogicMiddleware(ILogger<BusinessLogicMiddleware> logger) => _logger = logger;

    public async Task InvokeAsync(PipelineContext context, MiddlewareDelegate next)
    {
        _logger.LogInformation("Business processing...");
        if (!context.IsTerminated)
        {
            context.Output = $"Processed: {context.Input}";
        }
        await next(context);
    }
}
