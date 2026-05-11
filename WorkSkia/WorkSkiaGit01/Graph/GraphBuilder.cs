namespace WorkSkiaGit01.Graph;

using System.Collections.Generic;
using System.Linq;

using LibGit2Sharp;

public static class GraphBuilder
{
    public static GraphLayout Build(string repositoryPath, int? maxCommits = null)
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

    private static GraphLayout LayoutCommits(
        IReadOnlyList<Commit> commits,
        IReadOnlyDictionary<string, List<GraphRef>> refsByCommit)
    {
        var lanes = new List<string?>();
        var pending = new Dictionary<string, List<PendingEdge>>(StringComparer.Ordinal);

        var nodes = new List<GraphNode>(commits.Count);
        var edges = new List<GraphEdge>();

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
                    edges.Add(new GraphEdge
                    {
                        FromRow = pe.FromRow,
                        FromLane = pe.FromLane,
                        ToRow = row,
                        ToLane = myLane,
                    });
                }
                pending.Remove(sha);
            }

            var refs = refsByCommit.TryGetValue(sha, out var refList)
                ? (IReadOnlyList<GraphRef>)refList
                : [];

            nodes.Add(new GraphNode
            {
                Sha = sha,
                ShortSha = sha[..Math.Min(7, sha.Length)],
                Row = row,
                Lane = myLane,
                Author = commit.Author.Name,
                When = commit.Author.When,
                Summary = commit.MessageShort,
                Refs = refs,
            });

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

        return new GraphLayout
        {
            Nodes = nodes,
            Edges = edges,
            LaneCount = lanes.Count,
        };
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
}
