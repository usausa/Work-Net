namespace WorkCliHost.Core;

/// <summary>
/// Base interface for all command filters.
/// </summary>
public interface ICommandFilter
{
    /// <summary>
    /// Gets the order in which filters are executed. Lower values execute first.
    /// </summary>
    int Order { get; }
}

/// <summary>
/// Delegate representing the next filter or command execution in the pipeline.
/// </summary>
public delegate ValueTask CommandExecutionDelegate();

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
