namespace WorkCalendar.Models;

using System;

public sealed class ScheduleEvent
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required DateOnly StartDate { get; init; }

    public required DateOnly EndDate { get; init; }

    public ScheduleStyle Style { get; init; } = ScheduleStyle.Filled;

    public Color BackgroundColor { get; init; } = Colors.LightGray;

    public Color TextColor { get; init; } = Colors.White;

    public bool Underline { get; init; }

    public string? LeadingGlyph { get; init; }

    public int DurationDays => (EndDate.DayNumber - StartDate.DayNumber) + 1;

    public bool IsMultiDay => DurationDays > 1;
}
