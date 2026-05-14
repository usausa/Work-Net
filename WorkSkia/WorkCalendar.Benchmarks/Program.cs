using System.Diagnostics;

using WorkCalendar.Services;
using WorkCalendar.ViewModels;

const int WarmupIterations = 50;
const int Iterations = 500;

var scheduleService = new ScheduleService();
var holidayService = new HolidayService();
var builder = new MonthViewBuilder();
var today = new DateOnly(2026, 4, 15);

RunScenario("2026/03 empty", 2026, 3);
RunScenario("2026/04 data", 2026, 4);
RunScenario("2026/05 data", 2026, 5);

void RunScenario(string name, int year, int month)
{
    var (rangeStart, rangeEnd) = builder.GetDisplayRange(year, month);
    var events = scheduleService.GetEvents(rangeStart, rangeEnd);
    var stamps = scheduleService.GetStamps(rangeStart, rangeEnd);
    var holidays = holidayService.GetHolidays(rangeStart, rangeEnd);

    for (var i = 0; i < WarmupIterations; i++)
    {
        _ = builder.Build(year, month, today, events, stamps, holidays);
    }

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
    var sw = Stopwatch.StartNew();
    for (var i = 0; i < Iterations; i++)
    {
        _ = builder.Build(year, month, today, events, stamps, holidays);
    }
    sw.Stop();
    var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();

    var averageMs = sw.Elapsed.TotalMilliseconds / Iterations;
    var allocatedBytes = (allocatedAfter - allocatedBefore) / Iterations;
    Console.WriteLine($"{name} | Avg: {averageMs:F4}ms | Alloc: {allocatedBytes:N0} bytes | Events: {events.Count}, Stamps: {stamps.Count}, Holidays: {holidays.Count}");
}
