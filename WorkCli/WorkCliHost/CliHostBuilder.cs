using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;

namespace WorkCliHost;

internal sealed class CliHostBuilder : ICliHostBuilder
{
    private readonly string[] _args;
    private readonly ServiceCollection _services = new();
    private readonly List<Action<RootCommand>> _commandConfigurations = new();
    private RootCommand? _customRootCommand;

    public CliHostBuilder(string[] args)
    {
        _args = args;

        _services.AddLogging(builder =>
        {
            builder.AddConsole();
        });
    }

    public ICliHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
    {
        configureServices(_services);
        return this;
    }

    public ICliHostBuilder ConfigureCommands(Action<RootCommand> configureRoot)
    {
        _commandConfigurations.Add(configureRoot);
        return this;
    }

    public ICliHostBuilder UseRootCommand(RootCommand rootCommand)
    {
        _customRootCommand = rootCommand;
        return this;
    }

    public ICliHost Build()
    {
        var serviceProvider = _services.BuildServiceProvider();
        
        var rootCommand = _customRootCommand ?? new RootCommand();
        
        foreach (var configure in _commandConfigurations)
        {
            configure(rootCommand);
        }
        
        var commandRegistrations = serviceProvider.GetServices<CommandRegistration>();
        foreach (var registration in commandRegistrations)
        {
            var command = CreateCommandWithSubCommands(registration, serviceProvider);
            rootCommand.Subcommands.Add(command);
        }
        
        return new CliHostImplementation(_args, rootCommand, serviceProvider);
    }

    private Command CreateCommandWithSubCommands(CommandRegistration registration, IServiceProvider serviceProvider)
    {
        var command = CreateCommand(registration.CommandType, serviceProvider);
        
        foreach (var subRegistration in registration.SubCommands)
        {
            var subCommand = CreateCommandWithSubCommands(subRegistration, serviceProvider);
            command.Subcommands.Add(subCommand);
        }
        
        return command;
    }

    private Command CreateCommand(Type commandType, IServiceProvider serviceProvider)
    {
        var attribute = commandType.GetCustomAttribute<CliCommandAttribute>()
            ?? throw new InvalidOperationException($"Type {commandType.Name} must have CliCommandAttribute");

        var command = new Command(attribute.Name, attribute.Description);

        // ICommandDefinitionを実装しているかチェック
        var isExecutableCommand = typeof(ICommandDefinition).IsAssignableFrom(commandType);
        
        if (isExecutableCommand)
        {
            var properties = commandType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new
                {
                    Property = p,
                    Attribute = GetCliArgumentAttribute(p)
                })
                .Where(x => x.Attribute != null)
                .OrderBy(x => x.Attribute!.Position)
                .ToList();

            var arguments = new List<(Argument Argument, PropertyInfo Property, Type ArgumentType)>();
            foreach (var prop in properties)
            {
                var argAttr = prop.Attribute!;
                var argumentType = typeof(Argument<>).MakeGenericType(prop.Property.PropertyType);
                
                // Argument<T>のコンストラクタ: Argument(string name)
                var argument = (Argument)Activator.CreateInstance(argumentType, argAttr.Name)!;
                
                // Descriptionプロパティを設定
                var descriptionProperty = argumentType.GetProperty("Description");
                if (descriptionProperty != null && argAttr.Description != null)
                {
                    descriptionProperty.SetValue(argument, argAttr.Description);
                }

                // デフォルト値を設定
                var defaultValue = GetDefaultValue(prop.Property, argAttr);
                if (defaultValue.HasValue)
                {
                    var defaultValueFactoryProperty = argumentType.GetProperty("DefaultValueFactory");
                    if (defaultValueFactoryProperty != null)
                    {
                        // Func<ArgumentResult, T>のデリゲートを作成
                        var argumentResultType = typeof(ArgumentResult);
                        var funcType = typeof(Func<,>).MakeGenericType(argumentResultType, prop.Property.PropertyType);
                        
                        var capturedValue = defaultValue.Value;
                        var lambdaMethod = GetType().GetMethod(nameof(CreateDefaultValueFactory), BindingFlags.NonPublic | BindingFlags.Static)!
                            .MakeGenericMethod(prop.Property.PropertyType);
                        
                        var factoryDelegate = lambdaMethod.Invoke(null, [capturedValue]);
                        defaultValueFactoryProperty.SetValue(argument, factoryDelegate);
                    }
                }

                arguments.Add((argument, prop.Property, argumentType));
                command.Arguments.Add(argument);
            }

            command.SetAction(parseResult =>
            {
                var instance = (ICommandDefinition)ActivatorUtilities.CreateInstance(serviceProvider, commandType);

                foreach (var (argument, property, argumentType) in arguments)
                {
                    // GetValueメソッドを呼び出す
                    var getValueMethod = typeof(ParseResult).GetMethod("GetValue", [argumentType])
                        ?? typeof(ParseResult).GetMethod("GetValue", 1, [argumentType]);
                    
                    if (getValueMethod == null)
                    {
                        // 汎用的なGetValueメソッドを取得
                        var methods = typeof(ParseResult).GetMethods()
                            .Where(m => m.Name == "GetValue" && m.IsGenericMethod && m.GetParameters().Length == 1);
                        
                        foreach (var method in methods)
                        {
                            var parameters = method.GetParameters();
                            if (parameters[0].ParameterType.IsGenericType && 
                                parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(Argument<>))
                            {
                                getValueMethod = method.MakeGenericMethod(property.PropertyType);
                                break;
                            }
                        }
                    }
                    
                    if (getValueMethod != null)
                    {
                        var value = getValueMethod.Invoke(parseResult, [argument]);
                        property.SetValue(instance, value);
                    }
                }

                instance.ExecuteAsync().AsTask().Wait();
                return 0;
            });
        }
        // ICommandGroupまたはサブコマンドのみの場合は、アクションを設定しない
        // System.CommandLineはアクションがない場合、自動的にヘルプを表示する

