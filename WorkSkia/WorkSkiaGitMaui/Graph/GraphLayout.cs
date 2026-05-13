namespace WorkSkiaGitMaui.Graph;

using System;
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

public enum GraphSegmentKind
{
    Vertical,
    HalfVerticalTop,
    HalfVerticalBottom,
    Diagonal,
    DiagonalBranch,
}

public sealed class GraphSegment
{
    public required GraphSegmentKind Kind { get; init; }

    public required int Lane { get; init; }

    public int ToLane { get; init; }

    public required int ColorIndex { get; init; }
}

public sealed class GraphRow
{
    public required string Sha { get; init; }

    public required string ShortSha { get; init; }

    public required int Row { get; init; }

    public required int Lane { get; init; }

    public required string Author { get; init; }

    public required DateTimeOffset When { get; init; }

    public required string Summary { get; init; }

    public required IReadOnlyList<GraphRef> Refs { get; init; }

    public required IReadOnlyList<GraphSegment> Segments { get; init; }

    public required int LaneCount { get; init; }
}

public sealed class GraphData
{
    public required IReadOnlyList<GraphRow> Rows { get; init; }

    public required int LaneCount { get; init; }
}
