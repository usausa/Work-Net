using System.Globalization;
using System.Windows;
using System.Windows.Input;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using Trader.Data;

namespace Trader.Charting;

public enum ChartStyle { Candles, Line, Area }

// TradingView 風のインタラクティブチャート。
// WPF のビジュアルツリーは使わず、OnPaintSurface で毎フレーム全体を描き直す即時モード描画。
// パン・ズーム・クロスヘア・オートスケールもすべて自前で計算している。
public sealed class ChartControl : SKElement
{
    // ---- 表示対象のデータと、そこから導出した値のキャッシュ ----
    MarketSeries? _series;
    string _tfLabel = "";
    long _seenVersion = -1;   // 計算済みの Version。系列が更新されたときだけ SMA/EMA を引き直す
    double[] _sma = [];
    double[] _ema = [];

    // ---- ビューポート ----
    // X 軸は「ローソク足のインデックス」を単位とする連続値の空間で扱う。
    // 価格や時刻ではなくインデックス空間にすることで、時間足が変わっても
    // パン・ズームの計算をそのまま使い回せる。
    double _firstIndex;         // 左端に来る足のインデックス (小数を取りうる)
    double _visible = 160;      // 画面に収める足の本数 = ズーム倍率
    bool _followRight = true;   // 右端追従モード。足が増えたら自動でスクロールして最新を映す
    const double RightPad = 6;  // 追従時に最新足の右へ空ける余白 (本数)

    // ---- 入力状態 ----
    Point? _mouse;      // null ならプロット外。クロスヘアの表示可否を兼ねる
    bool _dragging;
    Point _dragStart;   // ドラッグ開始時のカーソル位置と _firstIndex を控えておき、差分でスクロールする
    double _dragFirst;

    // ---- 表示オプション (ツールバーから設定される) ----
    public ChartStyle Mode { get; set; } = ChartStyle.Candles;
    public bool ShowSma { get; set; } = true;
    public bool ShowEma { get; set; } = true;
    public bool ShowVolume { get; set; } = true;

    // 軸が占める領域。プロット領域はコントロールからこれを除いた残り。
    const float AxisW = 74f;   // 右の価格軸
    const float AxisH = 26f;   // 下の時間軸

