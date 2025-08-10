namespace WorkMiddleware;

using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;
using Microsoft.Extensions.Logging;

internal class Program
{
    static async Task Main(string[] args)
    {
        // 1. DI 構築
        var services = new ServiceCollection()
            .AddLogging(b =>
            {
                b.ClearProviders();
                b.AddSimpleConsole(o =>
                {
                    o.TimestampFormat = "HH:mm:ss ";
                    o.SingleLine = true;
                });
                b.SetMinimumLevel(LogLevel.Debug);
            })
            // Middleware を DI に登録
            .AddTransient<ExceptionHandlingMiddleware>()
            .AddTransient<LoggingMiddleware>()
            .AddTransient<TimingMiddleware>()
            .AddTransient<ValidationMiddleware>()
            .AddTransient<ShortCircuitMiddleware>()
            .AddTransient<BusinessLogicMiddleware>()
            .BuildServiceProvider();

        // 2. パイプラインを組み立て
        var builder = services.CreatePipelineBuilder();

        builder
            .UseMiddleware<ExceptionHandlingMiddleware>()
            .UseMiddleware<LoggingMiddleware>()
            .UseMiddleware<TimingMiddleware>()
            // 条件付き (Input が "skip" の場合はスキップ)
            .UseWhen(ctx => ctx.Input == "skip", branch =>
            {
                branch.Use(async (ctx, next) =>
                {
                    ctx.Output = "Skipped main flow.";
                    ctx.IsTerminated = true;
                    await next(ctx);
                });
            })
            .UseMiddleware<ValidationMiddleware>()
            .UseMiddleware<ShortCircuitMiddleware>()
            // 分岐 (Input が "alt" の場合は別ルートへ)
            .Map(ctx => ctx.Input == "alt", branch =>
            {
                branch.Use(async (ctx, next) =>
                {
                    ctx.Output = "Alternative branch result.";
                    await next(ctx);
                });
            })
            .UseMiddleware<BusinessLogicMiddleware>()
            .Run(ctx =>
            {
                // 終端 (何もしない例)
                return Task.CompletedTask;
            });

        var pipeline = builder.Build();

        // 3. 実行例
        await Execute(pipeline, "hello");
        await Execute(pipeline, "");
        await Execute(pipeline, "skip");
        await Execute(pipeline, "alt");

        Console.WriteLine("Done.");
    }

    static async Task Execute(Func<PipelineContext, Task> pipeline, string input)
    {
        Console.WriteLine();
        Console.WriteLine("=== Execute: Input = " + (input == "" ? "(empty)" : input) + " ===");
        var context = new PipelineContext
        {
            Input = input
        };
        await pipeline(context);
        Console.WriteLine($"Result Output={context.Output}");
    }

}

public sealed class PipelineContext
{
    // 任意の共有データ
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    // 実行スコープ (1 実行毎に作成)
    public IServiceProvider RequestServices { get; internal set; } = default!;

    // キャンセル
    public CancellationToken CancellationToken { get; init; }

    // シナリオ例: 入力/出力
    public string? Input { get; set; }
    public string? Output { get; set; }

    // 途中で終了させたい場合のフラグ（サンプル）
    public bool IsTerminated { get; set; }
}

public delegate Task MiddlewareDelegate(PipelineContext context);

public interface IMiddleware
{
    Task InvokeAsync(PipelineContext context, MiddlewareDelegate next);
}

public sealed class MiddlewareBuilder
{
    private readonly IList<Func<MiddlewareDelegate, MiddlewareDelegate>> _components =
        new List<Func<MiddlewareDelegate, MiddlewareDelegate>>();

    private readonly IServiceProvider _rootProvider;

    public MiddlewareBuilder(IServiceProvider rootProvider)
    {
        _rootProvider = rootProvider;
    }

    // 標準的な Use (func ベース)
    public MiddlewareBuilder Use(Func<PipelineContext, MiddlewareDelegate, Task> middleware)
    {
        _components.Add(next => ctx => middleware(ctx, next));
        return this;
    }

    // IMiddleware を DI から取り出して差し込む
    public MiddlewareBuilder UseMiddleware<TMiddleware>() where TMiddleware : class, IMiddleware
    {
        _components.Add(next => async context =>
        {
            var mw = context.RequestServices.GetRequiredService<TMiddleware>();
            await mw.InvokeAsync(context, next);
        });
        return this;
    }

    // 条件付き (ASP.NET Core の UseWhen 風)
    public MiddlewareBuilder UseWhen(Func<PipelineContext, bool> predicate, Action<MiddlewareBuilder> configuration)
    {
        _components.Add(next =>
        {
            // 分岐用に新しい builder を構築
            var branchBuilder = new MiddlewareBuilder(_rootProvider);
            configuration(branchBuilder);
            var branch = branchBuilder.BuildInternal(next); // 分岐後は main パイプラインに合流

            return async context =>
            {
                if (predicate(context))
                    await branch(context);
                else
                    await next(context);
            };
        });
        return this;
    }

