namespace WorkCalendar.Services;

using System;
using System.Collections.Generic;

using WorkCalendar.Models;

public interface IScheduleService
{
    IReadOnlyList<ScheduleEvent> GetEvents(DateOnly start, DateOnly end);

    IReadOnlyList<Stamp> GetStamps(DateOnly start, DateOnly end);
}
