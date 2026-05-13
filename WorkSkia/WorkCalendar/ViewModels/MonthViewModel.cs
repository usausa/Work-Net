namespace WorkCalendar.ViewModels;

using System;
using System.Collections.Generic;

public sealed class MonthViewModel
{
    public required int Year { get; init; }

    public required int Month { get; init; }

    public required DateOnly Today { get; init; }

    public required IReadOnlyList<WeekViewModel> Weeks { get; init; }
}
