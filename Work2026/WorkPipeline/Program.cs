namespace WorkPipeline;

using System.Diagnostics;

internal static class Program
{
    public static async Task Main()
    {
        Debug.WriteLine("=== Example 1: Basic Pipeline ===\n");
        await Example1_BasicPipeline();

        Debug.WriteLine("\n\n=== Example 2: Multiple Filters ===\n");
        await Example2_MultipleFilters();

        Debug.WriteLine("\n\n=== Example 3: Exception Handling ===\n");
        await Example3_ExceptionHandling();

        Debug.WriteLine("\n\n=== Example 4: Pipeline Builder ===\n");
        await Example4_PipelineBuilder();

        Debug.WriteLine("\n\n=== Example 5: Inline Filters ===\n");
        await Example5_InlineFilters();
    }


    static async Task Example1_BasicPipeline()
    {
        var pipeline = new CommandPipeline();
        pipeline.UseFilter<LoggingFilter>();

        var command = new SampleCommand();
        await pipeline.ExecuteAsync(command);
    }

    static async Task Example2_MultipleFilters()
    {
        var pipeline = new CommandPipeline();
        pipeline
            .UseFilter<ExceptionHandlingFilter>()
            .UseFilter<LoggingFilter>()
            .UseFilter<ValidationFilter>();

        var command = new SampleCommand();
        var context = new CommandContext();

        await pipeline.ExecuteAsync(command, context);
        Debug.WriteLine($"\nCommand executed: {context.IsExecuted}");
    }

    static async Task Example3_ExceptionHandling()
    {
        var pipeline = new CommandPipeline();
        pipeline
            .UseFilter<ExceptionHandlingFilter>()
            .UseFilter<LoggingFilter>();

        var command = new FailingCommand();
        var context = new CommandContext();

        await pipeline.ExecuteAsync(command, context);

        if (context.Exception != null)
        {
            Debug.WriteLine($"\nException was handled: {context.Exception.Message}");
        }
    }

    static async Task Example4_PipelineBuilder()
    {
        var builder = new CommandPipelineBuilder();
        builder
            .UseFilter<LoggingFilter>()
            .UseFilter<ValidationFilter>();

        var command = new SampleCommand();
        var pipeline = builder.Build(command);

        var context = new CommandContext();
        await pipeline(context);
    }

    static async Task Example5_InlineFilters()
    {
        var pipeline = new CommandPipeline();

        // インラインでフィルターを定義
        pipeline.Use(async (context, next) =>
        {
            Debug.WriteLine("[Inline Filter 1] Before");
            await next(context);
            Debug.WriteLine("[Inline Filter 1] After");
        });

        pipeline.Use(async (context, next) =>
        {
            Debug.WriteLine("[Inline Filter 2] Before");
            context.Items["timestamp"] = DateTime.UtcNow;
            await next(context);
            Debug.WriteLine($"[Inline Filter 2] After (Timestamp: {context.Items["timestamp"]})");
        });

        var command = new SampleCommand();
        await pipeline.ExecuteAsync(command);
    }
}

//--------------------------------------------------------------------------------
// Sample
//--------------------------------------------------------------------------------

// Sample command
public class SampleCommand : ICommand
{
    public async ValueTask ExecuteAsync(CommandContext context)
    {
        Debug.WriteLine(">>> [SampleCommand] Executing command logic");
        await Task.Delay(100); // 非同期処理のシミュレーション
        context.IsExecuted = true;
        Debug.WriteLine(">>> [SampleCommand] Command executed successfully");
    }
}

// Error command
public class FailingCommand : ICommand
{
    public ValueTask ExecuteAsync(CommandContext context)
    {
        Debug.WriteLine(">>> [FailingCommand] About to throw exception");
        throw new InvalidOperationException("Command execution failed!");
    }
}

// Logging
public class LoggingFilter : ICommandExecutionFilter
{
    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        Debug.WriteLine("[LoggingFilter] Before execution");
        var startTime = DateTime.UtcNow;

        try
        {
            await next(context);
        }
        finally
        {
            var duration = DateTime.UtcNow - startTime;
            Debug.WriteLine($"[LoggingFilter] After execution (Duration: {duration.TotalMilliseconds}ms)");
        }
    }
}

// Exception Handling
public class ExceptionHandlingFilter : ICommandExecutionFilter
{
    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        try
        {
            Debug.WriteLine("[ExceptionHandlingFilter] Before execution");
            await next(context);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ExceptionHandlingFilter] Exception caught: {ex.Message}");
            context.Exception = ex;
        }
    }
}

public class ValidationFilter : ICommandExecutionFilter
{
    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        Debug.WriteLine("[ValidationFilter] Validating...");

        // Validation
        if (context.Items.ContainsKey("invalid"))
        {
            throw new InvalidOperationException("Validation failed");
        }

        await next(context);
        Debug.WriteLine("[ValidationFilter] Validation passed");
    }
}

//--------------------------------------------------------------------------------
// Framework
//--------------------------------------------------------------------------------

// Context
public sealed class CommandContext
{
    public Dictionary<string, object> Items { get; } = new();

    public bool IsExecuted { get; set; }

    public Exception? Exception { get; set; }
}

// Delegate
public delegate ValueTask CommandExecutionDelegate(CommandContext context);

// Command
public interface ICommand
{
    ValueTask ExecuteAsync(CommandContext context);
}

// Filter
public interface ICommandExecutionFilter
{
    ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next);
}

// Pipeline
public class CommandPipeline
{
    private readonly List<Func<CommandExecutionDelegate, CommandExecutionDelegate>> filters = new();

    // By func
    public CommandPipeline Use(Func<CommandContext, CommandExecutionDelegate, ValueTask> filter)
    {
        filters.Add(next =>
        {
            return context => filter(context, next);
        });
        return this;
    }

    // By instance
    public CommandPipeline UseFilter(ICommandExecutionFilter filter)
    {
        return Use(filter.ExecuteAsync);
    }

    // By type
    public CommandPipeline UseFilter<TFilter>() where TFilter : ICommandExecutionFilter, new()
    {
        var filter = new TFilter();
        return UseFilter(filter);
    }

    // ** Execute Core **
    public async ValueTask ExecuteAsync(ICommand command, CommandContext? context = null)
    {
        context ??= new CommandContext();

        CommandExecutionDelegate pipeline = command.ExecuteAsync;

        // Reverse apply filters
        for (var i = filters.Count - 1; i >= 0; i--)
        {
            pipeline = filters[i](pipeline);
        }

        // Execute
        await pipeline(context);
    }
}

// Builder
public class CommandPipelineBuilder
{
    private readonly List<Func<CommandExecutionDelegate, CommandExecutionDelegate>> components = new();

    public CommandPipelineBuilder Use(Func<CommandContext, CommandExecutionDelegate, ValueTask> middleware)
    {
        components.Add(next =>
        {
            return context => middleware(context, next);
        });
        return this;
    }

    public CommandPipelineBuilder UseFilter(ICommandExecutionFilter filter)
    {
        return Use(filter.ExecuteAsync);
    }

    public CommandPipelineBuilder UseFilter<TFilter>() where TFilter : ICommandExecutionFilter, new()
    {
        var filter = new TFilter();
        return UseFilter(filter);
    }

    public CommandExecutionDelegate Build(ICommand command)
    {
        CommandExecutionDelegate pipeline = command.ExecuteAsync;

        for (var i = components.Count - 1; i >= 0; i--)
        {
            pipeline = components[i](pipeline);
        }

        return pipeline;
    }
}

