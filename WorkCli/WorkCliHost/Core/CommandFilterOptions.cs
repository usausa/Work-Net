namespace WorkCliHost.Core;

/// <summary>
/// Options for configuring command filters.
/// </summary>
public sealed class CommandFilterOptions
{
    /// <summary>
    /// Gets the collection of global filters that apply to all commands.
    /// </summary>
    public List<GlobalFilterDescriptor> GlobalFilters { get; } = new();

    /// <summary>
    /// Gets or sets whether to include filters from base classes.
    /// Default is true.
    /// </summary>
    public bool IncludeBaseClassFilters { get; set; } = true;

    /// <summary>
    /// Gets or sets the default order for filters when not explicitly specified.
    /// Default is 0.
    /// </summary>
    public int DefaultFilterOrder { get; set; } = 0;
}

/// <summary>
/// Descriptor for a global filter.
/// </summary>
public sealed class GlobalFilterDescriptor
{
    public Type FilterType { get; }
    public int Order { get; }

    public GlobalFilterDescriptor(Type filterType, int order = 0)
    {
        if (!typeof(ICommandFilter).IsAssignableFrom(filterType))
        {
            throw new ArgumentException($"Filter type must implement {nameof(ICommandFilter)}", nameof(filterType));
        }

        FilterType = filterType;
        Order = order;
    }
}
