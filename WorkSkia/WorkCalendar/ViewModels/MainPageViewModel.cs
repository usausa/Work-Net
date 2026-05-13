namespace WorkCalendar.ViewModels;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using WorkCalendar.Models;
using WorkCalendar.Services;

public sealed class MainPageViewModel : INotifyPropertyChanged
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    private readonly MonthViewBuilder builder = new(DayOfWeek.Monday);
    private readonly IScheduleService scheduleService = new ScheduleService();
    private readonly IHolidayService holidayService = new HolidayService();

    private int currentYear;
    private int currentMonth;
    private MonthViewModel? monthViewModel;

    public MonthViewModel? MonthViewModel
    {
        get => monthViewModel;
        private set => SetField(ref monthViewModel, value);
    }

    public ICommand PrevMonthCommand { get; }
    public ICommand NextMonthCommand { get; }
    public ICommand DayTappedCommand { get; }
    public ICommand EventTappedCommand { get; }

    public MainPageViewModel()
    {
        PrevMonthCommand = new Command(OnPrevMonth);
        NextMonthCommand = new Command(OnNextMonth);
        DayTappedCommand = new Command<DayViewModel>(OnDayTapped);
        EventTappedCommand = new Command<ScheduleEvent>(OnEventTapped);

        currentYear = Today.Year;
        currentMonth = Today.Month;
        LoadMonth(currentYear, currentMonth);
    }

    private void OnPrevMonth()
    {
        var prev = new DateOnly(currentYear, currentMonth, 1).AddMonths(-1);
        currentYear = prev.Year;
        currentMonth = prev.Month;
        LoadMonth(currentYear, currentMonth);
    }

    private void OnNextMonth()
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
        MonthViewModel = builder.Build(year, month, Today, events, stamps, holidays);
    }

    private static void OnDayTapped(DayViewModel day) =>
        Debug.WriteLine($"Day tapped: {day.Date:yyyy-MM-dd}");

    private static void OnEventTapped(ScheduleEvent evt) =>
        Debug.WriteLine($"Event tapped: {evt.Id} {evt.Title}");

    // ------------------------------------------------------------------ INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
