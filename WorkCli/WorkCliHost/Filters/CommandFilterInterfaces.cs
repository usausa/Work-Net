using Microsoft.Extensions.Logging;

namespace WorkCliHost.Filters;

/// <summary>
/// Filter that runs around command execution.
/// Similar to IAsyncActionFilter in ASP.NET Core.
/// </summary>
public interface ICommandExecutionFilter : ICommandFilter
{
    /// <summary>
    /// Called before and after command execution.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="next">The delegate representing the next filter or the command execution.</param>
    ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next);
}

/// <summary>
/// Delegate representing the next filter or command execution in the pipeline.
/// </summary>
public delegate ValueTask CommandExecutionDelegate();

/// <summary>
/// Filter that runs before command execution.
/// </summary>
public interface IBeforeCommandFilter : ICommandFilter
{
    /// <summary>
    /// Called before command execution.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    ValueTask OnBeforeExecutionAsync(CommandContext context);
}

/// <summary>
/// Filter that runs after command execution.
/// </summary>
public interface IAfterCommandFilter : ICommandFilter
{
    /// <summary>
    /// Called after command execution.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    ValueTask OnAfterExecutionAsync(CommandContext context);
}

/// <summary>
/// Filter that can handle exceptions during command execution.
/// </summary>
public interface IExceptionFilter : ICommandFilter
{
    /// <summary>
    /// Called when an exception occurs during command execution.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="exception">The exception that occurred.</param>
    ValueTask OnExceptionAsync(CommandContext context, Exception exception);
}

// ============================================================================
// Filter Attributes
// ============================================================================

/// <summary>
/// Base attribute for applying filters to commands.
/// Similar to FilterAttribute in ASP.NET Core.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public abstract class CommandFilterAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the order in which filters are executed. Lower values execute first.
    /// Default is 0. If not specified, filters are executed in the order they are defined in the inheritance hierarchy.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets the filter type.
    /// </summary>
    public abstract Type FilterType { get; }
}

/// <summary>
/// Attribute for applying a specific filter type to a command.
/// </summary>
/// <typeparam name="TFilter">The filter type.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class CommandFilterAttribute<TFilter> : CommandFilterAttribute
    where TFilter : ICommandFilter
{
    public override Type FilterType => typeof(TFilter);
}

// ============================================================================
// Example Filter Implementations
// ============================================================================

/// <summary>
/// Example: Logging filter that logs before and after command execution.
/// </summary>
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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Executing command: {CommandType}", context.CommandType.Name);

        try
        {
            await next();
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation("Command {CommandType} completed in {ElapsedMilliseconds}ms with exit code {ExitCode}",
                context.CommandType.Name,
                stopwatch.ElapsedMilliseconds,
                context.ExitCode);
        }
    }
}

/// <summary>
/// Example: Timing filter that measures command execution time.
/// </summary>
public sealed class TimingFilter : ICommandExecutionFilter
{
    public int Order => -100; // Execute before other filters

    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await next();

        stopwatch.Stop();
        context.Items["ExecutionTime"] = stopwatch.Elapsed;
        Console.WriteLine($"Command executed in {stopwatch.ElapsedMilliseconds}ms");
    }
}

/// <summary>
/// Example: Authorization filter that checks user permissions.
/// </summary>
public sealed class AuthorizationFilter : IBeforeCommandFilter
{
    private readonly ILogger<AuthorizationFilter> _logger;

    public AuthorizationFilter(ILogger<AuthorizationFilter> logger)
    {
        _logger = logger;
    }

    public int Order => -1000; // Execute very early

    public ValueTask OnBeforeExecutionAsync(CommandContext context)
    {
        // Example: Check if user has required permissions
        _logger.LogInformation("Checking authorization for command: {CommandType}", context.CommandType.Name);

        // In real implementation, you would check actual permissions here
        var isAuthorized = true; // context.Items["CurrentUser"] is authorized

        if (!isAuthorized)
        {
            _logger.LogWarning("User is not authorized to execute {CommandType}", context.CommandType.Name);
            context.IsShortCircuited = true;
            context.ExitCode = 403; // Forbidden
        }

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Example: Validation filter that validates command arguments before execution.
/// </summary>
public sealed class ValidationFilter : IBeforeCommandFilter
{
    private readonly ILogger<ValidationFilter> _logger;

    public ValidationFilter(ILogger<ValidationFilter> logger)
    {
        _logger = logger;
    }

    public int Order => -500; // Execute after authorization but before other filters

    public ValueTask OnBeforeExecutionAsync(CommandContext context)
    {
        _logger.LogInformation("Validating command arguments for: {CommandType}", context.CommandType.Name);

        // Example: Validate command properties
        // In real implementation, you would use FluentValidation or similar
        var isValid = true;

        if (!isValid)
        {
            _logger.LogError("Validation failed for {CommandType}", context.CommandType.Name);
            context.IsShortCircuited = true;
            context.ExitCode = 400; // Bad Request
        }

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Example: Exception handling filter that catches and logs exceptions.
/// </summary>
public sealed class ExceptionHandlingFilter : IExceptionFilter
{
    private readonly ILogger<ExceptionHandlingFilter> _logger;

    public ExceptionHandlingFilter(ILogger<ExceptionHandlingFilter> logger)
    {
        _logger = logger;
    }

    public int Order => int.MaxValue; // Execute last to catch all exceptions

    public ValueTask OnExceptionAsync(CommandContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception in command {CommandType}: {Message}",
            context.CommandType.Name,
            exception.Message);

        // Set exit code based on exception type
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

/// <summary>
/// Example: Cleanup filter that runs after command execution.
/// </summary>
public sealed class CleanupFilter : IAfterCommandFilter
{
    private readonly ILogger<CleanupFilter> _logger;

    public CleanupFilter(ILogger<CleanupFilter> logger)
    {
        _logger = logger;
    }

    public int Order => 1000; // Execute late

    public ValueTask OnAfterExecutionAsync(CommandContext context)
    {
        _logger.LogInformation("Cleaning up after command: {CommandType}", context.CommandType.Name);

        // Example: Clean up temporary files, close connections, etc.
        if (context.Items.TryGetValue("TempFiles", out var tempFiles))
        {
            // Delete temp files
        }

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Example: Transaction filter that wraps command execution in a transaction.
/// </summary>
public sealed class TransactionFilter : ICommandExecutionFilter
{
    private readonly ILogger<TransactionFilter> _logger;

    public TransactionFilter(ILogger<TransactionFilter> logger)
    {
        _logger = logger;
    }

    public int Order => -200;

    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        _logger.LogInformation("Starting transaction for command: {CommandType}", context.CommandType.Name);

        // Example: Begin transaction
        // var transaction = await _dbContext.Database.BeginTransactionAsync();
        // context.Items["Transaction"] = transaction;

        try
        {
            await next();

            // Commit transaction if successful
            _logger.LogInformation("Committing transaction for command: {CommandType}", context.CommandType.Name);
            // await transaction.CommitAsync();
        }
        catch
        {
            // Rollback on exception
            _logger.LogWarning("Rolling back transaction for command: {CommandType}", context.CommandType.Name);
            // await transaction.RollbackAsync();
            throw;
        }
    }
}
