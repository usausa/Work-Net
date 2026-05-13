namespace WorkCalendar;

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

using Microsoft.Maui.Controls.Shapes;

using WorkCalendar.Models;
using WorkCalendar.Services;
using WorkCalendar.ViewModels;

public partial class MainPage : ContentPage
{
    private const int DaysPerWeek = 7;
    private const double DateRowHeight = 26;
    private const double SlotRowHeight = 17;

    private static readonly Color GridLineColor = Color.FromArgb("#E0E0E0");
    private static readonly Color WeekdayTextColor = Color.FromArgb("#1F1F1F");
    private static readonly Color SaturdayTextColor = Color.FromArgb("#2196F3");
    private static readonly Color SundayTextColor = Color.FromArgb("#E53935");
    private static readonly Color OutsideMonthTextColor = Color.FromArgb("#BDBDBD");
    private static readonly Color OutsideMonthBackground = Color.FromArgb("#F2F2F2");
    private static readonly Color WeekendBackground = Color.FromArgb("#FFF1F1");

    private static readonly DateOnly ReferenceToday = DateOnly.FromDateTime(DateTime.Today);

    private readonly MonthViewBuilder builder = new(DayOfWeek.Monday);
    private readonly IScheduleService scheduleService = new ScheduleService();
    private readonly IHolidayService holidayService = new HolidayService();

    private int currentYear;
    private int currentMonth;