    // Map: 分岐した枝の最後は next へ戻さず終了（ASP.NET Core Map 風）
    public MiddlewareBuilder Map(Func<PipelineContext, bool> predicate, Action<MiddlewareBuilder> configuration)
    {
        _components.Add(next =>
        {
            var branchBuilder = new MiddlewareBuilder(_rootProvider);
            configuration(branchBuilder);
            // Map は最後に何もしない終端 delegate を付加
            branchBuilder.Run(_ => Task.CompletedTask);
            var branch = branchBuilder.BuildInternal(_ => Task.CompletedTask);

            return async context =>
            {
                if (predicate(context))
                    await branch(context);
                else
                    await next(context);
            };
        });
        return this;
    }

    // 終端処理を設定
    public MiddlewareBuilder Run(MiddlewareDelegate terminal)
    {
        _components.Add(_ => terminal);
        return this;
    }

    // 外部公開: 実行関数を組み立て
    public Func<PipelineContext, Task> Build()
    {
        var pipeline = BuildInternal(_ => Task.CompletedTask);

        // 1 回の Execute ごとに DI スコープを切るラッパ
        return async outerContext =>
        {
            using var scope = _rootProvider.CreateScope();
            outerContext.RequestServices = scope.ServiceProvider;
            await pipeline(outerContext);
        };
    }

    private MiddlewareDelegate BuildInternal(MiddlewareDelegate terminal)
    {
        MiddlewareDelegate app = terminal;

        // 逆順に積む
        foreach (var component in _components.Reverse())
        {
            app = component(app);
        }

        return app;
    }
}

public static class MiddlewareBuilderExtensions
{
    public static MiddlewareBuilder CreatePipelineBuilder(this IServiceProvider provider)
        => new(provider);

    public static MiddlewareBuilder2 CreatePipelineBuilder2(this IServiceProvider provider)
        => new(provider);
}

//--------------------------------------------------------------------------------

public sealed class MiddlewareBuilder2
{
    private readonly IList<Func<MiddlewareDelegate, MiddlewareDelegate>> _components =
        new List<Func<MiddlewareDelegate, MiddlewareDelegate>>();
    private readonly IServiceProvider _rootProvider;

    public MiddlewareBuilder2(IServiceProvider rootProvider) => _rootProvider = rootProvider;

    //public MiddlewareBuilder2 UseMiddleware<TMiddleware>() where TMiddleware : class, IMiddleware
    //{
    //    _components.Add(next => async context =>
    //    {
    //        var mw = context.RequestServices.GetRequiredService<TMiddleware>();
    //        await mw.InvokeAsync(context, next);
    //    });
    //    return this;
    //}

    // 追加: 起動時(ビルド時) 1 回だけ解決
    public MiddlewareBuilder2 UseSingletonMiddleware<TMiddleware>() where TMiddleware : class, IMiddleware
    {
        var mw = _rootProvider.GetRequiredService<TMiddleware>(); // root から解決
        _components.Add(next => ctx => mw.InvokeAsync(ctx, next));
        return this;
    }

    public MiddlewareBuilder2 Run(MiddlewareDelegate terminal)
    {
        _components.Add(_ => terminal);
        return this;
    }

    public Func<PipelineContext, Task> Build()
    {
        var pipeline = BuildInternal(_ => Task.CompletedTask);
        return async outerContext =>
        {
            using var scope = _rootProvider.CreateScope();
            outerContext.RequestServices = scope.ServiceProvider;
            await pipeline(outerContext);
        };
    }

    private MiddlewareDelegate BuildInternal(MiddlewareDelegate terminal)
    {
        MiddlewareDelegate app = terminal;
        for (int i = _components.Count - 1; i >= 0; i--)
            app = _components[i](app);
        return app;
    }
}

public sealed class LoggingMiddleware : IMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger) => _logger = logger;

    public async Task InvokeAsync(PipelineContext context, MiddlewareDelegate next)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("LoggingMiddleware: Start Input={Input}", context.Input);

        await next(context);

        sw.Stop();
        _logger.LogInformation("LoggingMiddleware: End (Elapsed={Elapsed} ms) Output={Output}",
            sw.ElapsedMilliseconds, context.Output);
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
            _logger.LogWarning("ValidationMiddleware: Input is null or whitespace. Short-circuit.");
            context.IsTerminated = true;
            context.Output = "Invalid input";
            return Task.CompletedTask;
        }

        return next(context);
    }
}

public sealed class TimingMiddleware : IMiddleware
{
    private readonly ILogger<TimingMiddleware> _logger;

    public TimingMiddleware(ILogger<TimingMiddleware> logger) => _logger = logger;

    public async Task InvokeAsync(PipelineContext context, MiddlewareDelegate next)
    {
        var sw = Stopwatch.StartNew();
        await next(context);
        sw.Stop();
        _logger.LogDebug("TimingMiddleware: Accumulated elapsed = {Elapsed} ms", sw.ElapsedMilliseconds);
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
            _logger.LogInformation("ShortCircuitMiddleware: Terminated flag detected. Halting pipeline.");
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
        // 何らかのビジネス処理
        _logger.LogInformation("BusinessLogicMiddleware: Processing...");
        context.Output = $"Processed: {context.Input}";
        await next(context);
    }
}

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
            _logger.LogError(ex, "Unhandled exception occurred in pipeline.");
            context.Output = "Error: " + ex.Message;
            context.IsTerminated = true;
        }
    }
}

