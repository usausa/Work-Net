namespace Trader.Models;

// 1本のローソク足 (OHLCV)。
// 系列は数千本を配列で持ち回り毎フレーム走査するため、参照型ではなく構造体にしている。
public struct Candle
{
    public DateTime Time;
    public double Open;
    public double High;
    public double Low;
    public double Close;
    public double Volume;

    // 陽線(始値以上で引けた)かどうか。配色の判定に使う。
    public readonly bool IsUp => Close >= Open;
}
