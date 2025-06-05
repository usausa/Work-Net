namespace ConsoleDemo;

internal class Program
{
    static void Main(string[] args)
    {
        // Example log: SHA, parents[], timestamp, message
        var log = new List<(string sha, string[] parents, DateTime ts, string msg)>
        {
            ("a1b2c3d", new string[0],       new DateTime(2025, 1, 1), "Initial commit"),
            ("b2c3d4e", new[]{ "a1b2c3d" },  new DateTime(2025, 1, 2), "Add README"),
            ("c3d4e5f", new[]{ "b2c3d4e" },  new DateTime(2025, 1, 3), "Feature A work"),
            ("d4e5f6a", new[]{ "b2c3d4e" },  new DateTime(2025, 1, 3), "Start feature B"),
            ("e5f6a7b", new[]{ "c3d4e5f", "d4e5f6a" }, new DateTime(2025, 1, 4), "Merge B into A"),
        };

        // Build graph
        var graph = new CommitGraph();
        graph.BuildFromLog(log);

        // Compute layout
        var layout = new GraphLayout();
        layout.ComputeLayout(graph);

        // Print to console
        PrintAsciiGraph(layout);
    }

    static void PrintAsciiGraph(GraphLayout layout)
    {
        var order = layout.RenderOrder;
        var lanes = layout.Lanes;
        var commitLane = layout.CommitLane;

        foreach (var commit in order)
        {
            int totalLanes = lanes.Count;
            for (int i = 0; i < totalLanes; i++)
            {
                if (commitLane[commit] == i)
                {
                    Console.Write("* ");
                }
                else
                {
                    Console.Write("  ");
                }
            }

            // Append SHA & message
            string sha7 = commit.Sha.Length >= 7 ? commit.Sha.Substring(0, 7) : commit.Sha;
            Console.WriteLine($" {sha7}  {commit.Message}");
        }
    }
}


// DAG
public class CommitNode
{
    public string Sha { get; }
    public List<CommitNode> Parents { get; } = new List<CommitNode>();
    public List<CommitNode> Children { get; } = new List<CommitNode>();
    public DateTime Timestamp { get; set; }
    public string Message { get; set; }

    public CommitNode(string sha)
    {
        Sha = sha;
    }
}

/// Holds all commits and builds the DAG from raw log entries.
public class CommitGraph
{
    /// All commit nodes indexed by SHA.
    public Dictionary<string, CommitNode> Nodes { get; } = new Dictionary<string, CommitNode>();

    /// Build the DAG from a sequence of (sha, parentShas, timestamp, message).
    public void BuildFromLog(IEnumerable<(string sha, string[] parentShas, DateTime ts, string msg)> log)
    {
        // Create nodes
        foreach (var entry in log)
        {
            if (!Nodes.ContainsKey(entry.sha))
            {
                var node = new CommitNode(entry.sha)
                {
                    Timestamp = entry.ts,
                    Message = entry.msg
                };
                Nodes[entry.sha] = node;
            }
        }

        // Hook up parent/child relationships
        foreach (var entry in log)
        {
            var node = Nodes[entry.sha];
            foreach (var p in entry.parentShas)
            {
                if (Nodes.TryGetValue(p, out var parent))
                {
                    node.Parents.Add(parent);
                    parent.Children.Add(node);
                }
            }
        }
    }
}

/// Computes a simple column-based layout for console rendering.
public class GraphLayout
{
    public List<CommitNode> RenderOrder { get; private set; }
    public Dictionary<CommitNode, int> CommitLane { get; private set; }
    public List<CommitNode?> Lanes { get; private set; }

    /// Compute layout: sort by timestamp, assign each commit to a column (lane).
    public void ComputeLayout(CommitGraph graph)
    {
        // 1) Simple time-based order (oldest first)
        RenderOrder = graph.Nodes.Values
                                .OrderBy(n => n.Timestamp)
                                .ToList();

        CommitLane = new Dictionary<CommitNode, int>();
        Lanes = new List<CommitNode?>();

        // 2) Walk through commits in render order and assign lanes
        foreach (var commit in RenderOrder)
        {
            int lane = FindLaneFor(commit);
            CommitLane[commit] = lane;
            Lanes[lane] = commit;

            // Ensure parents remain in lanes for edge consistency
            foreach (var parent in commit.Parents)
            {
                if (!CommitLane.ContainsKey(parent))
                {
                    int pl = FindLaneFor(parent);
                    CommitLane[parent] = pl;
                    Lanes[pl] = parent;
                }
            }
        }
    }

    private int FindLaneFor(CommitNode commit)
    {
        // Try to reuse a parent's lane
        foreach (var p in commit.Parents)
        {
            if (CommitLane.TryGetValue(p, out var pl) && Lanes[pl] == p)
                return pl;
        }

        // Find an empty lane
        for (int i = 0; i < Lanes.Count; i++)
        {
            if (Lanes[i] == null)
                return i;
        }

        // Otherwise append a new lane
        Lanes.Add(null);
        return Lanes.Count - 1;
    }
}
