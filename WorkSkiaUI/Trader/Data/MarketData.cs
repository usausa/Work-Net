using Trader.Models;

namespace Trader.Data;

// 銘柄定義。VolPerMinute は1分あたりのボラティリティ(対数収益率の標準偏差)で、
// 時間足に応じてスケールさせることで足の長さが変わっても値動きの粗さを揃える。
// Seed は同じ銘柄なら毎回同じチャートが出るようにするための固定乱数種。
public sealed record SymbolInfo(string Name, double BasePrice, double VolPerMinute, int Decimals, int Seed);

public static class Symbols
{
    public static readonly SymbolInfo[] All =
    [
        new("BTC/USD",    64000,  0.00080, 1, 101),
        new("ETH/USD",     3200,  0.00095, 2, 202),
        new("AAPL",         226,  0.00035, 2, 303),
        new("EUR/JPY",    171.5,  0.00012, 3, 404),
        new("NIKKEI 225", 41800,  0.00030, 0, 505),
    ];
}

// 疑似マーケットデータ系列。
// 実データフィードの代わりに、幾何ブラウン運動 (対数正規のランダムウォーク) に
// 「トレンドレジーム」を重ねて生成する。純粋なランダムウォークだと方向感のない
// のっぺりした波形になり、移動平均を重ねたときにチャートらしく見えないため。
public sealed class MarketSeries
{
    public SymbolInfo Info { get; }
    public TimeSpan Timeframe { get; }
    public List<Candle> Candles { get; } = [];

    // データ変更のたびに増える。描画側はこの値を見てインジケーターの再計算要否を判断する。
    public long Version { get; private set; }

    readonly Random _rng;
    readonly double _sigma;   // この時間足1本あたりのボラティリティ

    // 現在のトレンドの向きと、その残り本数。_driftLeft が尽きたら向きを引き直す。
    double _drift;
    int _driftLeft;

    public MarketSeries(SymbolInfo info, TimeSpan timeframe, int history = 5000)
    {
        Info = info;
        Timeframe = timeframe;
        _rng = new Random(info.Seed * 397 ^ (int)timeframe.TotalMinutes);

        // ボラティリティは時間の平方根に比例してスケールする
        _sigma = info.VolPerMinute * Math.Sqrt(timeframe.TotalMinutes);

        // ヒストリカルを生成: 現在時刻から history 本ぶん遡った時刻を開始点にして、
        // 最後の足がちょうど「現在進行中の足」になるように埋めていく。
        var t = Align(DateTime.Now, timeframe) - TimeSpan.FromTicks(timeframe.Ticks * (history - 1));
        double price = info.BasePrice * (0.75 + 0.5 * _rng.NextDouble());  // 開始価格は基準値の前後にばらす
        for (int i = 0; i < history; i++)
        {
            Candles.Add(NextCandle(t, ref price));
            t += timeframe;
        }
        Version++;
    }

    // 1本分の足を生成する。price は生成後の終値で上書きされる。
    Candle NextCandle(DateTime time, ref double price)
    {
        // トレンドレジーム: 数十本ごとにドリフトの向きを入れ替えて、上昇・下降の局面を作る
        if (--_driftLeft <= 0)
        {
            _driftLeft = _rng.Next(30, 120);
            _drift = (_rng.NextDouble() - 0.5) * _sigma * 0.7;
        }

        // 足の中を6ステップに分けて歩かせ、その通過点から高値・安値を拾う。
        // 始値と終値だけではヒゲが作れないため。
        double open = price, hi = price, lo = price, p = price;
        for (int s = 0; s < 6; s++)
        {
            p *= Math.Exp(Gauss() * _sigma * 0.45 + _drift / 6);
            if (p > hi) hi = p;
            if (p < lo) lo = p;
        }
        price = p;

        // 出来高は値幅が大きい足ほど多くなるようにして、価格と出来高の相関を持たせる
        double range = (hi - lo) / open;
        double volume = Info.BasePrice * 12 * (0.2 + _rng.NextDouble()) * (1 + 140 * range);
        return new Candle { Time = time, Open = open, High = hi, Low = lo, Close = p, Volume = volume };
    }

    // リアルタイムのティックを1回分適用する。dtSeconds はティック間隔。
    public void Tick(DateTime now, double dtSeconds)
    {
        var boundary = Align(now, Timeframe);
        var last = Candles[^1];

        // 時間足の境界を跨いだら、直前の終値を始値として新しい足を起こす
        if (boundary > last.Time)
        {
            double open = last.Close;
            last = new Candle { Time = boundary, Open = open, High = open, Low = open, Close = open, Volume = 0 };
            Candles.Add(last);
        }

        // 進行中の足を更新する。終値を動かし、それに合わせて高値・安値を広げる。
        double sigmaTick = Info.VolPerMinute * Math.Sqrt(dtSeconds / 60.0) * 1.4;
        double p = last.Close * Math.Exp(Gauss() * sigmaTick);
        last.Close = p;
        if (p > last.High) last.High = p;
        if (p < last.Low) last.Low = p;

        // 出来高は足の経過割合に応じて積み上げる
        last.Volume += Info.BasePrice * 12 * (0.2 + _rng.NextDouble()) * (dtSeconds / Timeframe.TotalSeconds) * 2;

        Candles[^1] = last;   // 構造体なのでコピーを書き戻す
        Version++;
    }

    // Box-Muller 法による標準正規乱数
    double Gauss()
    {
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = _rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }

    // 時刻を時間足の境界に切り下げる (例: 1時間足なら毎時0分)
    public static DateTime Align(DateTime t, TimeSpan tf) => new(t.Ticks - t.Ticks % tf.Ticks);
}

// (銘柄, 時間足) ごとの系列をキャッシュして提供する。
// 一度作った系列は破棄せず保持し、画面に出ていない組み合わせも Tick を受け続けるので、
// 銘柄や時間足を切り替えても時間が飛んだように見えない。
public sealed class MarketDataService
{
    readonly Dictionary<(string, TimeSpan), MarketSeries> _cache = [];

    public MarketSeries Get(SymbolInfo symbol, TimeSpan timeframe)
    {
        var key = (symbol.Name, timeframe);
        if (!_cache.TryGetValue(key, out var series))
            _cache[key] = series = new MarketSeries(symbol, timeframe);
        return series;
    }

    public IEnumerable<MarketSeries> All => _cache.Values;
}
