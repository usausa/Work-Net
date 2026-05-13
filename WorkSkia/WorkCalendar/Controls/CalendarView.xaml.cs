namespace WorkCalendar.Controls;

using System;
using System.Globalization;
using System.Linq;
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

    public static readonly BindableProperty DayTappedCommandProperty =
        BindableProperty.Create(nameof(DayTappedCommand), typeof(ICommand), typeof(CalendarView));

    public static readonly BindableProperty EventTappedCommandProperty =
        BindableProperty.Create(nameof(EventTappedCommand), typeof(ICommand), typeof(CalendarView));

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

    public CalendarView()
    {
        InitializeComponent();
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

    // ------------------------------------------------------------------ Render

    private void Render(MonthViewModel month)
    {
        YearLabel.Text = month.Year.ToString(CultureInfo.InvariantCulture);
        YearLabel.FontSize = YearFontSize;
        YearLabel.TextColor = YearTextColor;
        MonthLabel.Text = $"{month.Month}\u6708";
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

        HeaderGrid.Padding = HeaderPadding;
        WeekdayHeaderGrid.Padding = WeekdayHeaderPadding;
        UpdateWeekdayHeaderColors();

        var slotCount = Math.Max(2, month.Weeks.Max(static w => w.SlotCount));

        WeeksHost.Children.Clear();
        for (var i = 0; i < month.Weeks.Count; i++)
        {
            var weekView = BuildWeekRow(month.Weeks[i], slotCount);
            Grid.SetRow(weekView, i);
            WeeksHost.Children.Add(weekView);
        }
    }

    private void UpdateWeekdayHeaderColors()
    {
        // Columns: 0=月 1=火 2=水 3=木 4=金 5=土 6=日
        var labels = WeekdayHeaderGrid.Children.OfType<Label>().ToList();
        foreach (var label in labels)
        {
            label.FontSize = WeekdayHeaderFontSize;
            var col = Grid.GetColumn(label);
            label.TextColor = col == 5 ? SaturdayHeaderColor
                            : col == 6 ? SundayHeaderColor
                            : WeekdayHeaderColor;
        }
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
            var bg = GetCellBackgroundColor(week.Days[c]);
            if (bg == Colors.Transparent) continue;
            var box = new BoxView { Color = bg, InputTransparent = true };
            Grid.SetColumn(box, c);
            Grid.SetRow(box, 0);
            Grid.SetRowSpan(box, totalRows);
            grid.Children.Add(box);
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
            var tappable = new Border { BackgroundColor = Colors.Transparent, StrokeThickness = 0 };
            tappable.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command<DayViewModel>(OnDayTapped),
                CommandParameter = day,
            });
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
        var label = new Label
        {
            Text = day.Date.Day.ToString(CultureInfo.InvariantCulture),
            FontSize = DateNumberFontSize,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            TextColor = day.IsToday ? TodayTextColor : GetDateTextColor(day),
            WidthRequest = DateNumberSize,
            HeightRequest = DateNumberSize,
        };
        var bubble = new Border
        {
            BackgroundColor = day.IsToday ? TodayBackground : Colors.Transparent,
            StrokeThickness = 0,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            Margin = DateNumberMargin,
            Padding = 0,
            Content = label,
        };
        if (day.IsToday)
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

    // ------------------------------------------------------------------ Tap handlers

    private void OnDayTapped(DayViewModel day) => DayTappedCommand?.Execute(day);
    private void OnEventTapped(ScheduleEvent evt) => EventTappedCommand?.Execute(evt);
}