    // SkiaSharp 3.x では文字描画が SKFont ベースになったため、書体とサイズの組を使い回す
    static readonly SKTypeface FaceReg = SKTypeface.FromFamilyName("Segoe UI");
    static readonly SKTypeface FaceBold = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold);
    readonly SKFont _fAxis = new(FaceReg, 11f);
    readonly SKFont _fText = new(FaceReg, 12f);
    readonly SKFont _fBold = new(FaceBold, 13f);
    readonly SKFont _fWater = new(FaceBold, 68f);

    // 毎フレーム全要素を描き直すので、Paint は生成せず色だけ差し替えて使い回す
    readonly SKPaint _pFill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    readonly SKPaint _pStroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
    readonly SKPaint _pDash = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };

    public ChartControl()
    {
        Cursor = Cursors.Cross;
        _pDash.PathEffect = SKPathEffect.CreateDash([4f, 4f], 0);   // クロスヘアと現在値ラインの破線
    }

    // ---------------- 公開 API ----------------

    // 銘柄・時間足の切り替え。データが総入れ替えになるのでインジケーターのキャッシュも捨てる。
    public void SetSeries(MarketSeries series, string tfLabel)
    {
        _series = series;
        _tfLabel = tfLabel;
        _seenVersion = -1;
        FitToLatest();
    }

    // 直近 160 本を表示し、右端追従モードに戻す。
    public void FitToLatest()
    {
        _visible = 160;
        _followRight = true;
        InvalidateVisual();
    }

    public void Refresh() => InvalidateVisual();

    // ---------------- 入力処理 ----------------
    // マウス操作でビューポート (_firstIndex / _visible) を書き換え、再描画を要求する。
    // 実際の座標変換は描画時に行うので、ここではインデックス空間の値だけを更新する。

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        // ダブルクリックは表示リセット (ドラッグ開始にはしない)
        if (e.ClickCount == 2) { FitToLatest(); return; }

        _dragging = true;
        _dragStart = e.GetPosition(this);
        _dragFirst = _firstIndex;
        CaptureMouse();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        _dragging = false;
        ReleaseMouseCapture();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var p = e.GetPosition(this);
        _mouse = p;

        // ドラッグ中: カーソルの移動量(px)を足の本数に換算して左端をずらす。
        // 累積ではなく開始位置からの差分で求めるので、誤差が溜まらない。
        if (_dragging && _series != null)
        {
            double plotW = Math.Max(1, ActualWidth - AxisW);
            double xPer = plotW / _visible;
            _firstIndex = _dragFirst - (p.X - _dragStart.X) / xPer;
            ClampView();
            UpdateFollow();
        }

        InvalidateVisual();   // クロスヘア追従のため、ドラッグしていなくても描き直す
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _mouse = null;
        InvalidateVisual();
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (_series == null) return;

        // カーソル下のインデックスを不動点にしてズームする。
        // 先に基準インデックスを求め、_visible を変えた後に同じ画面位置へ戻るよう左端を再計算する。
        var p = e.GetPosition(this);
        double plotW = Math.Max(1, ActualWidth - AxisW);
        double anchor = _firstIndex + p.X / (plotW / _visible);

        _visible = Math.Clamp(_visible * Math.Pow(1.0012, -e.Delta), 8, 4000);
        _firstIndex = anchor - p.X * _visible / plotW;

        ClampView();
        UpdateFollow();
        InvalidateVisual();
    }

    // データが完全に画面外へ出てしまわないよう、最低でも keep 本は残るように左端を制限する
    void ClampView()
    {
        if (_series == null) return;
        int n = _series.Candles.Count;
        double keep = Math.Min(10, _visible / 2);
        _firstIndex = Math.Clamp(_firstIndex, -_visible + keep, n - keep);
    }

    // 右端(最新足＋余白)まで戻したら追従モードに復帰し、そうでなければ解除する
    void UpdateFollow()
    {
        if (_series == null) return;
        _followRight = _firstIndex + _visible >= _series.Candles.Count + RightPad - 1;
    }

    // ---------------- 描画 ----------------

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);
        var canvas = e.Surface.Canvas;
        canvas.Clear(Theme.Bg);

        var s = _series;
        if (s == null || s.Candles.Count == 0 || ActualWidth < 80 || ActualHeight < 80) return;

        // サーフェスは物理ピクセルだが、以降は WPF の DIP 座標系で描く。
        // こうしておくとマウス座標(DIP)をそのまま使え、高DPI環境でも座標変換が不要になる。
        canvas.Scale((float)(e.Info.Width / ActualWidth));
        float W = (float)ActualWidth, H = (float)ActualHeight;
        var plot = new SKRect(0, 0, W - AxisW, H - AxisH);

        var candles = s.Candles;
        int n = candles.Count;
        int dec = s.Info.Decimals;

        // インジケーターは系列が更新されたときだけ再計算する (ティックごとの全系列走査を避ける)
        if (s.Version != _seenVersion)
        {
            _seenVersion = s.Version;
            _sma = Indicators.Sma(candles, 20);
            _ema = Indicators.Ema(candles, 50);
        }

        // 追従モードなら、増えた足のぶん左端を送って常に最新が右端に来るようにする
        if (_followRight) _firstIndex = n - _visible + RightPad;
        ClampView();

        // ---- 可視範囲の確定 ----
        // xPer は足1本あたりの幅。i0..i1 が実データとして存在する描画対象の範囲。
        float xPer = plot.Width / (float)_visible;
        int i0 = Math.Max(0, (int)Math.Floor(_firstIndex));
        int i1 = Math.Min(n - 1, (int)Math.Ceiling(_firstIndex + _visible));
        if (i1 < i0) return;

        // ---- 価格レンジのオートスケール ----
        // 可視範囲の高安に加え、表示中のインジケーターも収まるように広げる。
        double minP = double.MaxValue, maxP = double.MinValue;
        for (int i = i0; i <= i1; i++)
        {
            var c = candles[i];
            if (c.Low < minP) minP = c.Low;
            if (c.High > maxP) maxP = c.High;
        }
        if (ShowSma) AccRange(_sma, i0, i1, ref minP, ref maxP);
        if (ShowEma) AccRange(_ema, i0, i1, ref minP, ref maxP);

        double range = maxP - minP;
        if (range <= 0) range = Math.Max(1e-9, Math.Abs(maxP) * 0.01);   // 全て同値のときの0除算回避
        // 下側の余白を厚く取るのは、出来高バーを重ねる領域を確保するため
        maxP += range * 0.06;
        minP -= range * (ShowVolume ? 0.30 : 0.08);
        range = maxP - minP;

        // ---- 座標変換 ----
        // X はインデックス→画面位置 (足の中心)、Y は価格→画面位置。PriceAt はその逆変換。
        float X(double i) => plot.Left + (float)((i - _firstIndex) * xPer) + xPer * 0.5f;
        float Y(double p) => plot.Top + (float)((maxP - p) / range) * plot.Height;
        double PriceAt(float y) => maxP - (y - plot.Top) / plot.Height * range;

        // ---- 価格グリッド + 右軸ラベル ----
        // 刻み幅から必要な小数桁を逆算し、ズーム率が変わっても桁が過不足なく出るようにする。
        double step = NiceStep(range, Math.Max(3, (int)(plot.Height / 80)));
        int gridDec = Math.Max(0, -(int)Math.Floor(Math.Log10(step) + 1e-9));
        for (double p = Math.Ceiling(minP / step) * step; p <= maxP; p += step)
        {
            float y = Y(p);
            _pStroke.Color = Theme.Grid;
            canvas.DrawLine(plot.Left, y, plot.Right, y, _pStroke);
            _pFill.Color = Theme.AxisText;
            canvas.DrawText(p.ToString("N" + gridDec, CultureInfo.InvariantCulture),
                plot.Right + 8, y + 4, SKTextAlign.Left, _fAxis, _pFill);
        }

        // ---- 時間グリッド + 下軸ラベル ----
        {
            // ラベルが重ならない間隔になるまで 1→2→5→10→20→50... と刻みを粗くしていく
            int[] ladder = [1, 2, 5];
            int tStep = 1, li = 0, pow = 1;
            while (tStep * xPer < 90) { li++; if (li == 3) { li = 0; pow *= 10; } tStep = ladder[li] * pow; }

            for (int i = Math.Max(0, (i0 + tStep - 1) / tStep * tStep); i <= i1; i += tStep)
            {
                float x = X(i);
                _pStroke.Color = Theme.Grid;
                canvas.DrawLine(x, plot.Top, x, plot.Bottom, _pStroke);

                // 日付や月が変わる区切りは上位の単位を明るい色で出し、時間軸の見当をつけやすくする
                var t = candles[i].Time;
                bool major;
                string label;
                if (s.Timeframe >= TimeSpan.FromDays(1))
                {
                    major = t.Day == 1;
                    label = t.ToString(major ? "yyyy/MM" : "MM/dd", CultureInfo.InvariantCulture);
                }
                else
                {
                    major = t.TimeOfDay == TimeSpan.Zero;
                    label = t.ToString(major ? "MM/dd" : "HH:mm", CultureInfo.InvariantCulture);
                }
                _pFill.Color = major ? Theme.TextBright : Theme.AxisText;
                canvas.DrawText(label, x, plot.Bottom + 17, SKTextAlign.Center, _fAxis, _pFill);
            }
        }

        // ---- 銘柄名のウォーターマーク ----
        _pFill.Color = Theme.Watermark;
        canvas.DrawText(s.Info.Name, plot.MidX, plot.MidY + 24, SKTextAlign.Center, _fWater, _pFill);

        // ==== ここから系列本体。軸へはみ出さないようプロット領域でクリップする ====
        canvas.Save();
        canvas.ClipRect(plot);

        // ---- 出来高バー ----
        // 価格軸とはスケールが無関係なので、可視範囲の最大出来高を高さ 100% として
        // プロット下部の一定割合に独立して描く。
        if (ShowVolume)
        {
            double maxV = 0;
            for (int i = i0; i <= i1; i++) if (candles[i].Volume > maxV) maxV = candles[i].Volume;
            if (maxV > 0)
            {
                float volH = plot.Height * 0.22f;
                float bw = Math.Max(1f, xPer * 0.7f);
                for (int i = i0; i <= i1; i++)
                {
                    var c = candles[i];
                    float h = (float)(c.Volume / maxV) * volH;
                    _pFill.Color = c.IsUp ? Theme.UpDim : Theme.DownDim;
                    canvas.DrawRect(X(i) - bw / 2, plot.Bottom - h, bw, h, _pFill);
                }
            }
        }

        // ---- 価格系列本体 ----
        if (Mode == ChartStyle.Candles)
        {
            float bw = Math.Max(1f, xPer * 0.72f);
            bool thin = bw <= 1.6f;   // 実体がヒゲと同じ太さになる縮尺では実体を省いて塗り潰れを防ぐ
            float wick = Math.Clamp(xPer * 0.08f, 1f, 1.5f);
            for (int i = i0; i <= i1; i++)
            {
                var c = candles[i];
                float x = X(i);
                var col = c.IsUp ? Theme.Up : Theme.Down;

                // ヒゲ (高値〜安値)
                _pStroke.Color = col;
                _pStroke.StrokeWidth = wick;
                canvas.DrawLine(x, Y(c.High), x, Y(c.Low), _pStroke);

                // 実体 (始値〜終値)。同値のときも消えないよう最低 1px は確保する
                if (!thin)
                {
                    float yo = Y(c.Open), yc = Y(c.Close);
                    float top = Math.Min(yo, yc);
                    float h = Math.Max(1f, Math.Abs(yo - yc));
                    _pFill.Color = col;
                    canvas.DrawRect(x - bw / 2, top, bw, h, _pFill);
                }
            }
            _pStroke.StrokeWidth = 1;   // 使い回している Paint を既定値に戻す
        }
        else
        {
            // ライン/エリアは終値を結んだ1本のパス
            using var path = new SKPath();
            path.MoveTo(X(i0), Y(candles[i0].Close));
            for (int i = i0 + 1; i <= i1; i++) path.LineTo(X(i), Y(candles[i].Close));

            // エリアは同じパスを下端で閉じ、上から下へ消えるグラデーションで塗る
            if (Mode == ChartStyle.Area)
            {
                using var fill = new SKPath(path);
                fill.LineTo(X(i1), plot.Bottom);
                fill.LineTo(X(i0), plot.Bottom);
                fill.Close();
                using var shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, plot.Top), new SKPoint(0, plot.Bottom),
                    [Theme.Line.WithAlpha(80), Theme.Line.WithAlpha(0)], null, SKShaderTileMode.Clamp);
                _pFill.Shader = shader;
                _pFill.Color = SKColors.White;   // Shader は Color のアルファと乗算されるため不透明にしておく
                canvas.DrawPath(fill, _pFill);
                _pFill.Shader = null;
            }

            _pStroke.Color = Theme.Line;
            _pStroke.StrokeWidth = 2;
            canvas.DrawPath(path, _pStroke);
            _pStroke.StrokeWidth = 1;
        }

        // ---- インジケーターの線 ----
        // ウォームアップ区間の NaN では線を繋がず、そこで一度パスを切る。
        void DrawIndicator(double[] arr, SKColor color)
        {
            using var path = new SKPath();
            bool started = false;
            for (int i = i0; i <= i1 && i < arr.Length; i++)
            {
                if (double.IsNaN(arr[i])) { started = false; continue; }
                if (!started) { path.MoveTo(X(i), Y(arr[i])); started = true; }
                else path.LineTo(X(i), Y(arr[i]));
            }
            _pStroke.Color = color;
            _pStroke.StrokeWidth = 1.6f;
            canvas.DrawPath(path, _pStroke);
            _pStroke.StrokeWidth = 1;
        }
        if (ShowSma) DrawIndicator(_sma, Theme.Sma);
        if (ShowEma) DrawIndicator(_ema, Theme.Ema);

        // ---- 現在値の水平ライン ----
        var lastC = candles[n - 1];
        var lastCol = lastC.IsUp ? Theme.Up : Theme.Down;
        float lastY = Y(lastC.Close);
        _pDash.Color = lastCol.WithAlpha(180);
        canvas.DrawLine(plot.Left, lastY, plot.Right, lastY, _pDash);

        canvas.Restore();
        // ==== ここから軸まわりのオーバーレイ (クリップ解除後) ====

        // ---- 軸の区切り線 ----
        _pStroke.Color = Theme.Border;
        canvas.DrawLine(plot.Right, 0, plot.Right, H, _pStroke);
        canvas.DrawLine(0, plot.Bottom, W, plot.Bottom, _pStroke);

        // 価格軸に載せるラベルチップ (現在値とクロスヘアで共通)
        void DrawPriceChip(float y, string text, SKColor bg, SKColor fg)
        {
            var r = new SKRect(plot.Right + 1, y - 10, W - 2, y + 10);
            _pFill.Color = bg;
            canvas.DrawRoundRect(r, 3, 3, _pFill);
            _pFill.Color = fg;
            canvas.DrawText(text, plot.Right + 8, y + 4, SKTextAlign.Left, _fAxis, _pFill);
        }

        // 現在値チップ。価格が画面外へ出ても軸上に見え続けるよう位置をクランプする。
        DrawPriceChip(Math.Clamp(lastY, 10, plot.Bottom - 10),
            lastC.Close.ToString("N" + dec, CultureInfo.InvariantCulture), lastCol, SKColors.White);

        // ---- クロスヘア ----
        // 縦線は最寄りの足にスナップさせ (hoverIdx は凡例の表示対象にも使う)、
        // 横線はカーソルの高さそのままにして任意の価格を読めるようにする。
        int hoverIdx = -1;
        if (_mouse is Point mp && plot.Contains((float)mp.X, (float)mp.Y))
        {
            hoverIdx = Math.Clamp((int)Math.Round(_firstIndex + (mp.X - plot.Left) / xPer - 0.5), 0, n - 1);
            float cx = X(hoverIdx);
            float cy = (float)mp.Y;

            _pDash.Color = Theme.Crosshair;
            canvas.Save();
            canvas.ClipRect(plot);
            canvas.DrawLine(cx, plot.Top, cx, plot.Bottom, _pDash);
            canvas.DrawLine(plot.Left, cy, plot.Right, cy, _pDash);
            canvas.Restore();

            // カーソル位置の価格チップ
            DrawPriceChip(cy, PriceAt(cy).ToString("N" + dec, CultureInfo.InvariantCulture),
                Theme.ChipBg, Theme.TextBright);

            // 時間軸チップ。端でははみ出さないよう左右にクランプする。
            var t = candles[hoverIdx].Time;
            string label = s.Timeframe >= TimeSpan.FromDays(1)
                ? t.ToString("yyyy/MM/dd (ddd)", CultureInfo.InvariantCulture)
                : t.ToString("MM/dd (ddd) HH:mm", CultureInfo.InvariantCulture);
            float tw = _fAxis.MeasureText(label) + 14;
            float bx = Math.Clamp(cx - tw / 2, plot.Left, plot.Right - tw);
            _pFill.Color = Theme.ChipBg;
            canvas.DrawRoundRect(new SKRect(bx, plot.Bottom + 3, bx + tw, plot.Bottom + 21), 3, 3, _pFill);
            _pFill.Color = Theme.TextBright;
            canvas.DrawText(label, bx + tw / 2, plot.Bottom + 17, SKTextAlign.Center, _fAxis, _pFill);
        }

        // ---- 左上の凡例 ----
        // 文字色と書体が項目ごとに変わるため、描いた幅を返して次の描画位置に継ぎ足していく。
        float DrawSeg(string txt, SKColor color, SKFont f, float x, float y)
        {
            _pFill.Color = color;
            canvas.DrawText(txt, x, y, SKTextAlign.Left, f, _pFill);
            return x + f.MeasureText(txt);
        }
        string FmtP(double v) => v.ToString("N" + dec, CultureInfo.InvariantCulture);

        // 表示対象はホバー中の足、ホバーしていなければ最新足。変化率は1本前の終値との比較。
        int lcIdx = hoverIdx >= 0 ? hoverIdx : n - 1;
        var lc = candles[lcIdx];
        double prevClose = lcIdx > 0 ? candles[lcIdx - 1].Close : lc.Open;
        double chg = prevClose != 0 ? (lc.Close - prevClose) / prevClose * 100 : 0;
        var vcol = lc.IsUp ? Theme.Up : Theme.Down;

        // 1行目: 銘柄・時間足・OHLC・変化率・出来高
        float lx = 10, ly = 21;
        lx = DrawSeg(s.Info.Name, Theme.TextBright, _fBold, lx, ly);
        lx = DrawSeg("  " + _tfLabel + "   ", Theme.TextDim, _fText, lx, ly);
        lx = DrawSeg("O ", Theme.TextDim, _fText, lx, ly);
        lx = DrawSeg(FmtP(lc.Open) + "  ", vcol, _fText, lx, ly);
        lx = DrawSeg("H ", Theme.TextDim, _fText, lx, ly);
        lx = DrawSeg(FmtP(lc.High) + "  ", vcol, _fText, lx, ly);
        lx = DrawSeg("L ", Theme.TextDim, _fText, lx, ly);
        lx = DrawSeg(FmtP(lc.Low) + "  ", vcol, _fText, lx, ly);
        lx = DrawSeg("C ", Theme.TextDim, _fText, lx, ly);
        lx = DrawSeg(FmtP(lc.Close), vcol, _fText, lx, ly);
        lx = DrawSeg($"  {chg.ToString("+0.00;-0.00", CultureInfo.InvariantCulture)}%", vcol, _fText, lx, ly);
        DrawSeg("  Vol " + FmtVol(lc.Volume), vcol, _fText, lx, ly);

        // 2行目以降: 有効なインジケーターを1行ずつ。ウォームアップ中は "–"。
        float iy = 42;
        if (ShowSma)
        {
            double v = lcIdx < _sma.Length ? _sma[lcIdx] : double.NaN;
            DrawSeg($"SMA 20   {(double.IsNaN(v) ? "–" : FmtP(v))}", Theme.Sma, _fText, 10, iy);
            iy += 19;
        }
        if (ShowEma)
        {
            double v = lcIdx < _ema.Length ? _ema[lcIdx] : double.NaN;
            DrawSeg($"EMA 50   {(double.IsNaN(v) ? "–" : FmtP(v))}", Theme.Ema, _fText, 10, iy);
        }
    }

    // ---------------- ヘルパー ----------------

    // インジケーター値の可視範囲を価格レンジに取り込む (NaN は無視)
    static void AccRange(double[] arr, int i0, int i1, ref double min, ref double max)
    {
        for (int i = i0; i <= i1 && i < arr.Length; i++)
        {
            double v = arr[i];
            if (double.IsNaN(v)) continue;
            if (v < min) min = v;
            if (v > max) max = v;
        }
    }

    // グリッド間隔を「切りのよい数」に丸める。
    // 目標本数から求めた生の間隔を、同じ桁の 1 / 2 / 2.5 / 5 / 10 のうち下回らない最小のものに寄せる。
    static double NiceStep(double range, int targetLines)
    {
        double raw = range / Math.Max(2, targetLines);
        double mag = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        foreach (double m in (double[])[1, 2, 2.5, 5, 10])
            if (raw <= m * mag * 1.00001) return m * mag;
        return 10 * mag;
    }

    // 出来高を K / M / B の単位付きに短縮する
    static string FmtVol(double v) =>
        v >= 1e9 ? (v / 1e9).ToString("0.00", CultureInfo.InvariantCulture) + "B" :
        v >= 1e6 ? (v / 1e6).ToString("0.00", CultureInfo.InvariantCulture) + "M" :
        v >= 1e3 ? (v / 1e3).ToString("0.00", CultureInfo.InvariantCulture) + "K" :
        v.ToString("0", CultureInfo.InvariantCulture);
}
