using Microsoft.Extensions.Logging;
using WorkCliHost.Core;

namespace WorkCliHost.Samples;

// ============================================================================
// Example Filter Implementations
// ============================================================================

/// <summary>
/// Example: Authorization filter that checks user permissions before command execution.
/// Demonstrates how to implement before-execution logic with short-circuiting.
/// </summary>
public sealed class AuthorizationFilter : ICommandExecutionFilter
{
    private readonly ILogger<AuthorizationFilter> _logger;

    public AuthorizationFilter(ILogger<AuthorizationFilter> logger)
    {
        _logger = logger;
    }

    public int Order => -1000; // Execute very early

    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        // Before: Check authorization
        _logger.LogInformation("Checking authorization for command: {CommandType}", context.CommandType.Name);

        // In real implementation, you would check actual permissions here
        var isAuthorized = true; // context.Items["CurrentUser"] is authorized

        if (!isAuthorized)
        {
            _logger.LogWarning("User is not authorized to execute {CommandType}", context.CommandType.Name);
            context.ExitCode = 403; // Forbidden
            return; // Short-circuit: don't call next()
        }

        await next(); // Proceed to next filter or command
    }
}

/// <summary>
/// Example: Validation filter that validates command arguments before execution.
/// Demonstrates validation logic with short-circuiting.
/// </summary>
public sealed class ValidationFilter : ICommandExecutionFilter
{
    private readonly ILogger<ValidationFilter> _logger;

    public ValidationFilter(ILogger<ValidationFilter> logger)
    {
        _logger = logger;
    }

    public int Order => -500; // Execute after authorization but before other filters

    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        // Before: Validate arguments
        _logger.LogInformation("Validating command arguments for: {CommandType}", context.CommandType.Name);

        // Example: Validate command properties
        // In real implementation, you would use FluentValidation or similar
        var isValid = true;

        if (!isValid)
        {
            _logger.LogError("Validation failed for {CommandType}", context.CommandType.Name);
            context.ExitCode = 400; // Bad Request
            return; // Short-circuit
        }

        await next(); // Proceed if valid
    }
}

/// <summary>
/// Example: Cleanup filter that runs after command execution.
/// Demonstrates after-execution logic.
/// </summary>
public sealed class CleanupFilter : ICommandExecutionFilter
{
    private readonly ILogger<CleanupFilter> _logger;

    public CleanupFilter(ILogger<CleanupFilter> logger)
    {
        _logger = logger;
    }

    public int Order => 1000; // Execute late

    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        await next(); // Execute command first

        // After: Cleanup
        _logger.LogInformation("Cleaning up after command: {CommandType}", context.CommandType.Name);

        // Example: Clean up temporary files, close connections, etc.
        if (context.Items.TryGetValue("TempFiles", out var tempFiles))
        {
            // Delete temp files
        }
    }
}

/// <summary>
/// Example: Transaction filter that wraps command execution in a transaction.
/// Demonstrates wrapping logic with exception handling.
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
