using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace WorkCliHost.Core;

/// <summary>
/// Provides helper methods for building commands.
/// These methods are used by the reflection-based builder and can be referenced by Source Generators.
/// </summary>
public static class CommandBuilderHelpers
{
    /// <summary>
    /// Creates a command action builder using reflection.
    /// This is the default builder when no custom builder is specified.
    /// </summary>
    public static CommandActionBuilder CreateReflectionBasedActionBuilder(Type commandType)
    {
        return context =>
        {
            // 1. 属性からプロパティと引数情報を収集
            var propertyInfos = CollectPropertiesWithArguments(commandType);
            var argumentList = new List<Argument>();
            var propertyArgumentMap = new List<(PropertyInfo Property, Argument Argument)>();

            // 2. 引数を作成
            foreach (var (property, argAttr) in propertyInfos)
            {
                var argumentType = typeof(Argument<>).MakeGenericType(property.PropertyType);
                var argument = (Argument)Activator.CreateInstance(argumentType, argAttr.Name)!;
                
                // Description設定
                if (argAttr.Description != null)
                {
                    var descriptionProperty = argumentType.GetProperty("Description");
                    descriptionProperty?.SetValue(argument, argAttr.Description);
                }

                // デフォルト値設定
                var defaultValue = GetDefaultValue(property, argAttr);
                if (defaultValue.HasValue)
                {
                    SetDefaultValue(argument, argumentType, property.PropertyType, defaultValue.Value);
                }

                argumentList.Add(argument);
                propertyArgumentMap.Add((property, argument));
            }

            // 3. コアアクションを作成
            CommandActionDelegate coreAction = async (commandInstance, parseResult, commandContext) =>
            {
                // インスタンスは既に生成済み（呼び出し側が生成）
                
                // 引数値を設定（リフレクション）
                foreach (var (property, argument) in propertyArgumentMap)
                {
                    var argumentType = typeof(Argument<>).MakeGenericType(property.PropertyType);
                    var value = GetArgumentValue(parseResult, argument, argumentType, property.PropertyType);
                    property.SetValue(commandInstance, value);
                }

                // コマンド実行
                await commandInstance.ExecuteAsync(commandContext);
            };

            // 4. 引数リストとコアアクションを返す
            return (argumentList, coreAction);
        };
    }

    /// <summary>
    /// Collects properties with argument attributes, considering inheritance hierarchy.
    /// </summary>
    public static List<(PropertyInfo Property, CliArgumentInfo Attribute)> CollectPropertiesWithArguments(Type commandType)
    {
        var typeHierarchy = new List<Type>();
        var currentType = commandType;
        
        // 継承階層を収集（派生→基底の順）
        while (currentType != null && currentType != typeof(object))
        {
            typeHierarchy.Add(currentType);
            currentType = currentType.BaseType;
        }
        
        // 基底→派生の順に反転
        typeHierarchy.Reverse();

        var allProperties = new List<(PropertyInfo Property, CliArgumentInfo Attribute, int TypeLevel, int PropertyIndex)>();
        
        for (int typeLevel = 0; typeLevel < typeHierarchy.Count; typeLevel++)
        {
            var type = typeHierarchy[typeLevel];
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
            for (int propIndex = 0; propIndex < properties.Length; propIndex++)
            {
                var property = properties[propIndex];
                var argAttr = GetCliArgumentAttribute(property);
                
                if (argAttr != null)
                {
                    allProperties.Add((property, argAttr, typeLevel, propIndex));
                }
            }
        }

        // ソート順序:
        // 1. Position指定がある場合は、そのPositionで優先
        // 2. Position指定がない場合（AutoPosition）は、TypeLevel（基底クラスが先）→ PropertyIndex
        var sortedProperties = allProperties
            .OrderBy(p =>
            {
                if (p.Attribute.Position != CliArgumentAttribute<object>.AutoPosition)
                {
                    // 明示的なPositionがある場合は、それを最優先
                    return (0, p.Attribute.Position, 0, 0);
                }
                else
                {
                    // AutoPositionの場合は、TypeLevel → PropertyIndexでソート
                    return (1, 0, p.TypeLevel, p.PropertyIndex);
                }
            })
            .Select(p => (p.Property, p.Attribute))
            .ToList();

        return sortedProperties;
    }

    /// <summary>
    /// Gets the CliArgumentAttribute from a property.
    /// </summary>
    public static CliArgumentInfo? GetCliArgumentAttribute(PropertyInfo property)
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

    /// <summary>
    /// Gets the default value for an argument.
    /// </summary>
    public static (bool HasValue, object? Value) GetDefaultValue(PropertyInfo property, CliArgumentInfo argInfo)
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

    /// <summary>
    /// Sets the default value for an argument.
    /// </summary>
    public static void SetDefaultValue(Argument argument, Type argumentType, Type propertyType, object? value)
    {
        var defaultValueFactoryProperty = argumentType.GetProperty("DefaultValueFactory");
        if (defaultValueFactoryProperty != null)
        {
            // Func<ArgumentResult, T>のデリゲートを作成
            var argumentResultType = typeof(ArgumentResult);
            var funcType = typeof(Func<,>).MakeGenericType(argumentResultType, propertyType);
            
            var method = typeof(CommandBuilderHelpers)
                .GetMethod(nameof(CreateDefaultValueFactory), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(propertyType);
            
            var factoryDelegate = method.Invoke(null, new[] { value });
            defaultValueFactoryProperty.SetValue(argument, factoryDelegate);
        }
    }

    /// <summary>
    /// Gets the argument value from parse result.
    /// </summary>
    public static object? GetArgumentValue(ParseResult parseResult, Argument argument, Type argumentType, Type propertyType)
    {
        // GetValueメソッドを呼び出す
        var getValueMethod = typeof(ParseResult).GetMethod("GetValue", new[] { argumentType })
            ?? typeof(ParseResult).GetMethod("GetValue", 1, new[] { argumentType });
        
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
                    getValueMethod = method.MakeGenericMethod(propertyType);
                    break;
                }
            }
        }
        
        if (getValueMethod != null)
        {
            return getValueMethod.Invoke(parseResult, new object[] { argument });
        }

        return null;
    }

    private static Func<ArgumentResult, T> CreateDefaultValueFactory<T>(object? value)
    {
        return _ => (T)value!;
    }
}

/// <summary>
/// Information about a CLI argument extracted from attributes.
/// </summary>
public sealed record CliArgumentInfo(
    int Position,
    string Name,
    string? Description,
    bool IsRequired,
    object? DefaultValue);
