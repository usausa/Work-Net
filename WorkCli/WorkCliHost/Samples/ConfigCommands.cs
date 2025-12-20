using Microsoft.Extensions.Logging;
using WorkCliHost.Core;

namespace WorkCliHost.Samples;

// ============================================================================
// Position省略のテスト
// ============================================================================

[CliCommand("config", Description = "Configuration management")]
public sealed class ConfigCommand : ICommandGroup
{
}

/// <summary>
/// Position省略例: すべて自動で順序決定
/// </summary>
[CliCommand("set", Description = "Set configuration value")]
public sealed class ConfigSetCommand : ICommandDefinition
{
    private readonly ILogger<ConfigSetCommand> _logger;

    public ConfigSetCommand(ILogger<ConfigSetCommand> logger)
    {
        _logger = logger;
    }

    // Position省略 - プロパティ定義順で自動決定
    [CliArgument<string>("key", Description = "Configuration key")]
    public string Key { get; set; } = default!;

    [CliArgument<string>("value", Description = "Configuration value")]
    public string Value { get; set; } = default!;

    [CliArgument<string>("environment", Description = "Target environment", IsRequired = false, DefaultValue = "development")]
    public string Environment { get; set; } = default!;

    public ValueTask ExecuteAsync(CommandContext context)
    {
        _logger.LogInformation("Setting {Key}={Value} for environment {Environment}", Key, Value, Environment);
        Console.WriteLine($"Set {Key}={Value} for environment '{Environment}'");
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Position一部指定例: 明示的な指定と自動を組み合わせ
/// </summary>
[CliCommand("get", Description = "Get configuration value")]
public sealed class ConfigGetCommand : ICommandDefinition
{
    private readonly ILogger<ConfigGetCommand> _logger;

    public ConfigGetCommand(ILogger<ConfigGetCommand> logger)
    {
        _logger = logger;
    }

    // Position明示指定 - 0番目
    [CliArgument<string>(0, "key", Description = "Configuration key")]
    public string Key { get; set; } = default!;

    // Position省略 - 明示的なPositionの後に自動配置される
    [CliArgument<string>("environment", Description = "Target environment", IsRequired = false, DefaultValue = "development")]
    public string Environment { get; set; } = default!;

    public ValueTask ExecuteAsync(CommandContext context)
    {
        _logger.LogInformation("Getting {Key} for environment {Environment}", Key, Environment);
        Console.WriteLine($"Getting {Key} for environment '{Environment}'");
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// 基底クラスと派生クラスでのPosition省略テスト
/// </summary>
public abstract class DeploymentCommandBase : ICommandDefinition
{
    // 基底クラスのプロパティは派生クラスより先に来る
    [CliArgument<string>("application", Description = "Application name")]
    public string Application { get; set; } = default!;

    [CliArgument<string>("version", Description = "Application version")]
    public string Version { get; set; } = default!;

    public abstract ValueTask ExecuteAsync(CommandContext context);
}

[CliCommand("deploy", Description = "Deploy application")]
public sealed class DeployCommand : DeploymentCommandBase
{
    private readonly ILogger<DeployCommand> _logger;

    public DeployCommand(ILogger<DeployCommand> logger)
    {
        _logger = logger;
    }

    // 派生クラスのプロパティは基底クラスの後に来る
    [CliArgument<string>("target", Description = "Deployment target", IsRequired = false, DefaultValue = "staging")]
    public string Target { get; set; } = default!;

    [CliArgument<bool>("force", Description = "Force deployment", IsRequired = false, DefaultValue = false)]
    public bool Force { get; set; }

    public override ValueTask ExecuteAsync(CommandContext context)
    {
        _logger.LogInformation("Deploying {Application} v{Version} to {Target} (force: {Force})", 
            Application, Version, Target, Force);
        Console.WriteLine($"Deploying {Application} v{Version} to {Target}{(Force ? " (forced)" : "")}");
        return ValueTask.CompletedTask;
    }
}
