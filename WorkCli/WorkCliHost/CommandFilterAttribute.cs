namespace WorkCliHost;

/// <summary>
/// Base attribute for applying filters to commands.
/// Similar to FilterAttribute in ASP.NET Core.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public abstract class CommandFilterAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the order in which filters are executed. Lower values execute first.
    /// Default is 0. If not specified, filters are executed in the order they are defined in the inheritance hierarchy.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets the filter type.
    /// </summary>
    public abstract Type FilterType { get; }
}

/// <summary>
/// Attribute for applying a specific filter type to a command.
/// </summary>
/// <typeparam name="TFilter">The filter type.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class CommandFilterAttribute<TFilter> : CommandFilterAttribute
    where TFilter : ICommandFilter
{
    public override Type FilterType => typeof(TFilter);
}
