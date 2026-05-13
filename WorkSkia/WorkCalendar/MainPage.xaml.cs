namespace WorkCalendar;

using System;
using System.Diagnostics;

using WorkCalendar.Models;
using WorkCalendar.Services;
using WorkCalendar.ViewModels;

public partial class MainPage : ContentPage
{
    private static readonly DateOnly ReferenceToday = DateOnly.FromDateTime(DateTime.Today);

    private readonly MonthViewBuilder builder = new(DayOfWeek.Monday);
    private readonly IScheduleService scheduleService = new ScheduleService();
    private readonly IHolidayService holidayService = new HolidayService();

    private int currentYear;
    private int currentMonth;

    public MainPage()
    {
        InitializeComponent();

        Calendar.PrevMonthCommand = new Command(OnPrevMonthTapped);
        Calendar.NextMonthCommand = new Command(OnNextMonthTapped);
        Calendar.DayTappedCommand = new Command<DayViewModel>(OnDayTapped);
        Calendar.EventTappedCommand = new Command<ScheduleEvent>(OnEventTapped);

        currentYear = ReferenceToday.Year;
        currentMonth = ReferenceToday.Month;
        LoadMonth(currentYear, currentMonth);
    }

    private void OnPrevMonthTapped()
    {
        var prev = new DateOnly(currentYear, currentMonth, 1).AddMonths(-1);
        currentYear = prev.Year;
        currentMonth = prev.Month;
        LoadMonth(currentYear, currentMonth);
    }

    private void OnNextMonthTapped()
    {
        var next = new DateOnly(currentYear, currentMonth, 1).AddMonths(1);
        currentYear = next.Year;
        currentMonth = next.Month;
        LoadMonth(currentYear, currentMonth);
    }

    private void LoadMonth(int year, int month)
    {
        var (rangeStart, rangeEnd) = builder.GetDisplayRange(year, month);
        var events = scheduleService.GetEvents(rangeStart, rangeEnd);
        var stamps = scheduleService.GetStamps(rangeStart, rangeEnd);
        var holidays = holidayService.GetHolidays(rangeStart, rangeEnd);
        Calendar.ViewModel = builder.Build(year, month, ReferenceToday, events, stamps, holidays);
    }

    private static void OnDayTapped(DayViewModel day) =>
        Debug.WriteLine($"Day tapped: {day.Date:yyyy-MM-dd}");

    private static void OnEventTapped(ScheduleEvent evt) =>
        Debug.WriteLine($"Event tapped: {evt.Id} {evt.Title}");
}
