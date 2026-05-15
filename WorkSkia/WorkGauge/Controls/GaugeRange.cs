namespace WorkGauge.Controls;

public sealed class GaugeRange
{
    private Color? endColor;

    public double StartValue { get; set; }
    public double EndValue { get; set; }

    public Color StartColor { get; set; } = Colors.Transparent;

    public Color EndColor
    {
        get => endColor ?? StartColor;
        set => endColor = value;
    }
}
