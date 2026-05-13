namespace WorkCalendar.ViewModels;

using WorkCalendar.Models;

public sealed class EventPlacement
{
    public required ScheduleEvent Event { get; init; }

    public required int StartColumn { get; init; }

    public required int ColumnSpan { get; init; }

    public required int Slot { get; init; }

    public bool ContinuesFromPreviousWeek { get; init; }

    public bool ContinuesToNextWeek { get; init; }
}
