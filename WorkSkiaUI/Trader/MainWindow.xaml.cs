using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Trader.Charting;
using Trader.Data;

namespace Trader;

// チャートのホスト。ツールバーの選択状態を ChartControl に反映し、
// ティックタイマーでリアルタイム更新を回す。描画そのものには関与しない。
public partial class MainWindow : Window
{
    readonly MarketDataService _feed = new();
    readonly DispatcherTimer _timer;
    const double TickSeconds = 0.1;

    // ツールバーで選択中の表示条件
    SymbolInfo _symbol = Symbols.All[0];
    TimeSpan _tf = TimeSpan.FromMinutes(1);
    MarketSeries? _series;

    public MainWindow()
    {
        InitializeComponent();
        BuildSymbolButtons();
        ApplySeries();

        // リアルタイム更新。表示中でない系列も進めておかないと、
        // 銘柄や時間足を切り替えたときにそこだけ時間が止まって見えてしまう。
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(TickSeconds) };
        _timer.Tick += (_, _) =>
        {
            var now = DateTime.Now;
            foreach (var series in _feed.All) series.Tick(now, TickSeconds);
            Chart.Refresh();
            UpdateStatus();
        };
        _timer.Start();
    }

    // 銘柄チップは Symbols.All から動的に生成する (XAML 側に銘柄一覧を持たせない)
    void BuildSymbolButtons()
    {
        var style = (Style)FindResource("Chip");
        for (int i = 0; i < Symbols.All.Length; i++)
        {
            var rb = new RadioButton
            {
                Content = Symbols.All[i].Name,
                GroupName = "sym",
                Tag = i,
                Style = style,
            };
            rb.Checked += OnSymbolChecked;
            rb.IsChecked = i == 0;
            SymbolPanel.Children.Add(rb);
        }
    }

    // 選択中の (銘柄, 時間足) の系列をチャートへ渡す。
    // 初期化中は Chart がまだ生成されていないハンドラー経路があるため null を許容する。
    void ApplySeries()
    {
        if (Chart == null) return;
        _series = _feed.Get(_symbol, _tf);
        Chart.SetSeries(_series, TfLabel(_tf));
        UpdateStatus();
    }

    // 画面下のステータス行。最終値と前足比、本数、操作ヒントを出す。
    void UpdateStatus()
    {
        if (_series == null) return;
        var candles = _series.Candles;
        var last = candles[^1];
        double prev = candles.Count > 1 ? candles[^2].Close : last.Open;
        double chg = prev != 0 ? (last.Close - prev) / prev * 100 : 0;
        StatusText.Text =
            $"{_symbol.Name}   最終値 {last.Close.ToString("N" + _symbol.Decimals, CultureInfo.InvariantCulture)}" +
            $" ({chg.ToString("+0.00;-0.00", CultureInfo.InvariantCulture)}%)   ・   {candles.Count:N0} 本   ・   ● LIVE" +
            "      (ドラッグ: スクロール / ホイール: ズーム / ダブルクリック: 最新表示)";
    }

    // 時間足の表示名。チャート凡例には国際的な表記 (1m/1h/1D) を使う。
    static string TfLabel(TimeSpan tf) => (int)tf.TotalMinutes switch
    {
        1 => "1m",
        5 => "5m",
        15 => "15m",
        60 => "1h",
        240 => "4h",
        1440 => "1D",
        _ => tf.ToString(),
    };

    // ---------------- ツールバーのイベントハンドラー ----------------
    // どのチップも Tag に選択値を持たせ、ここで解決して状態へ反映する。

    void OnSymbolChecked(object sender, RoutedEventArgs e)
    {
        _symbol = Symbols.All[(int)((RadioButton)sender).Tag!];
        ApplySeries();
    }

    void OnTfChecked(object sender, RoutedEventArgs e)
    {
        _tf = TimeSpan.FromMinutes(int.Parse((string)((RadioButton)sender).Tag!, CultureInfo.InvariantCulture));
        ApplySeries();
    }

    // チャートタイプと表示オプションはデータに影響しないので、再描画だけ要求する
    void OnModeChecked(object sender, RoutedEventArgs e)
    {
        if (Chart == null) return;
        Chart.Mode = Enum.Parse<ChartStyle>((string)((RadioButton)sender).Tag!);
        Chart.Refresh();
    }

    void OnIndicator(object sender, RoutedEventArgs e)
    {
        if (Chart == null) return;
        Chart.ShowSma = CbSma.IsChecked == true;
        Chart.ShowEma = CbEma.IsChecked == true;
        Chart.ShowVolume = CbVol.IsChecked == true;
        Chart.Refresh();
    }

    void OnFit(object sender, RoutedEventArgs e) => Chart.FitToLatest();
}
