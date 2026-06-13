using System.Text.Json;
using WorkDraw.Models;

namespace WorkDraw.Services;

/// <summary>1つの図面の編集状態。Editor コンポーネントが所有する。</summary>
public class DiagramState
{
    public List<NodeModel> Nodes { get; private set; } = new();
    public List<EdgeModel> Edges { get; private set; } = new();

    public string? SelectedNodeId { get; set; }
    public string? SelectedEdgeId { get; set; }

    public bool SnapEnabled { get; set; } = true;
    public bool GridVisible { get; set; } = true;
    public int GridSize { get; set; } = 20;

    public double Zoom { get; set; } = 1.0;
    public double ViewX { get; set; }
    public double ViewY { get; set; }

    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public NodeModel? SelectedNode => Nodes.FirstOrDefault(n => n.Id == SelectedNodeId);

    public double Snap(double v) => SnapEnabled ? Math.Round(v / GridSize) * GridSize : v;

    // ---- 履歴 ----
    public void PushUndo()
    {
        _undo.Push(Serialize());
        if (_undo.Count > 50)
        {
            // 新しい方から 50 件だけ残す
            var keep = _undo.Take(50).Reverse().ToList();
            _undo.Clear();
            foreach (var s in keep) _undo.Push(s);
        }
        _redo.Clear();
    }

    public void Undo()
    {
        if (_undo.Count == 0) return;
        _redo.Push(Serialize());
        LoadJson(_undo.Pop());
    }

    public void Redo()
    {
        if (_redo.Count == 0) return;
        _undo.Push(Serialize());
        LoadJson(_redo.Pop());
    }

    // ---- ノード操作 ----
    public NodeModel AddNode(StencilDef def, double x, double y)
    {
        PushUndo();
        var node = new NodeModel
        {
            Type = def.Type,
            Label = def.Name,
            W = def.W,
            H = def.H,
            IsContainer = def.IsContainer,
            X = Snap(x - def.W / 2),
            Y = Snap(y - def.H / 2),
        };
        Nodes.Add(node);
        SelectedNodeId = node.Id;
        SelectedEdgeId = null;
        return node;
    }

    public void DeleteSelected()
    {
        if (SelectedNodeId is not null)
        {
            PushUndo();
            Nodes.RemoveAll(n => n.Id == SelectedNodeId);
            Edges.RemoveAll(e => e.SourceId == SelectedNodeId || e.TargetId == SelectedNodeId);
            SelectedNodeId = null;
        }
        else if (SelectedEdgeId is not null)
        {
            PushUndo();
            Edges.RemoveAll(e => e.Id == SelectedEdgeId);
            SelectedEdgeId = null;
        }
    }

    public void Connect(string sourceId, string targetId)
    {
        if (sourceId == targetId) return;
        if (Edges.Any(e => e.SourceId == sourceId && e.TargetId == targetId)) return;
        PushUndo();
        var edge = new EdgeModel { SourceId = sourceId, TargetId = targetId };
        Edges.Add(edge);
        SelectedEdgeId = edge.Id;
        SelectedNodeId = null;
    }

    public void Clear()
    {
        PushUndo();
        Nodes.Clear();
        Edges.Clear();
        SelectedNodeId = null;
        SelectedEdgeId = null;
    }

    public void ReplaceAll(List<NodeModel> nodes, List<EdgeModel> edges)
    {
        PushUndo();
        Nodes = nodes;
        Edges = edges;
        SelectedNodeId = null;
        SelectedEdgeId = null;
    }

    // ---- 座標変換（画面px → ワールド座標）----
    public (double X, double Y) ToWorld(double clientX, double clientY, double rectLeft, double rectTop)
        => (ViewX + (clientX - rectLeft) / Zoom, ViewY + (clientY - rectTop) / Zoom);

    public void ZoomAt(double factor, double worldX, double worldY)
    {
        var newZoom = Math.Clamp(Zoom * factor, 0.2, 4.0);
        if (Math.Abs(newZoom - Zoom) < 0.0001) return;
        // ズーム中心のワールド座標が画面上で動かないように視点を補正する
        ViewX = worldX - (worldX - ViewX) * Zoom / newZoom;
        ViewY = worldY - (worldY - ViewY) * Zoom / newZoom;
        Zoom = newZoom;
    }

    // ---- エッジの直交ルーティング（フローティング接続）----
    public static string RouteEdge(NodeModel s, NodeModel t)
    {
        var pts = RoutePoints(s, t);
        return "M " + string.Join(" L ", pts.Select(p => $"{p.X:0.##} {p.Y:0.##}"));
    }

    public static (double X, double Y) EdgeMidpoint(NodeModel s, NodeModel t)
    {
        var pts = RoutePoints(s, t);
        var i = pts.Count / 2;
        return ((pts[i - 1].X + pts[i].X) / 2, (pts[i - 1].Y + pts[i].Y) / 2);
    }

    private static List<(double X, double Y)> RoutePoints(NodeModel s, NodeModel t)
    {
        const double stub = 16;
        var dx = t.Cx - s.Cx;
        var dy = t.Cy - s.Cy;
        var pts = new List<(double X, double Y)>();

        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            // 水平方向優先: 左右の辺から出入りする
            var sx = dx >= 0 ? s.X + s.W : s.X;
            var tx = dx >= 0 ? t.X : t.X + t.W;
            var s1x = sx + (dx >= 0 ? stub : -stub);
            var t1x = tx - (dx >= 0 ? stub : -stub);
            var midX = (s1x + t1x) / 2;
            pts.Add((sx, s.Cy));
            pts.Add((midX, s.Cy));
            pts.Add((midX, t.Cy));
            pts.Add((tx, t.Cy));
        }
        else
        {
            var sy = dy >= 0 ? s.Y + s.H : s.Y;
            var ty = dy >= 0 ? t.Y : t.Y + t.H;
            var s1y = sy + (dy >= 0 ? stub : -stub);
            var t1y = ty - (dy >= 0 ? stub : -stub);
            var midY = (s1y + t1y) / 2;
            pts.Add((s.Cx, sy));
            pts.Add((s.Cx, midY));
            pts.Add((t.Cx, midY));
            pts.Add((t.Cx, ty));
        }
        return pts;
    }

    // ---- 描画順: コンテナ（面積の大きい順）→ 通常ノード ----
    public IEnumerable<NodeModel> NodesInRenderOrder =>
        Nodes.Where(n => n.IsContainer).OrderByDescending(n => n.W * n.H)
             .Concat(Nodes.Where(n => !n.IsContainer));

    // ---- 永続化 ----
    public string Serialize() =>
        JsonSerializer.Serialize(new DiagramDocument { Nodes = Nodes, Edges = Edges }, JsonOpts);

    public bool LoadJson(string json)
    {
        try
        {
            var doc = JsonSerializer.Deserialize<DiagramDocument>(json);
            if (doc is null) return false;
            Nodes = doc.Nodes;
            Edges = doc.Edges;
            SelectedNodeId = null;
            SelectedEdgeId = null;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
