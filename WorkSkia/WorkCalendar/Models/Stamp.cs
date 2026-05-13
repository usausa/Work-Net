namespace WorkCalendar.Models;

using System;

public sealed class Stamp
{
    public required string Id { get; init; }

    public required DateOnly Date { get; init; }

    public required string Glyph { get; init; }

    public StampPosition Position { get; init; } = StampPosition.Center;

    public double FontSize { get; init; } = 28;

    public double Opacity { get; init; } = 1.0;
}
