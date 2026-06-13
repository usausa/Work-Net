using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WorkDraw.Models;
using WorkDraw.Services;

namespace WorkDraw.Components.Pages;

public partial class Editor : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    internal DiagramState D { get; } = new();

    private ElementReference _svgRef;
    private ElementReference _labelInputRef;
    private DotNetObjectReference<Editor>? _selfRef;

    // キャンバスの画面上の位置（クライアント座標→ワールド座標変換に使用）
    private double _rectLeft, _rectTop, _rectW = 800, _rectH = 600;

    private enum Mode { None, DragNode, Connect, Pan, Resize }
    private Mode _mode = Mode.None;

    private string? _dragNodeId;
    private double _dragOffX, _dragOffY;
    private bool _moved; // ドラッグ/リサイズで実際に動いたか（Undo 記録用）

    private string? _connectFromId;
    private double _mouseWX, _mouseWY;
    private string? _hoverNodeId;

    private int _resizeHandle;
    private double _origX, _origY, _origW, _origH;

    private double _panClientX, _panClientY, _panViewX, _panViewY;

    private string? _dragStencilType;

    private string? _editingNodeId;
    private string _editingText = "";
    private bool _focusLabelInput;

    private string _statusMsg = "左のパレットからキャンバスへドラッグ＆ドロップで配置できます";

    internal static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>コンテナのラベルタグ幅の概算（CJK 12px・半角 6.8px）。</summary>
    internal static double TagWidth(string label) =>
        label.Sum(ch => ch > 0x7F ? 12.0 : 6.8) + 14;

    /// <summary>子コンポーネントのイベント後にエディタ全体を再描画させる。</summary>
    internal void Refresh() => StateHasChanged();

    // ---- DiagramNode から参照する表示状態 ----
    internal bool IsSelected(NodeModel n) => D.SelectedNodeId == n.Id;
    internal bool IsHovered(NodeModel n) => _hoverNodeId == n.Id;
    internal bool InConnectMode => _mode == Mode.Connect;
    internal bool IsConnectSource(NodeModel n) => _connectFromId == n.Id;
    internal bool ShowPorts(NodeModel n) =>
        _mode == Mode.None && (IsHovered(n) || IsSelected(n));
    internal bool ShowSelection(NodeModel n) =>
        IsSelected(n) && _mode != Mode.Connect;

    private string SceneTransform =>
        $"scale({F(D.Zoom)}) translate({F(-D.ViewX)} {F(-D.ViewY)})";

    // ---------- 初期化・破棄 ----------

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _selfRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("workdraw.initKeyboard", _selfRef);
            await RefreshRectAsync();
        }
        if (_focusLabelInput)
        {
            _focusLabelInput = false;
            await JS.InvokeVoidAsync("workdraw.focusElement", _labelInputRef);
        }
    }

    public async ValueTask DisposeAsync()
    {
        // _selfRef が null = プリレンダー段階（インタラクティブ初期化前）。JS は呼べない。
        if (_selfRef is null) return;
        try
        {
            await JS.InvokeVoidAsync("workdraw.disposeKeyboard");
        }
        catch (JSDisconnectedException) { }
        catch (InvalidOperationException) { }
        _selfRef.Dispose();
    }

    private sealed record Rect(double Left, double Top, double Width, double Height);

    private async Task RefreshRectAsync()
    {
        var r = await JS.InvokeAsync<Rect>("workdraw.getRect", _svgRef);
        (_rectLeft, _rectTop, _rectW, _rectH) = (r.Left, r.Top, r.Width, r.Height);
    }

    private (double X, double Y) ToWorld(double clientX, double clientY)
        => D.ToWorld(clientX, clientY, _rectLeft, _rectTop);

    // ---------- パレット ----------

    private void OnPaletteDragStart(StencilDef s) => _dragStencilType = s.Type;

    private void AddAtCenter(StencilDef s)
    {
        var cx = D.ViewX + _rectW / 2 / D.Zoom;
        var cy = D.ViewY + _rectH / 2 / D.Zoom;
        D.AddNode(s, cx, cy);
        _statusMsg = $"{s.Name} を配置しました";
    }

    private async Task OnCanvasDrop(DragEventArgs e)
    {
        if (_dragStencilType is null) return;
        var def = StencilCatalog.Find(_dragStencilType);
        _dragStencilType = null;
        if (def is null) return;
        await RefreshRectAsync();
        var (wx, wy) = ToWorld(e.ClientX, e.ClientY);
        D.AddNode(def, wx, wy);
        _statusMsg = $"{def.Name} を配置しました";
    }

    // ---------- キャンバス（空白部分）----------

    private async Task OnCanvasMouseDown(MouseEventArgs e)
    {
        await RefreshRectAsync();
        if (_editingNodeId is not null) CommitLabel();

        // 空白を押下: 選択解除してパン開始（左 or 中ボタン）
        D.SelectedNodeId = null;
        D.SelectedEdgeId = null;
        if (e.Button == 0 || e.Button == 1)
        {
            _mode = Mode.Pan;
            (_panClientX, _panClientY) = (e.ClientX, e.ClientY);
            (_panViewX, _panViewY) = (D.ViewX, D.ViewY);
        }
    }

    private void OnCanvasMouseMove(MouseEventArgs e)
    {
        (_mouseWX, _mouseWY) = ToWorld(e.ClientX, e.ClientY);

        // キャンバス外でボタンを離した場合のドラッグ取り残しを解消する
        if (_mode != Mode.None && e.Buttons == 0)
        {
            OnCanvasMouseUp(e);
            return;
        }

        switch (_mode)
        {
            case Mode.DragNode when D.Nodes.FirstOrDefault(n => n.Id == _dragNodeId) is { } node:
                if (!_moved) { D.PushUndo(); _moved = true; }
                node.X = D.Snap(_mouseWX - _dragOffX);
                node.Y = D.Snap(_mouseWY - _dragOffY);
                foreach (var (child, dx, dy) in _dragChildren)
                {
                    child.X = node.X + dx;
                    child.Y = node.Y + dy;
                }
                break;

            case Mode.Resize when D.Nodes.FirstOrDefault(n => n.Id == _dragNodeId) is { } rn:
                if (!_moved) { D.PushUndo(); _moved = true; }
                ApplyResize(rn);
                break;

            case Mode.Pan:
                D.ViewX = _panViewX - (e.ClientX - _panClientX) / D.Zoom;
                D.ViewY = _panViewY - (e.ClientY - _panClientY) / D.Zoom;
                break;
        }
    }

    private void OnCanvasMouseUp(MouseEventArgs e)
    {
        if (_mode == Mode.Connect && _connectFromId is not null
            && _hoverNodeId is not null && _hoverNodeId != _connectFromId)
        {
            D.Connect(_connectFromId, _hoverNodeId);
            _statusMsg = "接続しました";
        }
        _mode = Mode.None;
        _dragNodeId = null;
        _connectFromId = null;
    }

    private void OnWheel(WheelEventArgs e)
    {
        var (wx, wy) = ToWorld(e.ClientX, e.ClientY);
        D.ZoomAt(e.DeltaY < 0 ? 1.1 : 1 / 1.1, wx, wy);
    }

    // ---------- ノード ----------

    private readonly List<(NodeModel Child, double Dx, double Dy)> _dragChildren = new();

    internal async Task OnNodeMouseDown(NodeModel n, MouseEventArgs e)
    {
        if (e.Button != 0) return;
        await RefreshRectAsync();
        if (_editingNodeId is not null && _editingNodeId != n.Id) CommitLabel();

        D.SelectedNodeId = n.Id;
        D.SelectedEdgeId = null;
        _mode = Mode.DragNode;
        _dragNodeId = n.Id;
        _moved = false;
        var (wx, wy) = ToWorld(e.ClientX, e.ClientY);
        (_dragOffX, _dragOffY) = (wx - n.X, wy - n.Y);

        // コンテナをドラッグするときは、中に乗っているノードも一緒に動かす
        _dragChildren.Clear();
        if (n.IsContainer)
        {
            foreach (var c in D.Nodes.Where(c => c.Id != n.Id
                         && c.Cx >= n.X && c.Cx <= n.X + n.W
                         && c.Cy >= n.Y && c.Cy <= n.Y + n.H
                         && !(c.IsContainer && c.W * c.H >= n.W * n.H)))
            {
                _dragChildren.Add((c, c.X - n.X, c.Y - n.Y));
            }
        }
    }

    internal void OnNodeOver(NodeModel n) => _hoverNodeId = n.Id;

    internal void OnNodeOut(NodeModel n)
    {
        if (_hoverNodeId == n.Id) _hoverNodeId = null;
    }

    internal void StartLabelEdit(NodeModel n)
    {
        _mode = Mode.None;
        _editingNodeId = n.Id;
        _editingText = n.Label;
        _focusLabelInput = true;
    }

    private void CommitLabel()
    {
        var node = D.Nodes.FirstOrDefault(n => n.Id == _editingNodeId);
        _editingNodeId = null;
        if (node is null || node.Label == _editingText) return;
        D.PushUndo();
        node.Label = _editingText;
    }

    private void CancelLabel() => _editingNodeId = null;

    private void OnLabelKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") CommitLabel();
        else if (e.Key == "Escape") CancelLabel();
    }

    // ---------- 接続ポート ----------

    internal async Task OnPortMouseDown(NodeModel n, MouseEventArgs e)
    {
        if (e.Button != 0) return;
        await RefreshRectAsync();
        _mode = Mode.Connect;
        _connectFromId = n.Id;
        (_mouseWX, _mouseWY) = ToWorld(e.ClientX, e.ClientY);
        _statusMsg = "接続先のノードの上でマウスを離してください";
    }

    // ---------- リサイズ ----------

    internal async Task OnResizeHandleMouseDown(NodeModel n, int handle, MouseEventArgs e)
    {
        if (e.Button != 0) return;
        await RefreshRectAsync();
        _mode = Mode.Resize;
        _dragNodeId = n.Id;
        _resizeHandle = handle;
        _moved = false;
        (_origX, _origY, _origW, _origH) = (n.X, n.Y, n.W, n.H);
    }

    private void ApplyResize(NodeModel n)
    {
        // ハンドル 0=左上 1=右上 2=右下 3=左下。対角が固定点。
        var ax = _resizeHandle is 1 or 2 ? _origX : _origX + _origW;
        var ay = _resizeHandle is 2 or 3 ? _origY : _origY + _origH;
        var sx = D.Snap(_mouseWX);
        var sy = D.Snap(_mouseWY);
        var minW = n.IsContainer ? 120 : 40;
        var minH = n.IsContainer ? 80 : 40;

        n.W = Math.Max(minW, Math.Abs(ax - sx));
        n.X = sx < ax ? ax - n.W : ax;
        n.H = Math.Max(minH, Math.Abs(ay - sy));
        n.Y = sy < ay ? ay - n.H : ay;
    }

    internal static string HandleCursor(int handle) =>
        handle is 0 or 2 ? "nwse-resize" : "nesw-resize";

    // ---------- エッジ ----------

    private void OnEdgeMouseDown(EdgeModel ed, MouseEventArgs e)
    {
        if (e.Button != 0) return;
        D.SelectedEdgeId = ed.Id;
        D.SelectedNodeId = null;
    }

    // ---------- キーボード（JS から呼び出し）----------

    [JSInvokable]
    public Task OnGlobalKeyDown(string key, bool ctrl, bool shift) => InvokeAsync(() =>
    {
        if (_editingNodeId is not null) return;
        switch (key)
        {
            case "Delete" or "Backspace":
                D.DeleteSelected();
                break;
            case "Escape":
                _mode = Mode.None;
                _connectFromId = null;
                D.SelectedNodeId = null;
                D.SelectedEdgeId = null;
                break;
            case "z" or "Z" when ctrl && shift:
            case "y" or "Y" when ctrl:
                D.Redo();
                break;
            case "z" or "Z" when ctrl:
                D.Undo();
                break;
        }
        StateHasChanged();
    });

    // ---------- ツールバー ----------

    private void NewDiagram()
    {
        D.Clear();
        _statusMsg = "新規作成しました（Ctrl+Z で元に戻せます）";
    }

    private async Task SaveJson()
    {
        await JS.InvokeVoidAsync("workdraw.downloadText", "diagram.json", D.Serialize(), "application/json");
        _statusMsg = "diagram.json をダウンロードしました";
    }

    private async Task OnLoadFile(InputFileChangeEventArgs e)
    {
        using var reader = new StreamReader(e.File.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024));
        var json = await reader.ReadToEndAsync();
        D.PushUndo();
        _statusMsg = D.LoadJson(json)
            ? $"{e.File.Name} を読み込みました"
            : "読み込みに失敗しました（JSON 形式が不正です）";
    }

    private async Task ExportPng()
    {
        var bounds = ContentBounds();
        if (bounds is null)
        {
            _statusMsg = "書き出す図形がありません";
            return;
        }
        var (x, y, w, h) = bounds.Value;
        await JS.InvokeVoidAsync("workdraw.exportPng", _svgRef, "diagram.png", x, y, w, h, 2);
        _statusMsg = "diagram.png をダウンロードしました";
    }

    private (double X, double Y, double W, double H)? ContentBounds()
    {
        if (D.Nodes.Count == 0) return null;
        var x1 = D.Nodes.Min(n => n.X) - 24;
        var y1 = D.Nodes.Min(n => n.Y) - 24;
        var x2 = D.Nodes.Max(n => n.X + n.W) + 24;
        var y2 = D.Nodes.Max(n => n.Y + n.H) + 40; // ラベル分の余白
        return (x1, y1, x2 - x1, y2 - y1);
    }

    private void ZoomIn() => ZoomAtViewportCenter(1.2);
    private void ZoomOut() => ZoomAtViewportCenter(1 / 1.2);

    private void ZoomAtViewportCenter(double factor)
    {
        var cx = D.ViewX + _rectW / 2 / D.Zoom;
        var cy = D.ViewY + _rectH / 2 / D.Zoom;
        D.ZoomAt(factor, cx, cy);
    }

    private void ZoomReset()
    {
        D.Zoom = 1;
    }

    private void ZoomFit()
    {
        var bounds = ContentBounds();
        if (bounds is null) return;
        var (x, y, w, h) = bounds.Value;
        D.Zoom = Math.Clamp(Math.Min(_rectW / w, _rectH / h) * 0.95, 0.2, 2.0);
        D.ViewX = x - (_rectW / D.Zoom - w) / 2;
        D.ViewY = y - (_rectH / D.Zoom - h) / 2;
    }

    // ---------- サンプル図 ----------

    private void LoadSample()
    {
        var nodes = new List<NodeModel>
        {
            new() { Id = "vpc",  Type = "vpc",  Label = "VPC 10.0.0.0/16", X = 320, Y = 60,  W = 620, H = 460, IsContainer = true },
            new() { Id = "pub",  Type = "subnet-public",  Label = "パブリックサブネット",   X = 460, Y = 110, W = 200, H = 170, IsContainer = true },
            new() { Id = "pri",  Type = "subnet-private", Label = "プライベートサブネット", X = 700, Y = 110, W = 200, H = 170, IsContainer = true },
            new() { Id = "db",   Type = "subnet-private", Label = "DB サブネット",          X = 700, Y = 320, W = 200, H = 170, IsContainer = true },
            new() { Id = "user", Type = "user",       Label = "ユーザー",    X = 40,  Y = 240 },
            new() { Id = "r53",  Type = "route53",    Label = "Route 53",   X = 170, Y = 110 },
            new() { Id = "cf",   Type = "cloudfront", Label = "CloudFront", X = 170, Y = 240 },
            new() { Id = "s3",   Type = "s3",         Label = "S3 静的サイト", X = 170, Y = 390 },
            new() { Id = "igw",  Type = "igw",        Label = "IGW",        X = 350, Y = 240 },
            new() { Id = "alb",  Type = "alb",        Label = "ALB",        X = 524, Y = 160 },
            new() { Id = "ec2",  Type = "ec2",        Label = "EC2 (App)",  X = 764, Y = 160 },
            new() { Id = "rds",  Type = "rds",        Label = "RDS",        X = 764, Y = 370 },
        };
        foreach (var n in nodes.Where(x => !x.IsContainer)) { n.W = 72; n.H = 72; }

        var edges = new List<EdgeModel>
        {
            new() { SourceId = "user", TargetId = "r53" },
            new() { SourceId = "user", TargetId = "cf" },
            new() { SourceId = "r53",  TargetId = "cf" },
            new() { SourceId = "cf",   TargetId = "s3" },
            new() { SourceId = "cf",   TargetId = "igw" },
            new() { SourceId = "igw",  TargetId = "alb" },
            new() { SourceId = "alb",  TargetId = "ec2" },
            new() { SourceId = "ec2",  TargetId = "rds" },
        };

        D.ReplaceAll(nodes, edges);
        ZoomFit();
        _statusMsg = "サンプル構成図を読み込みました";
    }
}
