namespace WorkCalendar.Services;

using System;
using System.Collections.Generic;
using System.Linq;

public sealed class HolidayService : IHolidayService
{
    private static readonly IReadOnlyList<DateOnly> Master =
    [
        new(2019, 1, 1),
        new(2019, 1, 14),
        new(2019, 2, 11),
        new(2019, 3, 21),
        new(2019, 4, 29),
        new(2019, 4, 30),
        new(2019, 5, 1),
        new(2019, 5, 2),
        new(2019, 5, 3),
        new(2019, 5, 4),
        new(2019, 5, 5),
        new(2019, 5, 6),
        new(2019, 6, 10),
        new(2019, 7, 15),
        new(2019, 8, 11),
        new(2019, 8, 12),
        new(2019, 9, 16),
        new(2019, 9, 23),
        new(2019, 10, 14),
        new(2019, 10, 22),
        new(2019, 11, 3),
        new(2019, 11, 4),
        new(2019, 11, 23),
    ];

    public IReadOnlyList<DateOnly> GetHolidays(DateOnly start, DateOnly end) =>
        Master.Where(d => (d >= start) && (d <= end)).ToList();
}
