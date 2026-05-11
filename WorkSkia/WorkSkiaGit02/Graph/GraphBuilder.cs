namespace WorkSkiaGit02.Graph;

using System.Collections.Generic;
using System.Linq;

using LibGit2Sharp;

public static class GraphBuilder
{
    public static GraphData Build(string repositoryPath, int? maxCommits = null)
    {
        using var repo = new Repository(repositoryPath);

        var refsByCommit = BuildRefsLookup(repo);

        var commits = EnumerateCommits(repo);
        if (maxCommits is int limit)
        {
            commits = commits.Take(limit);
        }

        var commitList = commits.ToList();
        return LayoutCommits(commitList, refsByCommit);
    }

    private static IEnumerable<Commit> EnumerateCommits(Repository repo)
    {
        var roots = new List<object>();

        foreach (var branch in repo.Branches)
        {
            if (branch.Tip is not null)
            {
                roots.Add(branch.Tip);
            }
        }

        foreach (var tag in repo.Tags)
        {
            var commit = tag.Target.Peel<Commit>();
            if (commit is not null)
            {
                roots.Add(commit);
            }
        }

        if (repo.Head.Tip is not null)
        {
            roots.Add(repo.Head.Tip);
        }

        if (roots.Count == 0)
        {
            return Enumerable.Empty<Commit>();
        }

        var filter = new CommitFilter
        {
            SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time,
            IncludeReachableFrom = roots,
        };
        return repo.Commits.QueryBy(filter);
    }

    private static Dictionary<string, List<GraphRef>> BuildRefsLookup(Repository repo)
    {
        var result = new Dictionary<string, List<GraphRef>>(StringComparer.Ordinal);

        void Add(string sha, GraphRef graphRef)
        {
            if (!result.TryGetValue(sha, out var list))
            {
                list = [];
                result[sha] = list;
            }
            list.Add(graphRef);
        }

        var headTipSha = repo.Head.Tip?.Sha;
        if (headTipSha is not null)
        {
            Add(headTipSha, new GraphRef { Name = "HEAD", Kind = GraphRefKind.Head });
        }

        foreach (var branch in repo.Branches)
        {
            if (branch.Tip is null)
            {
                continue;
            }

            Add(branch.Tip.Sha, new GraphRef
            {
                Name = branch.FriendlyName,
                Kind = branch.IsRemote ? GraphRefKind.RemoteBranch : GraphRefKind.LocalBranch,
            });
        }

        foreach (var tag in repo.Tags)
        {
            var commit = tag.Target.Peel<Commit>();
            if (commit is null)
            {
                continue;
            }

            Add(commit.Sha, new GraphRef { Name = tag.FriendlyName, Kind = GraphRefKind.Tag });
        }

        return result;
    }

    private static GraphData LayoutCommits(
        IReadOnlyList<Commit> commits,
        IReadOnlyDictionary<string, List<GraphRef>> refsByCommit)
    {
        var lanes = new List<string?>();
        var pending = new Dictionary<string, List<PendingEdge>>(StringComparer.Ordinal);

        var nodeInfos = new NodeInfo[commits.Count];
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
                    resolvedEdges.Add(new ResolvedEdge(pe.FromRow, pe.FromLane, row, myLane));
                }
                pending.Remove(sha);
            }

            nodeInfos[row] = new NodeInfo(commit, myLane);

            if (otherLanes is not null)
            {
                foreach (var lane in otherLanes)
                {
                    lanes[lane] = null;
                }
            }
            lanes[myLane] = null;

            var parents = commit.Parents.ToList();
            for (var i = 0; i < parents.Count; i++)
            {
                var parent = parents[i];
                var parentSha = parent.Sha;

                var existingLane = lanes.IndexOf(parentSha);
                if (existingLane < 0)
                {
                    var laneForParent = (i == 0) ? myLane : AllocateLane(lanes);
                    lanes[laneForParent] = parentSha;
                }

                AddPending(pending, parentSha, row, myLane);
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
            var info = nodeInfos[i];
            var commit = info.Commit;
            var sha = commit.Sha;
            var refs = refsByCommit.TryGetValue(sha, out var refList)
                ? (IReadOnlyList<GraphRef>)refList
                : [];

            rows.Add(new GraphRow
            {
                Sha = sha,
                ShortSha = sha[..Math.Min(7, sha.Length)],
                Row = i,
                Lane = info.Lane,
                Author = commit.Author.Name,
                When = commit.Author.When,
                Summary = commit.MessageShort,
                Refs = refs,
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
        }

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
        int fromLane)
    {
        if (!pending.TryGetValue(sha, out var list))
        {
            list = [];
            pending[sha] = list;
        }
        list.Add(new PendingEdge(fromRow, fromLane));
    }

    private readonly record struct PendingEdge(int FromRow, int FromLane);

    private readonly record struct ResolvedEdge(int FromRow, int FromLane, int ToRow, int ToLane);

    private readonly record struct NodeInfo(Commit Commit, int Lane);
}
