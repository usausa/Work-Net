namespace WorkCalendar.Services;

using System;
using System.Collections.Generic;

public interface IHolidayService
{
    IReadOnlyList<DateOnly> GetHolidays(DateOnly start, DateOnly end);
}
