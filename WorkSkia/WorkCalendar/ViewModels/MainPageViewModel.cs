namespace WorkCalendar.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using WorkCalendar.Models;
using WorkCalendar.Services;

public sealed class MainPageViewModel : INotifyPropertyChanged
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    private readonly IScheduleService scheduleService = new ScheduleService();
    private readonly IHolidayService holidayService = new HolidayService();

    private MonthViewBuilder builder;
    private int currentYear;
    private int currentMonth;
    private MonthViewModel? monthViewModel;
    private DayOfWeek firstDayOfWeek = DayOfWeek.Monday;
    private CalendarSelectionMode selectionMode = CalendarSelectionMode.None;
    private DateOnly? selectedDate;
    private ObservableCollection<DateOnly> selectedDates = [];
    private DateOnly? selectedStartDate;
    private DateOnly? selectedEndDate;
    private DateOnly? minDate;
    private DateOnly? maxDate;
    private CultureInfo? culture;

    public MonthViewModel? MonthViewModel
    {
        get => monthViewModel;
        private set => SetField(ref monthViewModel, value);
    }

    /// <summary>週の開始曜日。変更すると月ビューが再構築されます。</summary>
    public DayOfWeek FirstDayOfWeek
    {
        get => firstDayOfWeek;
        set
        {
            if (!SetField(ref firstDayOfWeek, value))
                return;
            builder = new MonthViewBuilder(value);
            LoadMonth(currentYear, currentMonth);
        }
    }

    public CalendarSelectionMode SelectionMode { get => selectionMode; set => SetField(ref selectionMode, value); }

    public DateOnly? SelectedDate          { get => selectedDate;       set => SetField(ref selectedDate, value); }
    public ObservableCollection<DateOnly> SelectedDates { get => selectedDates; set => SetField(ref selectedDates, value); }
    public DateOnly? SelectedStartDate     { get => selectedStartDate;  set => SetField(ref selectedStartDate, value); }
    public DateOnly? SelectedEndDate       { get => selectedEndDate;    set => SetField(ref selectedEndDate, value); }

    /// <summary>選択可能な最小日付。null の場合は制限なし。</summary>
    public DateOnly? MinDate               { get => minDate;            set => SetField(ref minDate, value); }

    /// <summary>選択可能な最大日付。null の場合は制限なし。</summary>
    public DateOnly? MaxDate               { get => maxDate;            set => SetField(ref maxDate, value); }

    /// <summary>表示に使用するカルチャ。null の場合は日本語固定。</summary>
    public CultureInfo? Culture            { get => culture;            set => SetField(ref culture, value); }

    public ICommand PrevMonthCommand { get; }
    public ICommand NextMonthCommand { get; }
    public ICommand GoToTodayCommand { get; }
    public ICommand DayTappedCommand { get; }
    public ICommand EventTappedCommand { get; }

    public MainPageViewModel()
    {
        builder = new MonthViewBuilder(firstDayOfWeek);
        PrevMonthCommand = new Command(OnPrevMonth);
        NextMonthCommand = new Command(OnNextMonth);
        GoToTodayCommand = new Command(OnGoToToday);
        DayTappedCommand = new Command<DayViewModel>(OnDayTapped);
        EventTappedCommand = new Command<ScheduleEvent>(OnEventTapped);

        currentYear = Today.Year;
        currentMonth = Today.Month;
        LoadMonth(currentYear, currentMonth);
    }

    /// <summary>指定した日付の月を表示します。</summary>
    public void GoToDate(DateOnly date)
    {
        currentYear = date.Year;
        currentMonth = date.Month;
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

    private void OnGoToToday() => GoToDate(Today);

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
