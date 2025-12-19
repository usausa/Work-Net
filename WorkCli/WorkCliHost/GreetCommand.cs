using Microsoft.Extensions.Logging;

namespace WorkCliHost;

[CliCommand("greet", Description = "Greet someone")]
public sealed class GreetCommand : ICommandDefinition
{
    private readonly ILogger<GreetCommand> _logger;

    public GreetCommand(ILogger<GreetCommand> logger)
    {
        _logger = logger;
    }

    [CliArgument(0, "name", Description = "Name to greet", IsRequired = true)]
    public string Name { get; set; } = default!;

    [CliArgument(1, "greeting", Description = "Greeting message", IsRequired = false, DefaultValue = "Hello")]
    public string Greeting { get; set; } = default!;

    [CliArgument(2, "count", Description = "Number of times to greet", IsRequired = false, DefaultValue = 1)]
    public int Count { get; set; }

    public ValueTask ExecuteAsync()
    {
        for (int i = 0; i < Count; i++)
        {
            var message = $"{Greeting}, {Name}!";
            _logger.LogInformation("Greeting: {Message}", message);
            Console.WriteLine(message);
        }
        
        return ValueTask.CompletedTask;
    }
}
