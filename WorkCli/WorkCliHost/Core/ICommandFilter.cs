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
/// Provides full control over the command execution pipeline with before, after, and exception handling.
/// </summary>
public interface ICommandExecutionFilter : ICommandFilter
{
    /// <summary>
    /// Called to execute the filter logic around the command execution.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="next">The delegate representing the next filter or the command execution.</param>
    /// <remarks>
    /// This filter provides full control over command execution:
    /// - Execute code before the command by placing it before <paramref name="next"/>()
    /// - Execute code after the command by placing it after <paramref name="next"/>()
    /// - Handle exceptions by wrapping <paramref name="next"/>() in try-catch
    /// - Short-circuit execution by not calling <paramref name="next"/>()
    /// </remarks>
    ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next);
}
