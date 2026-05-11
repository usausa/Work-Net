namespace WorkSkiaGit02.Converters;

using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

using WorkSkiaGit02.Graph;

public sealed class RefKindBrushConverter : IValueConverter
{
    public bool Foreground { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not GraphRefKind kind)
        {
            return Brushes.Transparent;
        }

        return (kind, Foreground) switch
        {
            (GraphRefKind.Head, false) => Brush(0x21, 0x96, 0xF3),
            (GraphRefKind.Head, true) => Brushes.White,
            (GraphRefKind.LocalBranch, false) => Brush(0xC8, 0xE6, 0xC9),
            (GraphRefKind.LocalBranch, true) => Brush(0x1B, 0x5E, 0x20),
            (GraphRefKind.RemoteBranch, false) => Brush(0xFF, 0xE0, 0xB2),
            (GraphRefKind.RemoteBranch, true) => Brush(0xE6, 0x5C, 0x00),
            (GraphRefKind.Tag, false) => Brush(0xFF, 0xF5, 0x9D),
            (GraphRefKind.Tag, true) => Brush(0x82, 0x6F, 0x00),
            _ => Brushes.LightGray,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static SolidColorBrush Brush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
