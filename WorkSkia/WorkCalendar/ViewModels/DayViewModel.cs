namespace WorkCalendar.ViewModels;

using System;
using System.Collections.Generic;

using WorkCalendar.Models;

public sealed class DayViewModel
{
    public required DateOnly Date { get; init; }

    public required bool IsCurrentMonth { get; init; }

    public required bool IsToday { get; init; }

    public required DayKind Kind { get; init; }

    public required IReadOnlyList<Stamp> Stamps { get; init; }
}
