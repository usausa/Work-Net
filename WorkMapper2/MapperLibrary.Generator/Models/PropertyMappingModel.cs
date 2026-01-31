namespace MapperLibrary.Generator.Models;

using System;

/// <summary>
/// Represents a property mapping configuration.
/// </summary>
internal sealed class PropertyMappingModel : IEquatable<PropertyMappingModel>
{
    /// <summary>
    /// Gets or sets the source property name.
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target property name.
    /// </summary>
    public string TargetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source property type as a fully qualified name.
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target property type as a fully qualified name.
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether type conversion is required.
    /// </summary>
    public bool RequiresConversion { get; set; }

    public bool Equals(PropertyMappingModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return SourceName == other.SourceName &&
               TargetName == other.TargetName &&
               SourceType == other.SourceType &&
               TargetType == other.TargetType &&
               RequiresConversion == other.RequiresConversion;
    }

    public override bool Equals(object? obj) => Equals(obj as PropertyMappingModel);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + (SourceName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TargetName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (SourceType?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TargetType?.GetHashCode() ?? 0);
            hash = (hash * 31) + RequiresConversion.GetHashCode();
            return hash;
        }
    }
}
