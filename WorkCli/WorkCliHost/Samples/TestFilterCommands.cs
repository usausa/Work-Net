using Microsoft.Extensions.Logging;
using WorkCliHost.Core;

namespace WorkCliHost.Samples;

// ============================================================================
// フィルタテスト用のコマンド
// ============================================================================

/// <summary>
/// Simple command with filters
/// </summary>
[CommandFilter<TimingFilter>(Order = -100)]
[CommandFilter<LoggingFilter>]
[CliCommand("test-filter", Description = "Test command with filters")]
public sealed class TestFilterCommand : ICommandDefinition
{
    private readonly ILogger<TestFilterCommand> _logger;

    public TestFilterCommand(ILogger<TestFilterCommand> logger)
    {
        _logger = logger;
    }

    [CliArgument<string>("message", Description = "Message to display")]
    public string Message { get; set; } = default!;

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        _logger.LogInformation("Processing message: {Message}", Message);
        
        // Simulate some work
        await Task.Delay(100);
        
        Console.WriteLine($"✅ {Message}");
        
        // Access filter data
        if (context.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            Console.WriteLine($"   Correlation ID: {correlationId}");
        }
    }
}

/// <summary>
/// Command that throws an exception to test exception filter
/// </summary>
[CommandFilter<ExceptionHandlingFilter>(Order = int.MaxValue)]
[CliCommand("test-exception", Description = "Test command that throws an exception")]
public sealed class TestExceptionCommand : ICommandDefinition
{
    [CliArgument<string>("type", Description = "Exception type (argument/file/unauthorized/generic)", DefaultValue = "generic")]
    public string ExceptionType { get; set; } = default!;

    public ValueTask ExecuteAsync(CommandContext context)
    {
        Console.WriteLine($"Throwing {ExceptionType} exception...");
        
        throw ExceptionType.ToLowerInvariant() switch
        {
            "argument" => new ArgumentException("Invalid argument provided"),
            "file" => new FileNotFoundException("File not found: test.txt"),
            "unauthorized" => new UnauthorizedAccessException("Access denied"),
            _ => new InvalidOperationException("Something went wrong")
        };
    }
}
