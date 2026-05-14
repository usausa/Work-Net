namespace WorkCalendar.ViewModels;

using System;
using System.Collections.Generic;
using System.Diagnostics;

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
        var sw = Stopwatch.StartNew();

        var (firstDayToShow, _) = GetDisplayRange(year, month);

        var t0 = sw.Elapsed;
        var holidaySet = holidays.Count == 0 ? null : new HashSet<DateOnly>(holidays);
        var t1 = sw.Elapsed;
        var stampLookup = CreateStampLookup(stamps);
        var t2 = sw.Elapsed;
        var weeklyCandidates = CreateWeeklyEventCandidates(firstDayToShow, events);
        var t3 = sw.Elapsed;

        var weeks = new List<WeekViewModel>(capacity: WeeksPerMonth);
        for (var w = 0; w < WeeksPerMonth; w++)
        {
            var weekStart = firstDayToShow.AddDays(w * DaysPerWeek);

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

            var placements = AssignPlacements(weeklyCandidates[w]);
            var slotCount = GetSlotCount(placements);

            weeks.Add(new WeekViewModel
            {
                Days = days,
                EventPlacements = placements,
                SlotCount = slotCount,
            });
        }

        var t4 = sw.Elapsed;

        var result = new MonthViewModel
        {
            Year = year,
            Month = month,
            Today = today,
            Weeks = weeks,
        };

        sw.Stop();
        Debug.WriteLine(
            $"[Build] {year}/{month:D2} {sw.Elapsed.TotalMilliseconds:F3}ms" +
            $" | HolidaySet: {(t1 - t0).TotalMilliseconds:F3}ms" +
            $" | StampLookup: {(t2 - t1).TotalMilliseconds:F3}ms" +
            $" | WeeklyCandidates: {(t3 - t2).TotalMilliseconds:F3}ms" +
            $" | Weeks: {(t4 - t3).TotalMilliseconds:F3}ms");

        return result;
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

    private static Dictionary<DateOnly, IReadOnlyList<Stamp>> CreateStampLookup(IReadOnlyList<Stamp> stamps)
    {
        var stampLookup = new Dictionary<DateOnly, IReadOnlyList<Stamp>>(stamps.Count);
        for (var i = 0; i < stamps.Count; i++)
        {
            var stamp = stamps[i];
            if (!stampLookup.TryGetValue(stamp.Date, out var existing))
            {
                stampLookup.Add(stamp.Date, [stamp]);
                continue;
            }

            if (existing is List<Stamp> list)
            {
                list.Add(stamp);
                continue;
            }

            stampLookup[stamp.Date] = new List<Stamp> { existing[0], stamp };
        }

        return stampLookup;
    }

    private static DayKind DetermineKind(DateOnly date, HashSet<DateOnly>? holidays)
    {
        if (holidays?.Contains(date) == true)
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

    private static List<EventCandidate>[] CreateWeeklyEventCandidates(DateOnly firstDayToShow, IReadOnlyList<ScheduleEvent> events)
    {
        var weeklyCandidates = new List<EventCandidate>[WeeksPerMonth];
        for (var i = 0; i < WeeksPerMonth; i++)
            weeklyCandidates[i] = [];

        for (var i = 0; i < events.Count; i++)
        {
            var e = events[i];
            var firstWeek = Math.Max(0, (e.StartDate.DayNumber - firstDayToShow.DayNumber) / DaysPerWeek);
            var lastWeek = Math.Min(WeeksPerMonth - 1, (e.EndDate.DayNumber - firstDayToShow.DayNumber) / DaysPerWeek);
            for (var weekIndex = firstWeek; weekIndex <= lastWeek; weekIndex++)
            {
                var weekStart = firstDayToShow.AddDays(weekIndex * DaysPerWeek);
                var weekEnd = weekStart.AddDays(DaysPerWeek - 1);
                var clippedStart = e.StartDate < weekStart ? weekStart : e.StartDate;
                var clippedEnd = e.EndDate > weekEnd ? weekEnd : e.EndDate;
                weeklyCandidates[weekIndex].Add(new EventCandidate(
                    e,
                    clippedStart.DayNumber - weekStart.DayNumber,
                    clippedEnd.DayNumber - weekStart.DayNumber,
                    e.StartDate < weekStart,
                    e.EndDate > weekEnd));
            }
        }

        for (var i = 0; i < weeklyCandidates.Length; i++)
        {
            weeklyCandidates[i].Sort(static (left, right) =>
            {
                var startComparison = left.StartCol.CompareTo(right.StartCol);
                if (startComparison != 0)
                    return startComparison;

                return (right.EndCol - right.StartCol).CompareTo(left.EndCol - left.StartCol);
            });
        }

        return weeklyCandidates;
    }

    private static IReadOnlyList<EventPlacement> AssignPlacements(List<EventCandidate> candidates)
    {
        Span<int> slotOccupancy = stackalloc int[16];
        var slotCount = 0;
        var placements = new List<EventPlacement>(capacity: candidates.Count);

        for (var i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            var mask = ((1 << ((c.EndCol - c.StartCol) + 1)) - 1) << c.StartCol;
            var slot = FindAvailableSlot(slotOccupancy[..slotCount], mask);
            if (slot >= slotCount)
                slotCount++;
            slotOccupancy[slot] |= mask;

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

    private static int FindAvailableSlot(ReadOnlySpan<int> occupancy, int mask)
    {
        for (var slot = 0; slot < occupancy.Length; slot++)
        {
            if ((occupancy[slot] & mask) == 0)
                return slot;
        }
        return occupancy.Length;
    }

    private static int GetSlotCount(IReadOnlyList<EventPlacement> placements)
    {
        var slotCount = 0;
        for (var i = 0; i < placements.Count; i++)
        {
            var count = placements[i].Slot + 1;
            if (count > slotCount)
                slotCount = count;
        }

        return slotCount;
    }

    private readonly record struct EventCandidate(
        ScheduleEvent Event,
        int StartCol,
        int EndCol,
        bool ContinuesFromPrev,
        bool ContinuesToNext);
}
