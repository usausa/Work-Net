using Microsoft.Extensions.Options;

namespace WorkCliHost.Core;

/// <summary>
/// Executes the filter pipeline for command execution.
/// </summary>
internal sealed class FilterPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CommandFilterOptions _options;

    public FilterPipeline(IServiceProvider serviceProvider, IOptions<CommandFilterOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    public async ValueTask<int> ExecuteAsync(
        Type commandType,
        ICommandDefinition commandInstance,
        CancellationToken cancellationToken = default)
    {
        var context = new CommandContext
        {
            Command = commandInstance,
            CommandType = commandType,
            CancellationToken = cancellationToken,
            ExitCode = 0
        };

        var filters = CollectFilters(commandType);
        
        try
        {
            await ExecutePipelineAsync(context, filters, commandInstance);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, filters, ex);
        }

        return context.ExitCode;
    }

    private List<FilterDescriptor> CollectFilters(Type commandType)
    {
        var filters = new List<FilterDescriptor>();

        // グローバルフィルタを追加
        foreach (var globalFilter in _options.GlobalFilters)
        {
            filters.Add(new FilterDescriptor(globalFilter.FilterType, globalFilter.Order, isGlobal: true));
        }

        // コマンドクラスとその基底クラスから属性フィルタを収集
        if (_options.IncludeBaseClassFilters)
        {
            var typeHierarchy = new List<Type>();
            var currentType = commandType;

            while (currentType != null && currentType != typeof(object))
            {
                typeHierarchy.Add(currentType);
                currentType = currentType.BaseType;
            }

            // 基底→派生の順に反転
            typeHierarchy.Reverse();

            foreach (var type in typeHierarchy)
            {
                var attributes = type.GetCustomAttributes(typeof(CommandFilterAttribute), inherit: false)
                    .Cast<CommandFilterAttribute>();

                foreach (var attr in attributes)
                {
                    filters.Add(new FilterDescriptor(attr.FilterType, attr.Order, isGlobal: false));
                }
            }
        }
        else
        {
            var attributes = commandType.GetCustomAttributes(typeof(CommandFilterAttribute), inherit: true)
                .Cast<CommandFilterAttribute>();

            foreach (var attr in attributes)
            {
                filters.Add(new FilterDescriptor(attr.FilterType, attr.Order, isGlobal: false));
            }
        }

        // Order順にソート
        filters.Sort((a, b) => a.Order.CompareTo(b.Order));

        return filters;
    }

    private async ValueTask ExecutePipelineAsync(
        CommandContext context,
        List<FilterDescriptor> filters,
        ICommandDefinition commandInstance)
    {
        var executionFilters = new List<ICommandExecutionFilter>();
        var beforeFilters = new List<IBeforeCommandFilter>();
        var afterFilters = new List<IAfterCommandFilter>();

        // フィルタインスタンスを作成して分類
        foreach (var descriptor in filters)
        {
            var filterInstance = _serviceProvider.GetService(descriptor.FilterType);
            if (filterInstance == null)
            {
                throw new InvalidOperationException($"Filter {descriptor.FilterType.Name} is not registered in DI container.");
            }

            if (filterInstance is ICommandExecutionFilter execFilter)
            {
                executionFilters.Add(execFilter);
            }
            if (filterInstance is IBeforeCommandFilter beforeFilter)
            {
                beforeFilters.Add(beforeFilter);
            }
            if (filterInstance is IAfterCommandFilter afterFilter)
            {
                afterFilters.Add(afterFilter);
            }
        }

        // パイプラインを構築
        CommandExecutionDelegate pipeline = async () =>
        {
            // Before filters
            foreach (var filter in beforeFilters)
            {
                if (context.IsShortCircuited)
                    break;

                await filter.OnBeforeExecutionAsync(context);
            }

            // Command execution
            if (!context.IsShortCircuited)
            {
                await commandInstance.ExecuteAsync(context);
            }

            // After filters (逆順)
            for (int i = afterFilters.Count - 1; i >= 0; i--)
            {
                await afterFilters[i].OnAfterExecutionAsync(context);
            }
        };

        // Execution filtersでラップ（逆順でラップして正順で実行）
        for (int i = executionFilters.Count - 1; i >= 0; i--)
        {
            var filter = executionFilters[i];
            var next = pipeline;
            pipeline = () => filter.ExecuteAsync(context, next);
        }

        // パイプライン実行
        await pipeline();
    }

    private async ValueTask HandleExceptionAsync(
        CommandContext context,
        List<FilterDescriptor> filters,
        Exception exception)
    {
        var exceptionFilters = new List<IExceptionFilter>();

        // Exception filtersを収集
        foreach (var descriptor in filters)
        {
            var filterInstance = _serviceProvider.GetService(descriptor.FilterType);
            if (filterInstance is IExceptionFilter exFilter)
            {
                exceptionFilters.Add(exFilter);
            }
        }

        // Exception filtersを実行（Order降順）
        exceptionFilters.Sort((a, b) => b.Order.CompareTo(a.Order));

        foreach (var filter in exceptionFilters)
        {
            await filter.OnExceptionAsync(context, exception);
        }

        // 例外フィルタで処理されなかった場合（ExitCodeが0のまま）は再スロー
        if (context.ExitCode == 0)
        {
            // ExitCodeを設定してから再スロー
            context.ExitCode = 1;
        }
    }

    private sealed class FilterDescriptor
    {
        public Type FilterType { get; }
        public int Order { get; }
        public bool IsGlobal { get; }

        public FilterDescriptor(Type filterType, int order, bool isGlobal)
        {
            FilterType = filterType;
            Order = order;
            IsGlobal = isGlobal;
        }
    }
}
