namespace WorkSkiaGitMaui.Data;

using System;
using System.Collections.Generic;

public sealed class PseudoCommit
{
    public required string Sha { get; init; }

    public required string Author { get; init; }

    public required DateTimeOffset AuthorWhen { get; init; }

    public required string Summary { get; init; }

    public required IReadOnlyList<string> ParentShas { get; init; }
}

public sealed class PseudoRef
{
    public required string TargetSha { get; init; }

    public required string Name { get; init; }

    public required string Kind { get; init; }
}
