namespace WorkCalendar.Controls;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using Microsoft.Maui.Controls.Shapes;

using WorkCalendar.Models;
using WorkCalendar.ViewModels;

public partial class CalendarView : ContentView
{
    // ------------------------------------------------------------------ BindableProperties: Commands / ViewModel

    public static readonly BindableProperty ViewModelProperty =
        BindableProperty.Create(nameof(ViewModel), typeof(MonthViewModel), typeof(CalendarView),
            defaultValue: null, propertyChanged: OnViewModelChanged);

    public static readonly BindableProperty PrevMonthCommandProperty =
        BindableProperty.Create(nameof(PrevMonthCommand), typeof(ICommand), typeof(CalendarView));

    public static readonly BindableProperty NextMonthCommandProperty =
        BindableProperty.Create(nameof(NextMonthCommand), typeof(ICommand), typeof(CalendarView));

    public static readonly BindableProperty GoToTodayCommandProperty =
        BindableProperty.Create(nameof(GoToTodayCommand), typeof(ICommand), typeof(CalendarView));

    public static readonly BindableProperty DayTappedCommandProperty =
        BindableProperty.Create(nameof(DayTappedCommand), typeof(ICommand), typeof(CalendarView));

    public static readonly BindableProperty EventTappedCommandProperty =
        BindableProperty.Create(nameof(EventTappedCommand), typeof(ICommand), typeof(CalendarView));

    // ------------------------------------------------------------------ BindableProperties: Selection

    public static readonly BindableProperty SelectionModeProperty =
        BindableProperty.Create(nameof(SelectionMode), typeof(CalendarSelectionMode), typeof(CalendarView),
            CalendarSelectionMode.None, propertyChanged: OnSelectionPropertyChanged);

    public static readonly BindableProperty SelectedDateProperty =
        BindableProperty.Create(nameof(SelectedDate), typeof(DateOnly?), typeof(CalendarView),
            null, BindingMode.TwoWay, propertyChanged: OnSelectionPropertyChanged);

    public static readonly BindableProperty SelectedDatesProperty =
        BindableProperty.Create(nameof(SelectedDates), typeof(ObservableCollection<DateOnly>), typeof(CalendarView),
            null, BindingMode.TwoWay, propertyChanged: OnSelectedDatesChanged);

    public static readonly BindableProperty SelectedStartDateProperty =
        BindableProperty.Create(nameof(SelectedStartDate), typeof(DateOnly?), typeof(CalendarView),
            null, BindingMode.TwoWay, propertyChanged: OnSelectionPropertyChanged);

    public static readonly BindableProperty SelectedEndDateProperty =
        BindableProperty.Create(nameof(SelectedEndDate), typeof(DateOnly?), typeof(CalendarView),
            null, BindingMode.TwoWay, propertyChanged: OnSelectionPropertyChanged);

    public static readonly BindableProperty SelectedDayBackgroundProperty =
        BindableProperty.Create(nameof(SelectedDayBackground), typeof(Color), typeof(CalendarView),
            Color.FromArgb("#1A73E8"), propertyChanged: OnSelectionPropertyChanged);

    public static readonly BindableProperty SelectedDayTextColorProperty =
        BindableProperty.Create(nameof(SelectedDayTextColor), typeof(Color), typeof(CalendarView),
            Colors.White, propertyChanged: OnSelectionPropertyChanged);

    public static readonly BindableProperty RangeBackgroundProperty =
        BindableProperty.Create(nameof(RangeBackground), typeof(Color), typeof(CalendarView),
            Color.FromArgb("#BDD7F5"), propertyChanged: OnSelectionPropertyChanged);

    // ------------------------------------------------------------------ BindableProperties: Navigation limits