        return command;
    }

    private static Func<ArgumentResult, T> CreateDefaultValueFactory<T>(object? value)
    {
        return _ => (T)value!;
    }

    private static CliArgumentInfo? GetCliArgumentAttribute(PropertyInfo property)
    {
        // ジェネリック版の属性を検索
        var genericAttr = property.GetCustomAttributes(true)
            .FirstOrDefault(a => a.GetType().IsGenericType &&
                                 a.GetType().GetGenericTypeDefinition() == typeof(CliArgumentAttribute<>));

        if (genericAttr != null)
        {
            var attrType = genericAttr.GetType();
            var position = (int)attrType.GetProperty("Position")!.GetValue(genericAttr)!;
            var name = (string)attrType.GetProperty("Name")!.GetValue(genericAttr)!;
            var description = (string?)attrType.GetProperty("Description")?.GetValue(genericAttr);
            var isRequired = (bool)attrType.GetProperty("IsRequired")!.GetValue(genericAttr)!;
            var defaultValue = attrType.GetProperty("DefaultValue")?.GetValue(genericAttr);

            return new CliArgumentInfo(position, name, description, isRequired, defaultValue);
        }

        // 非ジェネリック版の属性を検索
        var attr = property.GetCustomAttribute<CliArgumentAttribute>();
        if (attr != null)
        {
            return new CliArgumentInfo(attr.Position, attr.Name, attr.Description, attr.IsRequired, null);
        }

        return null;
    }

    private static (bool HasValue, object? Value) GetDefaultValue(PropertyInfo property, CliArgumentInfo argInfo)
    {
        if (argInfo.DefaultValue != null)
        {
            return (true, argInfo.DefaultValue);
        }

        if (!argInfo.IsRequired)
        {
            var defaultValue = property.PropertyType.IsValueType
                ? Activator.CreateInstance(property.PropertyType)
                : null;
            return (true, defaultValue);
        }

        return (false, null);
    }
}

internal sealed class CliHostImplementation : ICliHost
{
    private readonly string[] _args;
    private readonly RootCommand _rootCommand;
    private readonly IServiceProvider _serviceProvider;

    public CliHostImplementation(string[] args, RootCommand rootCommand, IServiceProvider serviceProvider)
    {
        _args = args;
        _rootCommand = rootCommand;
        _serviceProvider = serviceProvider;
    }

    public async Task<int> RunAsync()
    {
        return _rootCommand.Parse(_args).Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

internal sealed record CliArgumentInfo(
    int Position,
    string Name,
    string? Description,
    bool IsRequired,
    object? DefaultValue);
