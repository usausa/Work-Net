using Microsoft.Extensions.Logging;

namespace WorkCliHost;

[CliCommand("user", Description = "User management commands")]
public sealed class UserCommand : ICommandDefinition
{
    private readonly ILogger<UserCommand> _logger;

    public UserCommand(ILogger<UserCommand> logger)
    {
        _logger = logger;
    }

    public ValueTask ExecuteAsync()
    {
        _logger.LogInformation("User command executed. Please specify a subcommand.");
        Console.WriteLine("User management. Use --help to see available subcommands.");
        return ValueTask.CompletedTask;
    }
}

[CliCommand("list", Description = "List all users")]
public sealed class UserListCommand : ICommandDefinition
{
    private readonly ILogger<UserListCommand> _logger;

    public UserListCommand(ILogger<UserListCommand> logger)
    {
        _logger = logger;
    }

    [CliArgument<int>(0, "count", Description = "Number of users to list", IsRequired = false, DefaultValue = 10)]
    public int Count { get; set; }

    public ValueTask ExecuteAsync()
    {
        _logger.LogInformation("Listing {Count} users", Count);
        Console.WriteLine($"Listing {Count} users:");
        for (int i = 1; i <= Count; i++)
        {
            Console.WriteLine($"  {i}. User{i}");
        }
        return ValueTask.CompletedTask;
    }
}

[CliCommand("add", Description = "Add a new user")]
public sealed class UserAddCommand : ICommandDefinition
{
    private readonly ILogger<UserAddCommand> _logger;

    public UserAddCommand(ILogger<UserAddCommand> logger)
    {
        _logger = logger;
    }

    [CliArgument<string>(0, "username", Description = "Username to add")]
    public string Username { get; set; } = default!;

    [CliArgument<string>(1, "email", Description = "User email address")]
    public string Email { get; set; } = default!;

    public ValueTask ExecuteAsync()
    {
        _logger.LogInformation("Adding user: {Username} ({Email})", Username, Email);
        Console.WriteLine($"Successfully added user: {Username} ({Email})");
        return ValueTask.CompletedTask;
    }
}

[CliCommand("role", Description = "User role management")]
public sealed class UserRoleCommand : ICommandDefinition
{
    private readonly ILogger<UserRoleCommand> _logger;

    public UserRoleCommand(ILogger<UserRoleCommand> logger)
    {
        _logger = logger;
    }

    public ValueTask ExecuteAsync()
    {
        _logger.LogInformation("User role command executed. Please specify a subcommand.");
        Console.WriteLine("User role management. Use --help to see available subcommands.");
        return ValueTask.CompletedTask;
    }
}

[CliCommand("assign", Description = "Assign role to user")]
public sealed class UserRoleAssignCommand : ICommandDefinition
{
    private readonly ILogger<UserRoleAssignCommand> _logger;

    public UserRoleAssignCommand(ILogger<UserRoleAssignCommand> logger)
    {
        _logger = logger;
    }

    [CliArgument<string>(0, "username", Description = "Username")]
    public string Username { get; set; } = default!;

    [CliArgument<string>(1, "role", Description = "Role name")]
    public string Role { get; set; } = default!;

    public ValueTask ExecuteAsync()
    {
        _logger.LogInformation("Assigning role '{Role}' to user '{Username}'", Role, Username);
        Console.WriteLine($"Successfully assigned role '{Role}' to user '{Username}'");
        return ValueTask.CompletedTask;
    }
}

[CliCommand("remove", Description = "Remove role from user")]
public sealed class UserRoleRemoveCommand : ICommandDefinition
{
    private readonly ILogger<UserRoleRemoveCommand> _logger;

    public UserRoleRemoveCommand(ILogger<UserRoleRemoveCommand> logger)
    {
        _logger = logger;
    }

    [CliArgument<string>(0, "username", Description = "Username")]
    public string Username { get; set; } = default!;

    [CliArgument<string>(1, "role", Description = "Role name")]
    public string Role { get; set; } = default!;

    public ValueTask ExecuteAsync()
    {
        _logger.LogInformation("Removing role '{Role}' from user '{Username}'", Role, Username);
        Console.WriteLine($"Successfully removed role '{Role}' from user '{Username}'");
        return ValueTask.CompletedTask;
    }
}
