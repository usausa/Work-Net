namespace WorkCalendar.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;

using WorkCalendar.Models;

public sealed class MonthViewBuilder(DayOfWeek weekStartDayOfWeek = DayOfWeek.Monday)
{
    private const int DaysPerWeek = 7;
    private const int WeeksPerMonth = 6;

    public (DateOnly Start, DateOnly End) GetDisplayRange(int year, int month)
    {
        var monthFirst = new DateOnly(year, month, 1);
        var firstDayToShow = AlignToWeekStart(monthFirst);
        var lastDayToShow = firstDayToShow.AddDays((WeeksPerMonth * DaysPerWeek) - 1);
        return (firstDayToShow, lastDayToShow);
    }

    public MonthViewModel Build(
        int year,
        int month,
        DateOnly today,
        IReadOnlyList<ScheduleEvent> events,
        IReadOnlyList<Stamp> stamps,
        IReadOnlyList<DateOnly> holidays)
    {
        var (firstDayToShow, _) = GetDisplayRange(year, month);
        var holidaySet = new HashSet<DateOnly>(holidays);
        var stampLookup = stamps
            .GroupBy(static s => s.Date)
            .ToDictionary(static g => g.Key, static g => (IReadOnlyList<Stamp>)g.ToList());

        var weeks = new List<WeekViewModel>(capacity: WeeksPerMonth);
        for (var w = 0; w < WeeksPerMonth; w++)
        {
            var weekStart = firstDayToShow.AddDays(w * DaysPerWeek);
            var weekEnd = weekStart.AddDays(DaysPerWeek - 1);

            var days = new List<DayViewModel>(capacity: DaysPerWeek);
            for (var d = 0; d < DaysPerWeek; d++)
            {
                var date = weekStart.AddDays(d);
                days.Add(new DayViewModel
                {
                    Date = date,
                    IsCurrentMonth = (date.Year == year) && (date.Month == month),
                    IsToday = date == today,
                    Kind = DetermineKind(date, holidaySet),
                    Stamps = stampLookup.TryGetValue(date, out var s) ? s : [],
                });
            }

            var placements = AssignPlacements(weekStart, weekEnd, events);
            var slotCount = placements.Count == 0 ? 0 : placements.Max(static p => p.Slot) + 1;

            weeks.Add(new WeekViewModel
            {
                Days = days,
                EventPlacements = placements,
                SlotCount = slotCount,
            });
        }

        return new MonthViewModel
        {
            Year = year,
            Month = month,
            Today = today,
            Weeks = weeks,
        };
    }

    private DateOnly AlignToWeekStart(DateOnly date)
    {
        var current = date;
        while (current.DayOfWeek != weekStartDayOfWeek)
        {
            current = current.AddDays(-1);
        }
        return current;
    }

    private static DayKind DetermineKind(DateOnly date, HashSet<DateOnly> holidays)
    {
        if (holidays.Contains(date))
        {
            return DayKind.Holiday;
        }
        return date.DayOfWeek switch
        {
            DayOfWeek.Sunday => DayKind.Sunday,
            DayOfWeek.Saturday => DayKind.Saturday,
            _ => DayKind.Weekday,
        };
    }

    private static IReadOnlyList<EventPlacement> AssignPlacements(
        DateOnly weekStart,
        DateOnly weekEnd,
        IReadOnlyList<ScheduleEvent> events)
    {
        var candidates = events
            .Where(e => (e.EndDate >= weekStart) && (e.StartDate <= weekEnd))
            .Select(e =>
            {
                var clippedStart = e.StartDate < weekStart ? weekStart : e.StartDate;
                var clippedEnd = e.EndDate > weekEnd ? weekEnd : e.EndDate;
                return new EventCandidate(
                    e,
                    clippedStart.DayNumber - weekStart.DayNumber,
                    clippedEnd.DayNumber - weekStart.DayNumber,
                    e.StartDate < weekStart,
                    e.EndDate > weekEnd);
            })
            .OrderBy(static c => c.StartCol)
            .ThenByDescending(static c => c.EndCol - c.StartCol)
            .ToList();

        var slotOccupancy = new List<bool[]>();
        var placements = new List<EventPlacement>(capacity: candidates.Count);

        foreach (var c in candidates)
        {
            var slot = FindAvailableSlot(slotOccupancy, c.StartCol, c.EndCol);
            if (slot >= slotOccupancy.Count)
            {
                slotOccupancy.Add(new bool[DaysPerWeek]);
            }
            for (var col = c.StartCol; col <= c.EndCol; col++)
            {
                slotOccupancy[slot][col] = true;
            }
            placements.Add(new EventPlacement
            {
                Event = c.Event,
                StartColumn = c.StartCol,
                ColumnSpan = (c.EndCol - c.StartCol) + 1,
                Slot = slot,
                ContinuesFromPreviousWeek = c.ContinuesFromPrev,
                ContinuesToNextWeek = c.ContinuesToNext,
            });
        }

        return placements;
    }

    private static int FindAvailableSlot(List<bool[]> occupancy, int startCol, int endCol)
    {
        for (var slot = 0; slot < occupancy.Count; slot++)
        {
            var fits = true;
            for (var col = startCol; col <= endCol; col++)
            {
                if (occupancy[slot][col])
                {
                    fits = false;
                    break;
                }
            }
            if (fits)
            {
                return slot;
            }
        }
        return occupancy.Count;
    }

    private readonly record struct EventCandidate(
        ScheduleEvent Event,
        int StartCol,
        int EndCol,
        bool ContinuesFromPrev,
        bool ContinuesToNext);
}
