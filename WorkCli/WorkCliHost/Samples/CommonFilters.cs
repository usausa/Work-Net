using Microsoft.Extensions.Logging;
using WorkCliHost.Core;

namespace WorkCliHost.Samples;

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
            await next(context);
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
        
        await next(context);
        
        stopwatch.Stop();
        context.Items["ExecutionTime"] = stopwatch.Elapsed;
        Console.WriteLine($"⏱  Command executed in {stopwatch.ElapsedMilliseconds}ms");
    }
}

/// <summary>
/// Example: Exception handling filter that catches and logs exceptions.
/// Demonstrates how to handle exceptions in ICommandExecutionFilter.
/// </summary>
public sealed class ExceptionHandlingFilter : ICommandExecutionFilter
{
    private readonly ILogger<ExceptionHandlingFilter> _logger;

    public ExceptionHandlingFilter(ILogger<ExceptionHandlingFilter> logger)
    {
        _logger = logger;
    }

    public int Order => int.MaxValue; // Execute last to catch all exceptions

    public async ValueTask ExecuteAsync(CommandContext context, CommandExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
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
            
            Console.Error.WriteLine($"❌ Error: {exception.Message}");
        }
    }
}
