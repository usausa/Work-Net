namespace WorkSkiaGitMaui.Converters;

using System;
using System.Globalization;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

using WorkSkiaGitMaui.Graph;

public sealed class RefKindBrushConverter : IValueConverter
{
    public bool Foreground { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not GraphRefKind kind)
        {
            return Colors.Transparent;
        }

        return (kind, Foreground) switch
        {
            (GraphRefKind.Head, false) => Color.FromRgb(0x21, 0x96, 0xF3),
            (GraphRefKind.Head, true) => Colors.White,
            (GraphRefKind.LocalBranch, false) => Color.FromRgb(0xC8, 0xE6, 0xC9),
            (GraphRefKind.LocalBranch, true) => Color.FromRgb(0x1B, 0x5E, 0x20),
            (GraphRefKind.RemoteBranch, false) => Color.FromRgb(0xFF, 0xE0, 0xB2),
            (GraphRefKind.RemoteBranch, true) => Color.FromRgb(0xE6, 0x5C, 0x00),
            (GraphRefKind.Tag, false) => Color.FromRgb(0xFF, 0xF5, 0x9D),
            (GraphRefKind.Tag, true) => Color.FromRgb(0x82, 0x6F, 0x00),
            _ => Colors.LightGray,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
