namespace WorkCliHost;

/// <summary>
/// Execution context that is passed through the filter pipeline and to the command execution.
/// Similar to HttpContext in ASP.NET Core.
/// </summary>
public sealed class CommandContext
{
    /// <summary>
    /// Gets or sets a key/value collection that can be used to share data within the scope of this command execution.
    /// </summary>
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();

    /// <summary>
    /// Gets the command instance being executed.
    /// </summary>
    public ICommandDefinition Command { get; internal set; } = default!;

    /// <summary>
    /// Gets the command type.
    /// </summary>
    public Type CommandType { get; internal set; } = default!;

    /// <summary>
    /// Gets the cancellation token for this command execution.
    /// </summary>
    public CancellationToken CancellationToken { get; internal set; }

    /// <summary>
    /// Gets or sets the exit code for the command execution.
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the command execution should be short-circuited.
    /// </summary>
    public bool IsShortCircuited { get; set; }
}
