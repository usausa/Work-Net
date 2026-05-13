namespace WorkSkiaGitMaui.Graph;

using System;
using System.Collections.Generic;

using WorkSkiaGitMaui.Data;

public static class GraphBuilder
{
    public static GraphData Build(
        IReadOnlyList<PseudoCommit> commits,
        IReadOnlyList<PseudoRef> refs)
    {
        var refsByCommit = BuildRefsLookup(commits, refs);
        return LayoutCommits(commits, refsByCommit);
    }

    private static Dictionary<string, List<GraphRef>> BuildRefsLookup(
        IReadOnlyList<PseudoCommit> commits,
        IReadOnlyList<PseudoRef> refs)
    {
        var knownShas = new HashSet<string>(StringComparer.Ordinal);
        foreach (var commit in commits)
        {
            knownShas.Add(commit.Sha);
        }

        var result = new Dictionary<string, List<GraphRef>>(StringComparer.Ordinal);
        foreach (var r in refs)
        {
            if (!knownShas.Contains(r.TargetSha))
            {
                continue;
            }

            if (!Enum.TryParse<GraphRefKind>(r.Kind, ignoreCase: false, out var kind))
            {
                continue;
            }

            if (!result.TryGetValue(r.TargetSha, out var list))
            {
                list = [];
                result[r.TargetSha] = list;
            }
            list.Add(new GraphRef { Name = r.Name, Kind = kind });
        }

        return result;
    }

    private static GraphData LayoutCommits(
        IReadOnlyList<PseudoCommit> commits,
        IReadOnlyDictionary<string, List<GraphRef>> refsByCommit)
    {
        var lanes = new List<string?>();
        var pending = new Dictionary<string, List<PendingEdge>>(StringComparer.Ordinal);

        var nodeLanes = new int[commits.Count];
        var resolvedEdges = new List<ResolvedEdge>();

        for (var row = 0; row < commits.Count; row++)
        {
            var commit = commits[row];
            var sha = commit.Sha;

            var myLane = -1;
            List<int>? otherLanes = null;
            for (var lane = 0; lane < lanes.Count; lane++)
            {
                if (lanes[lane] == sha)
                {
                    if (myLane < 0)
                    {
                        myLane = lane;
                    }
                    else
                    {
                        otherLanes ??= [];
                        otherLanes.Add(lane);
                    }
                }
            }

            if (myLane < 0)
            {
                myLane = AllocateLane(lanes);
            }

            if (pending.TryGetValue(sha, out var pendingList))
            {
                foreach (var pe in pendingList)
                {
                    resolvedEdges.Add(new ResolvedEdge(pe.FromRow, pe.FromLane, row, myLane, pe.BranchFromSource));
                }
                pending.Remove(sha);
            }

            nodeLanes[row] = myLane;

            if (otherLanes is not null)
            {
                foreach (var lane in otherLanes)
                {
                    lanes[lane] = null;
                }
            }
            lanes[myLane] = null;

            var parents = commit.ParentShas;
            for (var i = 0; i < parents.Count; i++)
            {
                var parentSha = parents[i];

                var existingLane = lanes.IndexOf(parentSha);
                if (existingLane < 0)
                {
                    var laneForParent = (i == 0) ? myLane : AllocateLane(lanes);
                    lanes[laneForParent] = parentSha;
                }

                AddPending(pending, parentSha, row, myLane, branchFromSource: i > 0);
            }
        }

        var laneCount = lanes.Count;
        var segmentsByRow = new List<GraphSegment>[commits.Count];
        for (var i = 0; i < segmentsByRow.Length; i++)
        {
            segmentsByRow[i] = [];
        }

        foreach (var edge in resolvedEdges)
        {
            AppendEdgeSegments(segmentsByRow, edge);
        }

        var rows = new List<GraphRow>(commits.Count);
        for (var i = 0; i < commits.Count; i++)
        {
            var commit = commits[i];
            var sha = commit.Sha;
            var refList = refsByCommit.TryGetValue(sha, out var list)
                ? (IReadOnlyList<GraphRef>)list
                : [];

            rows.Add(new GraphRow
            {
                Sha = sha,
                ShortSha = sha[..Math.Min(7, sha.Length)],
                Row = i,
                Lane = nodeLanes[i],
                Author = commit.Author,
                When = commit.AuthorWhen,
                Summary = commit.Summary,
                Refs = refList,
                Segments = segmentsByRow[i],
                LaneCount = laneCount,
            });
        }

        return new GraphData
        {
            Rows = rows,
            LaneCount = laneCount,
        };
    }