    public static readonly BindableProperty MinDateProperty =
        BindableProperty.Create(nameof(MinDate), typeof(DateOnly?), typeof(CalendarView),
            null, propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty MaxDateProperty =
        BindableProperty.Create(nameof(MaxDate), typeof(DateOnly?), typeof(CalendarView),
            null, propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty DisabledDayTextColorProperty =
        BindableProperty.Create(nameof(DisabledDayTextColor), typeof(Color), typeof(CalendarView),
            Color.FromArgb("#C0C0C0"), propertyChanged: OnRenderPropertyChanged);

    // ------------------------------------------------------------------ BindableProperties: Localization

    public static readonly BindableProperty CultureProperty =
        BindableProperty.Create(nameof(Culture), typeof(CultureInfo), typeof(CalendarView),
            null, propertyChanged: OnCultureChanged);

    // ------------------------------------------------------------------ BindableProperties: Layout / Sizes

    public static readonly BindableProperty DateRowHeightProperty =
        BindableProperty.Create(nameof(DateRowHeight), typeof(double), typeof(CalendarView), 26d, propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty SlotRowHeightProperty =
        BindableProperty.Create(nameof(SlotRowHeight), typeof(double), typeof(CalendarView), 17d, propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty DateNumberSizeProperty =
        BindableProperty.Create(nameof(DateNumberSize), typeof(double), typeof(CalendarView), 22d, propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty DateNumberMarginProperty =
        BindableProperty.Create(nameof(DateNumberMargin), typeof(Thickness), typeof(CalendarView), new Thickness(4, 2, 0, 0), propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty DateNumberFontSizeProperty =
        BindableProperty.Create(nameof(DateNumberFontSize), typeof(double), typeof(CalendarView), 13d, propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty EventFontSizeProperty =
        BindableProperty.Create(nameof(EventFontSize), typeof(double), typeof(CalendarView), 11d, propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty EventRowHeightProperty =
        BindableProperty.Create(nameof(EventRowHeight), typeof(double), typeof(CalendarView), 15d, propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty StampMarginEdgeProperty =
        BindableProperty.Create(nameof(StampMarginEdge), typeof(double), typeof(CalendarView), 2d, propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty NavButtonWidthProperty =
        BindableProperty.Create(nameof(NavButtonWidth), typeof(double), typeof(CalendarView), 44d);

    public static readonly BindableProperty NavButtonHeightProperty =
        BindableProperty.Create(nameof(NavButtonHeight), typeof(double), typeof(CalendarView), 44d);

    public static readonly BindableProperty NavButtonFontSizeProperty =
        BindableProperty.Create(nameof(NavButtonFontSize), typeof(double), typeof(CalendarView), 18d);

    public static readonly BindableProperty FirstDayOfWeekProperty =
        BindableProperty.Create(nameof(FirstDayOfWeek), typeof(DayOfWeek), typeof(CalendarView),
            DayOfWeek.Monday, propertyChanged: OnFirstDayOfWeekChanged);

    public static readonly BindableProperty SwipeEnabledProperty =
        BindableProperty.Create(nameof(SwipeEnabled), typeof(bool), typeof(CalendarView), true, propertyChanged: OnSwipeEnabledChanged);

    public static readonly BindableProperty HeaderPaddingProperty =
        BindableProperty.Create(nameof(HeaderPadding), typeof(Thickness), typeof(CalendarView), new Thickness(16, 12, 16, 8));

    public static readonly BindableProperty WeekdayHeaderFontSizeProperty =
        BindableProperty.Create(nameof(WeekdayHeaderFontSize), typeof(double), typeof(CalendarView), 13d);

    public static readonly BindableProperty WeekdayHeaderPaddingProperty =
        BindableProperty.Create(nameof(WeekdayHeaderPadding), typeof(Thickness), typeof(CalendarView), new Thickness(0, 6, 0, 6));

    public static readonly BindableProperty YearFontSizeProperty =
        BindableProperty.Create(nameof(YearFontSize), typeof(double), typeof(CalendarView), 13d);

    public static readonly BindableProperty MonthFontSizeProperty =
        BindableProperty.Create(nameof(MonthFontSize), typeof(double), typeof(CalendarView), 28d);

    // ------------------------------------------------------------------ BindableProperties: Colors

    public static readonly BindableProperty GridLineColorProperty =
        BindableProperty.Create(nameof(GridLineColor), typeof(Color), typeof(CalendarView), Color.FromArgb("#E0E0E0"), propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty WeekdayTextColorProperty =
        BindableProperty.Create(nameof(WeekdayTextColor), typeof(Color), typeof(CalendarView), Color.FromArgb("#1F1F1F"), propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty SaturdayTextColorProperty =
        BindableProperty.Create(nameof(SaturdayTextColor), typeof(Color), typeof(CalendarView), Color.FromArgb("#2196F3"), propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty SundayTextColorProperty =
        BindableProperty.Create(nameof(SundayTextColor), typeof(Color), typeof(CalendarView), Color.FromArgb("#E53935"), propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty OutsideMonthTextColorProperty =
        BindableProperty.Create(nameof(OutsideMonthTextColor), typeof(Color), typeof(CalendarView), Color.FromArgb("#BDBDBD"), propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty OutsideMonthBackgroundProperty =
        BindableProperty.Create(nameof(OutsideMonthBackground), typeof(Color), typeof(CalendarView), Color.FromArgb("#F2F2F2"), propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty WeekendBackgroundProperty =
        BindableProperty.Create(nameof(WeekendBackground), typeof(Color), typeof(CalendarView), Color.FromArgb("#FFF1F1"), propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty TodayBackgroundProperty =
        BindableProperty.Create(nameof(TodayBackground), typeof(Color), typeof(CalendarView), Colors.Black, propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty TodayTextColorProperty =
        BindableProperty.Create(nameof(TodayTextColor), typeof(Color), typeof(CalendarView), Colors.White, propertyChanged: OnRenderPropertyChanged);

    public static readonly BindableProperty NavButtonColorProperty =
        BindableProperty.Create(nameof(NavButtonColor), typeof(Color), typeof(CalendarView), Color.FromArgb("#333333"));

    public static readonly BindableProperty YearTextColorProperty =
        BindableProperty.Create(nameof(YearTextColor), typeof(Color), typeof(CalendarView), Colors.Black);

    public static readonly BindableProperty MonthTextColorProperty =
        BindableProperty.Create(nameof(MonthTextColor), typeof(Color), typeof(CalendarView), Colors.Black);

    public static readonly BindableProperty WeekdayHeaderColorProperty =
        BindableProperty.Create(nameof(WeekdayHeaderColor), typeof(Color), typeof(CalendarView), Color.FromArgb("#333333"));

    public static readonly BindableProperty SaturdayHeaderColorProperty =
        BindableProperty.Create(nameof(SaturdayHeaderColor), typeof(Color), typeof(CalendarView), Color.FromArgb("#2196F3"));

    public static readonly BindableProperty SundayHeaderColorProperty =
        BindableProperty.Create(nameof(SundayHeaderColor), typeof(Color), typeof(CalendarView), Color.FromArgb("#E53935"));

    // ------------------------------------------------------------------ CLR Properties

    public MonthViewModel? ViewModel
    {
        get => (MonthViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public ICommand? PrevMonthCommand
    {
        get => (ICommand?)GetValue(PrevMonthCommandProperty);
        set => SetValue(PrevMonthCommandProperty, value);
    }

    public ICommand? NextMonthCommand
    {
        get => (ICommand?)GetValue(NextMonthCommandProperty);
        set => SetValue(NextMonthCommandProperty, value);
    }

    public ICommand? GoToTodayCommand
    {
        get => (ICommand?)GetValue(GoToTodayCommandProperty);
        set => SetValue(GoToTodayCommandProperty, value);
    }

    public ICommand? DayTappedCommand
    {
        get => (ICommand?)GetValue(DayTappedCommandProperty);
        set => SetValue(DayTappedCommandProperty, value);
    }

    public ICommand? EventTappedCommand
    {
        get => (ICommand?)GetValue(EventTappedCommandProperty);
        set => SetValue(EventTappedCommandProperty, value);
    }

    public CalendarSelectionMode SelectionMode
    {
        get => (CalendarSelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    public DateOnly? SelectedDate
    {
        get => (DateOnly?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public ObservableCollection<DateOnly>? SelectedDates
    {
        get => (ObservableCollection<DateOnly>?)GetValue(SelectedDatesProperty);
        set => SetValue(SelectedDatesProperty, value);
    }

    public DateOnly? SelectedStartDate
    {
        get => (DateOnly?)GetValue(SelectedStartDateProperty);
        set => SetValue(SelectedStartDateProperty, value);
    }

    public DateOnly? SelectedEndDate
    {
        get => (DateOnly?)GetValue(SelectedEndDateProperty);
        set => SetValue(SelectedEndDateProperty, value);
    }

    public Color SelectedDayBackground { get => (Color)GetValue(SelectedDayBackgroundProperty); set => SetValue(SelectedDayBackgroundProperty, value); }
    public Color SelectedDayTextColor  { get => (Color)GetValue(SelectedDayTextColorProperty);  set => SetValue(SelectedDayTextColorProperty, value); }
    public Color RangeBackground       { get => (Color)GetValue(RangeBackgroundProperty);       set => SetValue(RangeBackgroundProperty, value); }

    public DateOnly? MinDate { get => (DateOnly?)GetValue(MinDateProperty); set => SetValue(MinDateProperty, value); }
    public DateOnly? MaxDate { get => (DateOnly?)GetValue(MaxDateProperty); set => SetValue(MaxDateProperty, value); }
    public Color DisabledDayTextColor  { get => (Color)GetValue(DisabledDayTextColorProperty);  set => SetValue(DisabledDayTextColorProperty, value); }

    public CultureInfo? Culture
    {
        get => (CultureInfo?)GetValue(CultureProperty);
        set => SetValue(CultureProperty, value);
    }

    public double DateRowHeight        { get => (double)GetValue(DateRowHeightProperty);        set => SetValue(DateRowHeightProperty, value); }
    public double SlotRowHeight        { get => (double)GetValue(SlotRowHeightProperty);        set => SetValue(SlotRowHeightProperty, value); }
    public double DateNumberSize       { get => (double)GetValue(DateNumberSizeProperty);       set => SetValue(DateNumberSizeProperty, value); }
    public Thickness DateNumberMargin  { get => (Thickness)GetValue(DateNumberMarginProperty);  set => SetValue(DateNumberMarginProperty, value); }
    public double DateNumberFontSize   { get => (double)GetValue(DateNumberFontSizeProperty);   set => SetValue(DateNumberFontSizeProperty, value); }
    public double EventFontSize        { get => (double)GetValue(EventFontSizeProperty);        set => SetValue(EventFontSizeProperty, value); }
    public double EventRowHeight       { get => (double)GetValue(EventRowHeightProperty);       set => SetValue(EventRowHeightProperty, value); }
    public double StampMarginEdge      { get => (double)GetValue(StampMarginEdgeProperty);      set => SetValue(StampMarginEdgeProperty, value); }
    public double NavButtonWidth       { get => (double)GetValue(NavButtonWidthProperty);       set => SetValue(NavButtonWidthProperty, value); }
    public double NavButtonHeight      { get => (double)GetValue(NavButtonHeightProperty);      set => SetValue(NavButtonHeightProperty, value); }
    public double NavButtonFontSize    { get => (double)GetValue(NavButtonFontSizeProperty);    set => SetValue(NavButtonFontSizeProperty, value); }
    public DayOfWeek FirstDayOfWeek    { get => (DayOfWeek)GetValue(FirstDayOfWeekProperty);   set => SetValue(FirstDayOfWeekProperty, value); }
    public bool SwipeEnabled           { get => (bool)GetValue(SwipeEnabledProperty);           set => SetValue(SwipeEnabledProperty, value); }
    public Thickness HeaderPadding     { get => (Thickness)GetValue(HeaderPaddingProperty);     set => SetValue(HeaderPaddingProperty, value); }
    public double WeekdayHeaderFontSize{ get => (double)GetValue(WeekdayHeaderFontSizeProperty);set => SetValue(WeekdayHeaderFontSizeProperty, value); }
    public Thickness WeekdayHeaderPadding{ get => (Thickness)GetValue(WeekdayHeaderPaddingProperty); set => SetValue(WeekdayHeaderPaddingProperty, value); }
    public double YearFontSize         { get => (double)GetValue(YearFontSizeProperty);         set => SetValue(YearFontSizeProperty, value); }
    public double MonthFontSize        { get => (double)GetValue(MonthFontSizeProperty);        set => SetValue(MonthFontSizeProperty, value); }

    public Color GridLineColor         { get => (Color)GetValue(GridLineColorProperty);         set => SetValue(GridLineColorProperty, value); }
    public Color WeekdayTextColor      { get => (Color)GetValue(WeekdayTextColorProperty);      set => SetValue(WeekdayTextColorProperty, value); }
    public Color SaturdayTextColor     { get => (Color)GetValue(SaturdayTextColorProperty);     set => SetValue(SaturdayTextColorProperty, value); }
    public Color SundayTextColor       { get => (Color)GetValue(SundayTextColorProperty);       set => SetValue(SundayTextColorProperty, value); }
    public Color OutsideMonthTextColor { get => (Color)GetValue(OutsideMonthTextColorProperty); set => SetValue(OutsideMonthTextColorProperty, value); }
    public Color OutsideMonthBackground{ get => (Color)GetValue(OutsideMonthBackgroundProperty);set => SetValue(OutsideMonthBackgroundProperty, value); }
    public Color WeekendBackground     { get => (Color)GetValue(WeekendBackgroundProperty);     set => SetValue(WeekendBackgroundProperty, value); }
    public Color TodayBackground       { get => (Color)GetValue(TodayBackgroundProperty);       set => SetValue(TodayBackgroundProperty, value); }
    public Color TodayTextColor        { get => (Color)GetValue(TodayTextColorProperty);        set => SetValue(TodayTextColorProperty, value); }
    public Color NavButtonColor        { get => (Color)GetValue(NavButtonColorProperty);        set => SetValue(NavButtonColorProperty, value); }
    public Color YearTextColor         { get => (Color)GetValue(YearTextColorProperty);         set => SetValue(YearTextColorProperty, value); }
    public Color MonthTextColor        { get => (Color)GetValue(MonthTextColorProperty);        set => SetValue(MonthTextColorProperty, value); }
    public Color WeekdayHeaderColor    { get => (Color)GetValue(WeekdayHeaderColorProperty);    set => SetValue(WeekdayHeaderColorProperty, value); }
    public Color SaturdayHeaderColor   { get => (Color)GetValue(SaturdayHeaderColorProperty);   set => SetValue(SaturdayHeaderColorProperty, value); }
    public Color SundayHeaderColor     { get => (Color)GetValue(SundayHeaderColorProperty);     set => SetValue(SundayHeaderColorProperty, value); }

    // ------------------------------------------------------------------ Constructor

    private const int DaysPerWeek = 7;
    private const int MaxWeekRows = 6;

    // Pre-allocated row containers reused across month renders to avoid GC pressure.
    private readonly Grid[] weekRowContainers = new Grid[MaxWeekRows];

    // Direction of the last month navigation: +1 = forward (next), -1 = backward (prev), 0 = jump
    private int lastNavDirection;
    private int previousYear;
    private int previousMonth;

    public CalendarView()
    {
        InitializeComponent();

        // Pre-create the six week-row containers and insert them into the fixed WeeksHost rows.
        for (var i = 0; i < MaxWeekRows; i++)
        {
            var container = new Grid { RowSpacing = 0, ColumnSpacing = 0 };
            Grid.SetRow(container, i);
            WeeksHost.Children.Add(container);
            weekRowContainers[i] = container;
        }

        AttachSwipeGestures();
    }

    // ------------------------------------------------------------------ Property changed callbacks

    private static void OnViewModelChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CalendarView view && newValue is MonthViewModel month)
            view.Render(month);
    }

    private static void OnRenderPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CalendarView view && view.ViewModel is MonthViewModel month)
            view.Render(month);
    }

    private static void OnFirstDayOfWeekChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CalendarView view)
        {
            view.UpdateWeekdayHeaderLabels();
            if (view.ViewModel is MonthViewModel month)
                view.Render(month);
        }
    }

    private static void OnSwipeEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CalendarView view)
            view.UpdateSwipeGestureState((bool)newValue);
    }

    private static void OnSelectionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CalendarView view && view.ViewModel is MonthViewModel month)
            view.RebuildWeeksHostOnly(month);
    }

    private static void OnSelectedDatesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (CalendarView)bindable;
        if (oldValue is ObservableCollection<DateOnly> old)
            old.CollectionChanged -= view.OnSelectedDatesCollectionChanged;
        if (newValue is ObservableCollection<DateOnly> next)
            next.CollectionChanged += view.OnSelectedDatesCollectionChanged;
        if (view.ViewModel is MonthViewModel month)
            view.RebuildWeeksHostOnly(month);
    }

    private static void OnCultureChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CalendarView view)
        {
            view.UpdateWeekdayHeaderLabels();
            if (view.ViewModel is MonthViewModel month)
                view.Render(month);
        }
    }

    private void OnSelectedDatesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (ViewModel is MonthViewModel month)
            RebuildWeeksHostOnly(month);
    }

    // ------------------------------------------------------------------ Render

    private void Render(MonthViewModel month)
    {
        // Determine navigation direction from previous displayed month.
        // lastNavDirection is pre-set by swipe; otherwise auto-detect from date comparison.
        if (lastNavDirection == 0 && (previousYear != 0))
        {
            var prevOrdinal = (previousYear * 12) + previousMonth;
            var newOrdinal  = (month.Year  * 12) + month.Month;
            lastNavDirection = newOrdinal > prevOrdinal ? 1 : newOrdinal < prevOrdinal ? -1 : 0;
        }

        previousYear  = month.Year;
        previousMonth = month.Month;
        YearLabel.Text = month.Year.ToString(CultureInfo.InvariantCulture);
        YearLabel.FontSize = YearFontSize;
        YearLabel.TextColor = YearTextColor;
        MonthLabel.Text = GetMonthDisplayName(month.Year, month.Month);
        MonthLabel.FontSize = MonthFontSize;
        MonthLabel.TextColor = MonthTextColor;

        PrevButton.TextColor = NavButtonColor;
        PrevButton.FontSize = NavButtonFontSize;
        PrevButton.WidthRequest = NavButtonWidth;
        PrevButton.HeightRequest = NavButtonHeight;
        NextButton.TextColor = NavButtonColor;
        NextButton.FontSize = NavButtonFontSize;
        NextButton.WidthRequest = NavButtonWidth;
        NextButton.HeightRequest = NavButtonHeight;
        TodayButton.FontSize = NavButtonFontSize;
        TodayButton.TextColor = NavButtonColor;

        HeaderGrid.Padding = HeaderPadding;
        WeekdayHeaderGrid.Padding = WeekdayHeaderPadding;
        UpdateWeekdayHeaderLabels();

        var slotCount = Math.Max(2, month.Weeks.Count > 0
            ? month.Weeks.Max(static w => w.SlotCount)
            : 0);

        RebuildWeeksHost(month, slotCount);
        AnimateSlideAsync(lastNavDirection).FireAndForget();
        lastNavDirection = 0;
    }

    private void RebuildWeeksHost(MonthViewModel month, int slotCount)
    {
        var weekCount = month.Weeks.Count;
        for (var i = 0; i < MaxWeekRows; i++)
        {
            var container = weekRowContainers[i];
            container.Children.Clear();
            container.IsVisible = i < weekCount;
            if (i < weekCount)
            {
                var weekView = BuildWeekRow(month.Weeks[i], slotCount);
                container.Children.Add(weekView);
            }
        }
    }

    // Rebuild only the week cells (no animation). Used when only selection/color state changes.
    private void RebuildWeeksHostOnly(MonthViewModel month)
    {
        var slotCount = Math.Max(2, month.Weeks.Count > 0
            ? month.Weeks.Max(static w => w.SlotCount)
            : 0);
        RebuildWeeksHost(month, slotCount);
    }

    private async Task AnimateSlideAsync(int direction)
    {
        if (direction == 0)
            return;

        var width = WeeksHost.Width > 0 ? WeeksHost.Width : 400d;
        WeeksHost.TranslationX = direction * width;
        await WeeksHost.TranslateToAsync(0, 0, 250, Easing.CubicOut);
    }

    private void UpdateWeekdayHeaderLabels()
    {
        WeekdayHeaderGrid.Children.Clear();
        var start = (int)FirstDayOfWeek;
        for (var i = 0; i < DaysPerWeek; i++)
        {
            var dayOfWeek = (DayOfWeek)((start + i) % DaysPerWeek);
            var label = new Label
            {
                Text = GetDayOfWeekShortName(dayOfWeek),
                FontSize = WeekdayHeaderFontSize,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = dayOfWeek == DayOfWeek.Saturday ? SaturdayHeaderColor
                          : dayOfWeek == DayOfWeek.Sunday   ? SundayHeaderColor
                          : WeekdayHeaderColor,
            };
            Grid.SetColumn(label, i);
            WeekdayHeaderGrid.Children.Add(label);
        }
    }

    private string GetDayOfWeekShortName(DayOfWeek dow)
    {
        var culture = Culture;
        if (culture is not null)
        {
            // Use the culture's abbreviated day name, truncated to 1 char for compact display.
            var abbreviated = culture.DateTimeFormat.GetAbbreviatedDayName(dow);
            return abbreviated.Length > 0 ? abbreviated[..1].ToUpper(culture) : abbreviated;
        }

        return dow switch
        {
            DayOfWeek.Monday    => "月",
            DayOfWeek.Tuesday   => "火",
            DayOfWeek.Wednesday => "水",
            DayOfWeek.Thursday  => "木",
            DayOfWeek.Friday    => "金",
            DayOfWeek.Saturday  => "土",
            DayOfWeek.Sunday    => "日",
            _ => string.Empty,
        };
    }

    private string GetMonthDisplayName(int year, int month)
    {
        var culture = Culture;
        if (culture is not null)
        {
            var dt = new DateTime(year, month, 1);
            return culture.DateTimeFormat.GetMonthName(month).ToUpper(culture);
        }
        return $"{month}\u6708";
    }

    private View BuildWeekRow(WeekViewModel week, int slotCount)
    {
        var totalRows = 2 + slotCount;
        var grid = new Grid
        {
            ColumnSpacing = 0,
            RowSpacing = 0,
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill,
        };
        for (var i = 0; i < DaysPerWeek; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(DateRowHeight)));
        for (var i = 0; i < slotCount; i++)
            grid.RowDefinitions.Add(new RowDefinition(new GridLength(SlotRowHeight)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

        // Per-column background
        for (var c = 0; c < DaysPerWeek; c++)
        {
            var day = week.Days[c];
            var bg = GetCellBackgroundColor(day);
            if (bg != Colors.Transparent)
            {
                var box = new BoxView { Color = bg, InputTransparent = true };
                Grid.SetColumn(box, c);
                Grid.SetRow(box, 0);
                Grid.SetRowSpan(box, totalRows);
                grid.Children.Add(box);
            }

            // Range highlight (behind date bubble, full cell height)
            var rangeBg = GetRangeCellBackground(day.Date);
            if (rangeBg != Colors.Transparent)
            {
                var rangeBox = new BoxView { Color = rangeBg, InputTransparent = true };
                Grid.SetColumn(rangeBox, c);
                Grid.SetRow(rangeBox, 0);
                Grid.SetRowSpan(rangeBox, totalRows);
                grid.Children.Add(rangeBox);
            }
        }

        // Top divider
        var topDivider = new BoxView
        {
            HeightRequest = 0.5,
            Color = GridLineColor,
            VerticalOptions = LayoutOptions.Start,
            InputTransparent = true,
        };
        Grid.SetRow(topDivider, 0);
        Grid.SetColumnSpan(topDivider, DaysPerWeek);
        grid.Children.Add(topDivider);

        // Vertical dividers
        for (var c = 0; c < DaysPerWeek - 1; c++)
        {
            var vd = new BoxView
            {
                WidthRequest = 0.5,
                Color = GridLineColor,
                HorizontalOptions = LayoutOptions.End,
                InputTransparent = true,
            };
            Grid.SetColumn(vd, c);
            Grid.SetRow(vd, 0);
            Grid.SetRowSpan(vd, totalRows);
            grid.Children.Add(vd);
        }

        // Cell tap targets
        for (var c = 0; c < DaysPerWeek; c++)
        {
            var day = week.Days[c];
            var disabled = IsDateDisabled(day.Date);
            var tappable = new Border { BackgroundColor = Colors.Transparent, StrokeThickness = 0 };
            if (!disabled)
            {
                tappable.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command<DayViewModel>(OnDayTapped),
                    CommandParameter = day,
                });
            }
            Grid.SetColumn(tappable, c);
            Grid.SetRow(tappable, 0);
            Grid.SetRowSpan(tappable, totalRows);
            grid.Children.Add(tappable);
        }

        // Date numbers
        for (var c = 0; c < DaysPerWeek; c++)
        {
            var dv = BuildDateNumberView(week.Days[c]);
            Grid.SetColumn(dv, c);
            Grid.SetRow(dv, 0);
            grid.Children.Add(dv);
        }

        // Stamps
        for (var c = 0; c < DaysPerWeek; c++)
        {
            foreach (var stamp in week.Days[c].Stamps)
            {
                var sv = BuildStampView(stamp);
                Grid.SetColumn(sv, c);
                Grid.SetRow(sv, 0);
                Grid.SetRowSpan(sv, totalRows);
                grid.Children.Add(sv);
            }
        }

        // Event placements
        foreach (var placement in week.EventPlacements)
        {
            var ev = BuildEventView(placement);
            Grid.SetColumn(ev, placement.StartColumn);
            Grid.SetColumnSpan(ev, placement.ColumnSpan);
            Grid.SetRow(ev, placement.Slot + 1);
            grid.Children.Add(ev);
        }

        return grid;
    }

    // ------------------------------------------------------------------ View builders

    private View BuildDateNumberView(DayViewModel day)
    {
        var disabled = IsDateDisabled(day.Date);
        var selected = IsDateSelected(day.Date);

        Color textColor;
        Color bubbleBg;

        if (disabled)
        {
            textColor = DisabledDayTextColor;
            bubbleBg  = Colors.Transparent;
        }
        else if (selected)
        {
            textColor = SelectedDayTextColor;
            bubbleBg  = SelectedDayBackground;
        }
        else if (day.IsToday)
        {
            textColor = TodayTextColor;
            bubbleBg  = TodayBackground;
        }
        else
        {
            textColor = GetDateTextColor(day);
            bubbleBg  = Colors.Transparent;
        }

        var label = new Label
        {
            Text = day.Date.Day.ToString(CultureInfo.InvariantCulture),
            FontSize = DateNumberFontSize,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            TextColor = textColor,
            WidthRequest = DateNumberSize,
            HeightRequest = DateNumberSize,
        };
        var bubble = new Border
        {
            BackgroundColor = bubbleBg,
            StrokeThickness = 0,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            Margin = DateNumberMargin,
            Padding = 0,
            Content = label,
        };
        if (bubbleBg != Colors.Transparent)
            bubble.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(2) };
        return bubble;
    }

    private View BuildStampView(Stamp stamp)
    {
        var m = StampMarginEdge;
        var label = new Label
        {
            Text = stamp.Glyph,
            FontSize = stamp.FontSize,
            Opacity = stamp.Opacity,
            InputTransparent = true,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        (label.HorizontalOptions, label.VerticalOptions, label.Margin) = stamp.Position switch
        {
            StampPosition.TopLeft      => (LayoutOptions.Start,  LayoutOptions.Start, new Thickness(m, 0, 0, 0)),
            StampPosition.TopCenter    => (LayoutOptions.Center, LayoutOptions.Start, new Thickness(0)),
            StampPosition.TopRight     => (LayoutOptions.End,    LayoutOptions.Start, new Thickness(0, 0, m, 0)),
            StampPosition.BottomLeft   => (LayoutOptions.Start,  LayoutOptions.End,   new Thickness(m, 0, 0, m)),
            StampPosition.BottomCenter => (LayoutOptions.Center, LayoutOptions.End,   new Thickness(0, 0, 0, m)),
            StampPosition.BottomRight  => (LayoutOptions.End,    LayoutOptions.End,   new Thickness(0, 0, m, m)),
            _                          => (LayoutOptions.Center, LayoutOptions.Center, new Thickness(0)),
        };
        return label;
    }

    private View BuildEventView(EventPlacement placement)
    {
        var evt = placement.Event;
        var label = new Label
        {
            Text = evt.Title,
            FontSize = EventFontSize,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.TailTruncation,
            VerticalTextAlignment = TextAlignment.Center,
            TextDecorations = evt.Underline ? TextDecorations.Underline : TextDecorations.None,
            TextColor = evt.TextColor,
        };
        Border border;
        if (evt.Style == ScheduleStyle.Filled)
        {
            var left = placement.ContinuesFromPreviousWeek ? 0 : 2;
            var right = placement.ContinuesToNextWeek ? 0 : 2;
            border = new Border
            {
                BackgroundColor = evt.BackgroundColor,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(left, right, left, right) },
                Padding = new Thickness(4, 0),
                HeightRequest = EventRowHeight,
                Margin = new Thickness(
                    placement.ContinuesFromPreviousWeek ? 0 : 1, 1,
                    placement.ContinuesToNextWeek ? 0 : 1, 0),
                Content = label,
            };
        }
        else
        {
            border = new Border
            {
                BackgroundColor = Colors.Transparent,
                StrokeThickness = 0,
                Padding = new Thickness(4, 0),
                HeightRequest = EventRowHeight,
                Margin = new Thickness(1, 1, 1, 0),
                Content = label,
            };
        }
        border.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command<ScheduleEvent>(OnEventTapped),
            CommandParameter = evt,
        });
        return border;
    }

    // ------------------------------------------------------------------ Color helpers

    private Color GetCellBackgroundColor(DayViewModel day)
    {
        if (!day.IsCurrentMonth) return OutsideMonthBackground;
        return day.Kind switch
        {
            DayKind.Saturday or DayKind.Sunday or DayKind.Holiday => WeekendBackground,
            _ => Colors.Transparent,
        };
    }

    private Color GetDateTextColor(DayViewModel day)
    {
        if (!day.IsCurrentMonth) return OutsideMonthTextColor;
        return day.Kind switch
        {
            DayKind.Sunday or DayKind.Holiday => SundayTextColor,
            DayKind.Saturday => SaturdayTextColor,
            _ => WeekdayTextColor,
        };
    }

    // ------------------------------------------------------------------ Selection / disabled helpers

    private bool IsDateDisabled(DateOnly date)
    {
        if (MinDate is DateOnly min && date < min) return true;
        if (MaxDate is DateOnly max && date > max) return true;
        return false;
    }

    private bool IsDateSelected(DateOnly date) => SelectionMode switch
    {
        CalendarSelectionMode.Single    => SelectedDate == date,
        CalendarSelectionMode.Multiple  => SelectedDates?.Contains(date) == true,
        CalendarSelectionMode.Range     => IsInRange(date, endpoints: true),
        _ => false,
    };

    // Returns true for the interior of the range (excluding endpoints, which are drawn as full bubbles).
    private Color GetRangeCellBackground(DateOnly date)
    {
        if (SelectionMode != CalendarSelectionMode.Range)
            return Colors.Transparent;
        if (SelectedStartDate is not DateOnly start || SelectedEndDate is not DateOnly end)
            return Colors.Transparent;
        if (start > end) (start, end) = (end, start);
        return (date > start && date < end) ? RangeBackground : Colors.Transparent;
    }

    private bool IsInRange(DateOnly date, bool endpoints)
    {
        if (SelectedStartDate is not DateOnly start || SelectedEndDate is not DateOnly end)
            return false;
        if (start > end) (start, end) = (end, start);
        return endpoints ? (date >= start && date <= end) : (date > start && date < end);
    }

    // ------------------------------------------------------------------ Swipe gestures (PanGestureRecognizer for reliable Android detection)

    // Minimum horizontal distance (dp) to trigger a month change.
    private const double SwipeThreshold = 80d;
    // If the vertical drift exceeds this ratio of horizontal movement, ignore (user is scrolling vertically).
    private const double SwipeMaxVerticalRatio = 0.6d;

    private PanGestureRecognizer? panGesture;
    private double panStartX;
    private double panStartY;
    private bool panConsumed;

    private void AttachSwipeGestures()
    {
        panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        UpdateSwipeGestureState(SwipeEnabled);
    }

    private void UpdateSwipeGestureState(bool enabled)
    {
        if (panGesture is null)
            return;

        RootGrid.GestureRecognizers.Remove(panGesture);
        if (enabled)
            RootGrid.GestureRecognizers.Add(panGesture);
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                panStartX  = e.TotalX;
                panStartY  = e.TotalY;
                panConsumed = false;
                break;

            case GestureStatus.Running:
                if (panConsumed)
                    break;
                var dx = e.TotalX - panStartX;
                var dy = e.TotalY - panStartY;
                var absDx = Math.Abs(dx);
                var absDy = Math.Abs(dy);
                // Ignore if vertical motion dominates.
                if (absDy > absDx * (1d / SwipeMaxVerticalRatio))
                    break;
                if (absDx >= SwipeThreshold)
                {
                    panConsumed = true;
                    if (dx < 0)
                    {
                        lastNavDirection = 1;
                        NextMonthCommand?.Execute(null);
                    }
                    else
                    {
                        lastNavDirection = -1;
                        PrevMonthCommand?.Execute(null);
                    }
                }
                break;
        }
    }

    // ------------------------------------------------------------------ Tap handlers

    private void OnDayTapped(DayViewModel day)
    {
        var date = day.Date;
        switch (SelectionMode)
        {
            case CalendarSelectionMode.Single:
                SelectedDate = SelectedDate == date ? null : date;
                break;

            case CalendarSelectionMode.Multiple:
                if (SelectedDates is null)
                    SelectedDates = new ObservableCollection<DateOnly>();
                var list = SelectedDates;
                if (list.Contains(date))
                    list.Remove(date);
                else
                    list.Add(date);
                break;

            case CalendarSelectionMode.Range:
                if (SelectedStartDate is null || (SelectedStartDate is not null && SelectedEndDate is not null))
                {
                    // Start a new range.
                    SelectedStartDate = date;
                    SelectedEndDate = null;
                }
                else
                {
                    // Complete the range.
                    if (date < SelectedStartDate)
                        (SelectedStartDate, SelectedEndDate) = (date, SelectedStartDate);
                    else
                        SelectedEndDate = date;
                }
                break;
        }

        DayTappedCommand?.Execute(day);
    }

    private void OnEventTapped(ScheduleEvent evt) => EventTappedCommand?.Execute(evt);
}

file static class TaskExtensions
{
    // Fire-and-forget helper that surfaces exceptions to the unhandled exception handler.
    internal static void FireAndForget(this Task task) =>
        task.ContinueWith(static t => throw t.Exception!.GetBaseException(),
            TaskContinuationOptions.OnlyOnFaulted);
}
