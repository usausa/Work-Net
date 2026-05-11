namespace WorkSkiaGit01.Graph;

using System.Collections.Generic;

public enum GraphRefKind
{
    Head,
    LocalBranch,
    RemoteBranch,
    Tag,
}

public sealed class GraphRef
{
    public required string Name { get; init; }

    public required GraphRefKind Kind { get; init; }
}

public sealed class GraphNode
{
    public required string Sha { get; init; }

    public required string ShortSha { get; init; }

    public required int Row { get; init; }

    public required int Lane { get; init; }

    public required string Author { get; init; }

    public required DateTimeOffset When { get; init; }

    public required string Summary { get; init; }

    public required IReadOnlyList<GraphRef> Refs { get; init; }
}

public sealed class GraphEdge
{
    public required int FromRow { get; init; }

    public required int FromLane { get; init; }

    public required int ToRow { get; init; }

    public required int ToLane { get; init; }
}

public sealed class GraphLayout
{
    public required IReadOnlyList<GraphNode> Nodes { get; init; }

    public required IReadOnlyList<GraphEdge> Edges { get; init; }

    public required int LaneCount { get; init; }

    public int RowCount => Nodes.Count;
}
