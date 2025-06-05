namespace ConsoleDemo2;

public class CommitNode
{
    public string Sha { get; }
    public List<CommitNode> Parents { get; } = [];
    public List<CommitNode> Children { get; } = [];
    public DateTime Timestamp { get; set; }
    public string Message { get; set; }

    public CommitNode(string sha)
    {
        Sha = sha;
    }
}

public class CommitGraph
{
    public Dictionary<string, CommitNode> Nodes { get; } = new();

    public void BuildFromLog(IEnumerable<(string sha, string[] parents, DateTime ts, string msg)> log)
    {
        foreach (var entry in log)
        {
            if (!Nodes.ContainsKey(entry.sha))
                Nodes[entry.sha] = new CommitNode(entry.sha)
                {
                    Timestamp = entry.ts,
                    Message = entry.msg
                };
        }
        foreach (var entry in log)
        {
            var node = Nodes[entry.sha];
            foreach (var p in entry.parents)
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

public class GraphLayout
{
    public List<CommitNode> RenderOrder { get; private set; }
    public Dictionary<CommitNode, int> CommitLane { get; private set; }
    public List<CommitNode?> Lanes { get; private set; }

    public void ComputeLayout(CommitGraph graph)
    {
        // Sort by timestamp ascending, but for commits at the same time, keep parents before children
        RenderOrder = TopologicalSort(graph);
        CommitLane = new Dictionary<CommitNode, int>();
        Lanes = [];

        foreach (var commit in RenderOrder)
        {
            int lane = FindLaneFor(commit);
            CommitLane[commit] = lane;
            if (lane >= Lanes.Count)
            {
                while (Lanes.Count <= lane)
                    Lanes.Add(null);
            }
            Lanes[lane] = commit;
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
        // Use an empty lane if possible
        for (int i = 0; i < Lanes.Count; i++)
        {
            if (Lanes[i] == null)
                return i;
        }
        Lanes.Add(null);
        return Lanes.Count - 1;
    }

    private List<CommitNode> TopologicalSort(CommitGraph graph)
    {
        var result = new List<CommitNode>();
        var visited = new HashSet<CommitNode>();
        void Visit(CommitNode node)
        {
            if (!visited.Add(node)) return;
            foreach (var parent in node.Parents)
                Visit(parent);
            result.Add(node);
        }
        foreach (var node in graph.Nodes.Values.OrderBy(n => n.Timestamp))
            Visit(node);
        result.Reverse(); // parents first, children after
        return result;
    }
}

// *         a1b2c3d  Initial commit
// | *       b2c3d4e  Add README
// | | *     c3d4e5f  Feature A work
// | * |     d4e5f6a  Start feature B
// | |/      e5f6a7b  Merge B into A
//
// *\        e5f6a7b  Merge B into A
// | *\|     d4e5f6a  Start feature B
//   | *\|   c3d4e5f  Feature A work
//     | *\  b2c3d4e  Add README
//       | * a1b2c3d  Initial commit
class Program
{
    static void Main(string[] args)
    {
        var log = new List<(string sha, string[] parents, DateTime ts, string msg)>
            {
                ("a1b2c3d", [],       new DateTime(2025, 1, 1), "Initial commit"),
                ("b2c3d4e", ["a1b2c3d"],  new DateTime(2025, 1, 2), "Add README"),
                ("c3d4e5f", ["b2c3d4e"],  new DateTime(2025, 1, 3), "Feature A work"),
                ("d4e5f6a", ["b2c3d4e"],  new DateTime(2025, 1, 3), "Start feature B"),
                ("e5f6a7b", ["c3d4e5f", "d4e5f6a"], new DateTime(2025, 1, 4), "Merge B into A"),
            };

        var graph = new CommitGraph();
        graph.BuildFromLog(log);

        var layout = new GraphLayout();
        layout.ComputeLayout(graph);

        PrintAsciiGraph(layout);
    }

    static void PrintAsciiGraph(GraphLayout layout)
    {
        var renderOrder = layout.RenderOrder;
        var commitLane = layout.CommitLane;
        int laneCount = layout.Lanes.Count;
        var nodeToIndex = new Dictionary<CommitNode, int>();
        for (int i = 0; i < renderOrder.Count; i++)
            nodeToIndex[renderOrder[i]] = i;

        // レーンの状態を維持
        var lanes = new CommitNode[laneCount];
        foreach (var commit in renderOrder)
        {
            int lane = commitLane[commit];

            // 描画用の1行分の文字配列
            char[] line = Enumerable.Repeat(' ', laneCount * 2 - 1).ToArray();

            // まず全レーンに | を仮置き
            for (int i = 0; i < laneCount; i++)
            {
                if (lanes[i] != null)
                    line[i * 2] = '|';
            }

            // 現コミットの位置に *
            line[lane * 2] = '*';

            // 分岐やマージの線を描く
            foreach (var parent in commit.Parents)
            {
                if (!commitLane.TryGetValue(parent, out int pLane))
                    continue;
                if (pLane == lane) continue; // 同じレーンは縦線でOK

                // 左側にマージ: /
                if (pLane < lane)
                    line[pLane * 2 + 1] = '/';
                // 右側にマージ: \
                else if (pLane > lane)
                    line[lane * 2 + 1] = '\\';
            }

            // SHAとメッセージ
            string sha7 = commit.Sha.Substring(0, 7);
            Console.WriteLine($"{new string(line)} {sha7}  {commit.Message}");

            // 現在の行でコミットしたレーンのみ更新
            for (int i = 0; i < laneCount; i++)
            {
                lanes[i] = null;
            }
            lanes[lane] = commit;
            // 複数親（マージ）の場合、親レーンにも線を残す
            foreach (var parent in commit.Parents)
            {
                if (commitLane.TryGetValue(parent, out int pLane))
                    lanes[pLane] = parent;
            }
        }
    }
}
