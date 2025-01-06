namespace WorkScan.SourceGenerator.Attributes;

using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ComponentSourceAttribute : Attribute
{
    // TODO Namespace, prefix, pattern ?
    // TODO interface auto ?
    // TODO singleton...

    public string Suffix { get; }

    public ComponentSourceAttribute(string suffix)
    {
        Suffix = suffix;
    }
}