    public MainPage()
    {
        InitializeComponent();

        PrevMonthLabel.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(OnPrevMonthTapped),
        });
        NextMonthLabel.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(OnNextMonthTapped),
        });

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
        // Phase 1: compute the display date range from the day layout.
        var (rangeStart, rangeEnd) = builder.GetDisplayRange(year, month);

        // Phase 2: fetch schedule data and holidays for that range via services.
        var events = scheduleService.GetEvents(rangeStart, rangeEnd);
        var stamps = scheduleService.GetStamps(rangeStart, rangeEnd);
        var holidays = holidayService.GetHolidays(rangeStart, rangeEnd);

        // Phase 3: build the model and render.
        var monthModel = builder.Build(year, month, ReferenceToday, events, stamps, holidays);
        Render(monthModel);
    }

    private void Render(MonthViewModel month)
    {
        YearLabel.Text = month.Year.ToString(CultureInfo.InvariantCulture);
        MonthLabel.Text = $"{month.Month}月";

        var slotCount = Math.Max(2, month.Weeks.Max(static w => w.SlotCount));

        WeeksHost.Children.Clear();
        for (var i = 0; i < month.Weeks.Count; i++)
        {
            var weekView = BuildWeekRow(month.Weeks[i], slotCount);
            Grid.SetRow(weekView, i);
            WeeksHost.Children.Add(weekView);
        }
    }

    private View BuildWeekRow(WeekViewModel week, int slotCount)
    {
        // Row layout: 1 date row (fixed) + slotCount slot rows (fixed) + 1 star filler row.
        // The star filler absorbs any extra height so the week always fills its 1/6 share
        // of the body area without leaving a gap above the next week.
        var totalRows = 2 + slotCount;
        var grid = new Grid
        {
            ColumnSpacing = 0,
            RowSpacing = 0,
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill,
        };
        for (var i = 0; i < DaysPerWeek; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(DateRowHeight)));
        for (var i = 0; i < slotCount; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(new GridLength(SlotRowHeight)));
        }
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

        // Per-column background (outside-month gray, weekend/holiday reddish).
        for (var c = 0; c < DaysPerWeek; c++)
        {
            var day = week.Days[c];
            var background = GetCellBackgroundColor(day);
            if (background != Colors.Transparent)
            {
                var bg = new BoxView
                {
                    Color = background,
                    InputTransparent = true,
                };
                Grid.SetColumn(bg, c);
                Grid.SetRow(bg, 0);
                Grid.SetRowSpan(bg, totalRows);
                grid.Children.Add(bg);
            }
        }

        // Top divider line of the week (horizontal grid line between weeks).
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

        // Vertical dividers between day columns.
        for (var c = 0; c < DaysPerWeek - 1; c++)
        {
            var vDivider = new BoxView
            {
                WidthRequest = 0.5,
                Color = GridLineColor,
                HorizontalOptions = LayoutOptions.End,
                InputTransparent = true,
            };
            Grid.SetColumn(vDivider, c);
            Grid.SetRow(vDivider, 0);
            Grid.SetRowSpan(vDivider, totalRows);
            grid.Children.Add(vDivider);
        }

        // Cell tap targets that span the whole cell area.
        for (var c = 0; c < DaysPerWeek; c++)
        {
            var day = week.Days[c];
            var tappable = new Border
            {
                BackgroundColor = Colors.Transparent,
                StrokeThickness = 0,
            };
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

        // Date numbers (with today highlight).
        for (var c = 0; c < DaysPerWeek; c++)
        {
            var day = week.Days[c];
            var dateView = BuildDateNumberView(day);
            Grid.SetColumn(dateView, c);
            Grid.SetRow(dateView, 0);
            grid.Children.Add(dateView);
        }

        // Stamps overlay (full cell height).
        for (var c = 0; c < DaysPerWeek; c++)
        {
            var day = week.Days[c];
            foreach (var stamp in day.Stamps)
            {
                var stampView = BuildStampView(stamp);
                Grid.SetColumn(stampView, c);
                Grid.SetRow(stampView, 0);
                Grid.SetRowSpan(stampView, totalRows);
                grid.Children.Add(stampView);
            }
        }

        // Schedule events placed by slot.
        foreach (var placement in week.EventPlacements)
        {
            var eventView = BuildEventView(placement);
            Grid.SetColumn(eventView, placement.StartColumn);
            Grid.SetColumnSpan(eventView, placement.ColumnSpan);
            Grid.SetRow(eventView, placement.Slot + 1);
            grid.Children.Add(eventView);
        }

        return grid;
    }

    private static Color GetCellBackgroundColor(DayViewModel day)
    {
        if (!day.IsCurrentMonth)
        {
            return OutsideMonthBackground;
        }
        return day.Kind switch
        {
            DayKind.Saturday or DayKind.Sunday or DayKind.Holiday => WeekendBackground,
            _ => Colors.Transparent,
        };
    }

    private static View BuildDateNumberView(DayViewModel day)
    {
        var label = new Label
        {
            Text = day.Date.Day.ToString(CultureInfo.InvariantCulture),
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            TextColor = day.IsToday ? Colors.White : GetDateTextColor(day),
            WidthRequest = 22,
            HeightRequest = 22,
        };

        var bubble = new Border
        {
            BackgroundColor = day.IsToday ? Colors.Black : Colors.Transparent,
            StrokeThickness = 0,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(4, 2, 0, 0),
            Padding = 0,
            Content = label,
        };

        if (day.IsToday)
        {
            bubble.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(2) };
        }

        return bubble;
    }

    private static View BuildStampView(Stamp stamp)
    {
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
            StampPosition.TopLeft => (LayoutOptions.Start, LayoutOptions.Start, new Thickness(2, 0, 0, 0)),
            StampPosition.TopCenter => (LayoutOptions.Center, LayoutOptions.Start, new Thickness(0)),
            StampPosition.TopRight => (LayoutOptions.End, LayoutOptions.Start, new Thickness(0, 0, 2, 0)),
            StampPosition.BottomLeft => (LayoutOptions.Start, LayoutOptions.End, new Thickness(2, 0, 0, 2)),
            StampPosition.BottomCenter => (LayoutOptions.Center, LayoutOptions.End, new Thickness(0, 0, 0, 2)),
            StampPosition.BottomRight => (LayoutOptions.End, LayoutOptions.End, new Thickness(0, 0, 2, 2)),
            _ => (LayoutOptions.Center, LayoutOptions.Center, new Thickness(0)),
        };

        return label;
    }

    private static View BuildEventView(EventPlacement placement)
    {
        var evt = placement.Event;
        var label = new Label
        {
            Text = evt.Title,
            FontSize = 11,
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
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(left, right, left, right),
                },
                Padding = new Thickness(4, 0),
                HeightRequest = 15,
                Margin = new Thickness(
                    placement.ContinuesFromPreviousWeek ? 0 : 1,
                    1,
                    placement.ContinuesToNextWeek ? 0 : 1,
                    0),
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
                HeightRequest = 15,
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

    private static Color GetDateTextColor(DayViewModel day)
    {
        if (!day.IsCurrentMonth)
        {
            return OutsideMonthTextColor;
        }
        return day.Kind switch
        {
            DayKind.Sunday or DayKind.Holiday => SundayTextColor,
            DayKind.Saturday => SaturdayTextColor,
            _ => WeekdayTextColor,
        };
    }

    private static void OnDayTapped(DayViewModel day) =>
        Debug.WriteLine($"Day tapped: {day.Date:yyyy-MM-dd}");

    private static void OnEventTapped(ScheduleEvent evt) =>
        Debug.WriteLine($"Event tapped: {evt.Id} {evt.Title}");
}
