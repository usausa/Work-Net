using Trader.Models;

namespace Trader.Charting;

// テクニカル指標の算出。
// 戻り値は元の系列と同じ長さの配列で、値が確定していない区間 (ウォームアップ) は NaN。
// 描画側は NaN を「線を切る位置」として扱うため、0 埋めではなく NaN を返している。
public static class Indicators
{
    // 単純移動平均。直近 period 本の合計を差分更新しながら 1 パスで求める。
    public static double[] Sma(IReadOnlyList<Candle> candles, int period)
    {
        var result = new double[candles.Count];
        double sum = 0;
        for (int i = 0; i < candles.Count; i++)
        {
            // 窓に入れて、窓からあふれた分を引く
            sum += candles[i].Close;
            if (i >= period) sum -= candles[i - period].Close;

            result[i] = i >= period - 1 ? sum / period : double.NaN;
        }
        return result;
    }

    // 指数移動平均。初期値は先頭の終値とし、SMA と見た目を揃えるため
    // 表示は period 本目以降からにしている。
    public static double[] Ema(IReadOnlyList<Candle> candles, int period)
    {
        var result = new double[candles.Count];
        double k = 2.0 / (period + 1);   // 平滑化係数
        double ema = 0;
        for (int i = 0; i < candles.Count; i++)
        {
            ema = i == 0 ? candles[i].Close : candles[i].Close * k + ema * (1 - k);
            result[i] = i >= period - 1 ? ema : double.NaN;
        }
        return result;
    }
}
