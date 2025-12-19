using Microsoft.Extensions.Logging;

namespace WorkCliHost;

// ============================================================================
// アプローチ2: インターフェースとミックスインパターン
// Note: C#ではインターフェースの属性は継承されないため、
// 実装クラスで明示的に属性を付ける必要があります
// ============================================================================

/// <summary>
/// Common arguments for commands that operate on a specific user.
/// Implementing classes must apply the CliArgumentAttribute.
/// </summary>
public interface IUserTargetArguments
{
    string Username { get; set; }
}

/// <summary>
/// Common arguments for role-related commands.
/// Implementing classes must apply the CliArgumentAttribute.
/// </summary>
public interface IRoleArguments
{
    string Role { get; set; }
}

// 使用例：複数のインターフェースを組み合わせる
[CliCommand("grant", Description = "Grant permission to user")]
public sealed class UserPermissionGrantCommand : ICommandDefinition, IUserTargetArguments, IRoleArguments
{
    private readonly ILogger<UserPermissionGrantCommand> _logger;

    public UserPermissionGrantCommand(ILogger<UserPermissionGrantCommand> logger)
    {
        _logger = logger;
    }

    // IUserTargetArgumentsから - 属性を明示的に付ける
    [CliArgument<string>(0, "username", Description = "Username")]
    public string Username { get; set; } = default!;

    // IRoleArgumentsから - 属性を明示的に付ける
    [CliArgument<string>(1, "role", Description = "Role name")]
    public string Role { get; set; } = default!;

    [CliArgument<string>(2, "permission", Description = "Permission to grant")]
    public string Permission { get; set; } = default!;

    public ValueTask ExecuteAsync(CommandContext context)
    {
        _logger.LogInformation("Granting permission '{Permission}' in role '{Role}' to user '{Username}'", Permission, Role, Username);
        Console.WriteLine($"Successfully granted permission '{Permission}' in role '{Role}' to user '{Username}'");
        return ValueTask.CompletedTask;
    }
}

// ============================================================================
// アプローチ3: ジェネリック基底クラス
// ============================================================================

/// <summary>
/// Generic base class for commands with common argument patterns.
/// </summary>
public abstract class CommandWithUserAndRole<TLogger> : ICommandDefinition
{
    protected readonly ILogger<TLogger> Logger;

    protected CommandWithUserAndRole(ILogger<TLogger> logger)
    {
        Logger = logger;
    }

    [CliArgument<string>(0, "username", Description = "Username")]
    public string Username { get; set; } = default!;

    [CliArgument<string>(1, "role", Description = "Role name")]
    public string Role { get; set; } = default!;

    public abstract ValueTask ExecuteAsync(CommandContext context);
}

// 使用例
[CliCommand("verify", Description = "Verify user role")]
public sealed class UserRoleVerifyCommand : CommandWithUserAndRole<UserRoleVerifyCommand>
{
    public UserRoleVerifyCommand(ILogger<UserRoleVerifyCommand> logger) : base(logger)
    {
    }

    public override ValueTask ExecuteAsync(CommandContext context)
    {
        Logger.LogInformation("Verifying role '{Role}' for user '{Username}'", Role, Username);
        Console.WriteLine($"Verifying if user '{Username}' has role '{Role}'");
        return ValueTask.CompletedTask;
    }
}

// ============================================================================
// アプローチ4: 部分クラスとコード生成（Source Generator用の準備）
// ============================================================================

/// <summary>
/// Marker attribute for commands that should include common user arguments.
/// Future: Could be processed by Source Generator to auto-generate properties.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class IncludeUserArgumentsAttribute : Attribute
{
}

/// <summary>
/// Marker attribute for commands that should include common role arguments.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class IncludeRoleArgumentsAttribute : Attribute
{
}

// 使用例（将来的にSource Generatorで自動生成可能）
[CliCommand("audit", Description = "Audit user role")]
[IncludeUserArguments]
[IncludeRoleArguments]
public sealed class UserRoleAuditCommand : ICommandDefinition
{
    private readonly ILogger<UserRoleAuditCommand> _logger;

    public UserRoleAuditCommand(ILogger<UserRoleAuditCommand> logger)
    {
        _logger = logger;
    }

    // Source Generatorで自動生成されることを想定
    [CliArgument<string>(0, "username", Description = "Username")]
    public string Username { get; set; } = default!;

    [CliArgument<string>(1, "role", Description = "Role name")]
    public string Role { get; set; } = default!;

    public ValueTask ExecuteAsync(CommandContext context)
    {
        _logger.LogInformation("Auditing role '{Role}' for user '{Username}'", Role, Username);
        Console.WriteLine($"Audit log for user '{Username}' with role '{Role}'");
        return ValueTask.CompletedTask;
    }
}
