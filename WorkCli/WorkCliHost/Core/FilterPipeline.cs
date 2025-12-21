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
            CancellationToken = cancellationToken
        };

        // フィルタを収集・ソート
        var filters = CollectFilters(commandType);

        // フィルタパイプラインを構築して実行
        await ExecutePipelineAsync(context, filters, commandInstance);
        
        return context.ExitCode;
    }

    /// <summary>
    /// Executes a command through the filter pipeline with a custom action delegate.
    /// </summary>
    /// <param name="commandType">The type of the command being executed.</param>
    /// <param name="commandInstance">The command instance.</param>
    /// <param name="action">The action to execute within the filter pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exit code from the command execution.</returns>
    public async ValueTask<int> ExecuteAsync(
        Type commandType,
        ICommandDefinition commandInstance,
        Func<ValueTask> action,
        CancellationToken cancellationToken)
    {
        var context = new CommandContext
        {
            Command = commandInstance,
            CommandType = commandType,
            CancellationToken = cancellationToken
        };

        // フィルタを収集・ソート
        var filters = CollectFilters(commandType);

        // フィルタパイプラインを構築して実行
        await ExecutePipelineWithActionAsync(context, filters, action);
        
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
        
        // フィルタインスタンスを作成
        foreach (var descriptor in filters)
        {
            var filterInstance = _serviceProvider.GetService(descriptor.FilterType);
            if (filterInstance is ICommandExecutionFilter execFilter)
            {
                executionFilters.Add(execFilter);
            }
        }
        
        // パイプラインの中心: コマンド実行
        CommandExecutionDelegate pipeline = ctx => commandInstance.ExecuteAsync(ctx);
        
        // Execution filtersでラップ（逆順でラップして正順で実行）
        for (int i = executionFilters.Count - 1; i >= 0; i--)
        {
            var filter = executionFilters[i];
            var next = pipeline;
            pipeline = ctx => filter.ExecuteAsync(ctx, next);
        }
        
        // パイプライン実行
        await pipeline(context);
    }

    private async ValueTask ExecutePipelineWithActionAsync(
        CommandContext context,
        List<FilterDescriptor> filters,
        Func<ValueTask> action)
    {
        var executionFilters = new List<ICommandExecutionFilter>();
        
        // フィルタインスタンスを作成
        foreach (var descriptor in filters)
        {
            var filterInstance = _serviceProvider.GetService(descriptor.FilterType);
            if (filterInstance is ICommandExecutionFilter execFilter)
            {
                executionFilters.Add(execFilter);
            }
        }
        
        // パイプラインの中心: カスタムアクション
        CommandExecutionDelegate pipeline = ctx => action();
        
        // Execution filtersでラップ（逆順でラップして正順で実行）
        for (int i = executionFilters.Count - 1; i >= 0; i--)
        {
            var filter = executionFilters[i];
            var next = pipeline;
            pipeline = ctx => filter.ExecuteAsync(ctx, next);
        }
        
        // パイプライン実行
        await pipeline(context);
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