    private static void AppendEdgeSegments(List<GraphSegment>[] segmentsByRow, ResolvedEdge edge)
    {
        var colorIndex = edge.ToLane;

        if (edge.FromLane == edge.ToLane)
        {
            segmentsByRow[edge.FromRow].Add(new GraphSegment
            {
                Kind = GraphSegmentKind.HalfVerticalBottom,
                Lane = edge.FromLane,
                ColorIndex = colorIndex,
            });

            for (var r = edge.FromRow + 1; r < edge.ToRow; r++)
            {
                segmentsByRow[r].Add(new GraphSegment
                {
                    Kind = GraphSegmentKind.Vertical,
                    Lane = edge.ToLane,
                    ColorIndex = colorIndex,
                });
            }

            segmentsByRow[edge.ToRow].Add(new GraphSegment
            {
                Kind = GraphSegmentKind.HalfVerticalTop,
                Lane = edge.ToLane,
                ColorIndex = colorIndex,
            });
        }
        else if (edge.BranchFromSource)
        {
            segmentsByRow[edge.FromRow].Add(new GraphSegment
            {
                Kind = GraphSegmentKind.HalfVerticalBottom,
                Lane = edge.FromLane,
                ColorIndex = colorIndex,
            });

            var diagRow = edge.FromRow + 1;
            segmentsByRow[diagRow].Add(new GraphSegment
            {
                Kind = GraphSegmentKind.DiagonalBranch,
                Lane = edge.FromLane,
                ToLane = edge.ToLane,
                ColorIndex = colorIndex,
            });

            if (diagRow < edge.ToRow)
            {
                segmentsByRow[diagRow].Add(new GraphSegment
                {
                    Kind = GraphSegmentKind.HalfVerticalBottom,
                    Lane = edge.ToLane,
                    ColorIndex = colorIndex,
                });

                for (var r = diagRow + 1; r < edge.ToRow; r++)
                {
                    segmentsByRow[r].Add(new GraphSegment
                    {
                        Kind = GraphSegmentKind.Vertical,
                        Lane = edge.ToLane,
                        ColorIndex = colorIndex,
                    });
                }

                segmentsByRow[edge.ToRow].Add(new GraphSegment
                {
                    Kind = GraphSegmentKind.HalfVerticalTop,
                    Lane = edge.ToLane,
                    ColorIndex = colorIndex,
                });
            }
        }
        else
        {
            segmentsByRow[edge.FromRow].Add(new GraphSegment
            {
                Kind = GraphSegmentKind.Diagonal,
                Lane = edge.FromLane,
                ToLane = edge.ToLane,
                ColorIndex = colorIndex,
            });

            for (var r = edge.FromRow + 1; r < edge.ToRow; r++)
            {
                segmentsByRow[r].Add(new GraphSegment
                {
                    Kind = GraphSegmentKind.Vertical,
                    Lane = edge.ToLane,
                    ColorIndex = colorIndex,
                });
            }

            segmentsByRow[edge.ToRow].Add(new GraphSegment
            {
                Kind = GraphSegmentKind.HalfVerticalTop,
                Lane = edge.ToLane,
                ColorIndex = colorIndex,
            });
        }
    }

    private static int AllocateLane(List<string?> lanes)
    {
        for (var i = 0; i < lanes.Count; i++)
        {
            if (lanes[i] is null)
            {
                return i;
            }
        }

        lanes.Add(null);
        return lanes.Count - 1;
    }

    private static void AddPending(
        Dictionary<string, List<PendingEdge>> pending,
        string sha,
        int fromRow,
        int fromLane,
        bool branchFromSource)
    {
        if (!pending.TryGetValue(sha, out var list))
        {
            list = [];
            pending[sha] = list;
        }
        list.Add(new PendingEdge(fromRow, fromLane, branchFromSource));
    }

    private readonly record struct PendingEdge(int FromRow, int FromLane, bool BranchFromSource);

    private readonly record struct ResolvedEdge(int FromRow, int FromLane, int ToRow, int ToLane, bool BranchFromSource);
}
