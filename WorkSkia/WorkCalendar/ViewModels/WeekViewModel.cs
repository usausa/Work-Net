namespace WorkCalendar.ViewModels;

using System.Collections.Generic;

public sealed class WeekViewModel
{
    public required IReadOnlyList<DayViewModel> Days { get; init; }

    public required IReadOnlyList<EventPlacement> EventPlacements { get; init; }

    public required int SlotCount { get; init; }
}
