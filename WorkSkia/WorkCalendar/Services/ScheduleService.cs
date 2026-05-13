namespace WorkCalendar.Services;

using System;
using System.Collections.Generic;
using System.Linq;

using WorkCalendar.Models;

public sealed class ScheduleService : IScheduleService
{
    private static readonly Color DarkRed = Color.FromArgb("#8B1538");
    private static readonly Color HotPink = Color.FromArgb("#D81B60");
    private static readonly Color VividMagenta = Color.FromArgb("#C2185B");
    private static readonly Color Cyan = Color.FromArgb("#00ACC1");
    private static readonly Color Green = Color.FromArgb("#43A047");
    private static readonly Color GreenText = Color.FromArgb("#2E7D32");
    private static readonly Color Pink = Color.FromArgb("#EC407A");
    private static readonly Color PinkText = Color.FromArgb("#E91E63");
    private static readonly Color Yellow = Color.FromArgb("#FBC02D");
    private static readonly Color Orange = Color.FromArgb("#FB8C00");
    private static readonly Color Blue = Color.FromArgb("#1E88E5");
    private static readonly Color CyanText = Color.FromArgb("#00ACC1");
    private static readonly Color YellowText = Color.FromArgb("#F9A825");

    // 週次イベントのテンプレート (曜日, タイトル, スタイル, 色)
    private static readonly (DayOfWeek Dow, string Title, ScheduleStyle Style, Color Color, bool IsText)[] WeeklyTemplates =
    [
        (DayOfWeek.Monday, "週間報告", ScheduleStyle.Text, GreenText, true),
        (DayOfWeek.Monday, "英会話", ScheduleStyle.Text, PinkText, true),
        (DayOfWeek.Wednesday, "サークル", ScheduleStyle.Text, YellowText, true),
        (DayOfWeek.Saturday, "水泳教室", ScheduleStyle.Text, CyanText, true),
    ];

    // 月次固定イベントのテンプレート (月内日オフセット, タイトル, スパン日数, スタイル, 背景色, テキスト色)
    private static readonly (int DayOffset, string Title, int Span, ScheduleStyle Style, Color Bg, Color? Fg)[] MonthlyTemplates =
    [
        (3,  "燃えるゴ",  1, ScheduleStyle.Filled, DarkRed,     null),
        (10, "燃えるゴ",  1, ScheduleStyle.Filled, DarkRed,     null),
        (17, "燃えるゴ",  1, ScheduleStyle.Filled, DarkRed,     null),
        (24, "燃えるゴ",  1, ScheduleStyle.Filled, DarkRed,     null),
        (5,  "○ジム",    1, ScheduleStyle.Filled, Cyan,        null),
        (19, "○ジム",    1, ScheduleStyle.Filled, Cyan,        null),
    ];

    // スタンプのテンプレート (月内日オフセット, グリフ, 位置, サイズ, 不透明度)
    private static readonly (int DayOffset, string Glyph, StampPosition Position, int FontSize, double Opacity)[] StampTemplates =
    [
        (3,  "\U0001F6A9", StampPosition.TopRight,     22, 1.0),
        (8,  "\U0001F426", StampPosition.TopRight,     22, 1.0),
        (13, "✈️",         StampPosition.Center,       26, 1.0),
        (16, "\U0001F436", StampPosition.Center,       32, 0.9),
        (20, "\U0001F45B", StampPosition.TopCenter,    22, 1.0),
        (22, "\U0001F43C", StampPosition.TopLeft,      22, 1.0),
        (26, "\U0001F38F", StampPosition.TopCenter,    22, 1.0),
        (29, "\U0001F408", StampPosition.TopRight,     24, 1.0),
    ];

    // 臨時イベントのテンプレート (月内日オフセット, タイトル, スパン, スタイル, 背景色, テキスト色, 下線)
    private static readonly (int DayOffset, string Title, int Span, ScheduleStyle Style, Color Bg, Color? Fg, bool Underline)[] OccasionalTemplates =
    [
        (2,  "ぶどう狩",   1, ScheduleStyle.Filled, VividMagenta, null,         false),
        (6,  "会社研修",   2, ScheduleStyle.Filled, Green,        null,         false),
        (9,  "温泉旅行",   1, ScheduleStyle.Filled, Orange,       null,         false),
        (12, "大阪出張",   2, ScheduleStyle.Text,   Blue,         Blue,         true),
        (14, "友達泊まり", 3, ScheduleStyle.Filled, HotPink,      null,         false),
        (21, "買い物",     1, ScheduleStyle.Filled, Yellow,       Colors.Black, false),
        (26, "海外出張",   4, ScheduleStyle.Filled, Blue,         null,         false),
    ];

    public IReadOnlyList<ScheduleEvent> GetEvents(DateOnly start, DateOnly end)
    {
        var events = new List<ScheduleEvent>();
        var months = EnumerateMonths(start, end);

        var idx = 0;
        foreach (var (year, month) in months)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);

            // 週次イベント
            var first = new DateOnly(year, month, 1);
            for (var day = 1; day <= daysInMonth; day++)
            {
                var date = new DateOnly(year, month, day);
                foreach (var (dow, title, style, color, _) in WeeklyTemplates)
                {
                    if (date.DayOfWeek == dow)
                    {
                        var ev = CreateEvent($"w{idx++:D4}", title, date, date, style, Colors.Transparent, color);
                        if (ev.StartDate <= end && ev.EndDate >= start)
                            events.Add(ev);
                    }
                }
            }

            // 月次固定イベント
            foreach (var (offset, title, span, style, bg, fg) in MonthlyTemplates)
            {
                var day = Math.Min(offset, daysInMonth);
                var evStart = new DateOnly(year, month, day);
                var evEnd = evStart.AddDays(span - 1);
                if (evEnd.Month != month) evEnd = new DateOnly(year, month, daysInMonth);
                if (evStart <= end && evEnd >= start)
                    events.Add(CreateEvent($"m{idx++:D4}", title, evStart, evEnd, style, bg, fg ?? Colors.White));
            }

            // 臨時イベント (月ごとにローテーション)
            var occCount = OccasionalTemplates.Length;
            var pickCount = 3 + (month % 3);
            for (var i = 0; i < pickCount; i++)
            {
                var t = OccasionalTemplates[(month + i * 3) % occCount];
                var day = Math.Min(t.DayOffset, daysInMonth);
                var evStart = new DateOnly(year, month, day);
                var evEnd = evStart.AddDays(t.Span - 1);
                if (evEnd.Month != month) evEnd = new DateOnly(year, month, daysInMonth);
                if (evStart <= end && evEnd >= start)
                    events.Add(CreateEvent($"o{idx++:D4}", t.Title, evStart, evEnd, t.Style, t.Bg, t.Fg ?? Colors.White, t.Underline));
            }
        }

        return events.OrderBy(e => e.StartDate).ToList();
    }

    public IReadOnlyList<Stamp> GetStamps(DateOnly start, DateOnly end)
    {
        var stamps = new List<Stamp>();
        var months = EnumerateMonths(start, end);

        var idx = 0;
        foreach (var (year, month) in months)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var pickCount = 4 + (month % 4);
            for (var i = 0; i < pickCount; i++)
            {
                var t = StampTemplates[(month + i * 2) % StampTemplates.Length];
                var day = Math.Min(t.DayOffset, daysInMonth);
                var date = new DateOnly(year, month, day);
                if (date >= start && date <= end)
                {
                    stamps.Add(new Stamp
                    {
                        Id = $"s{idx++:D4}",
                        Date = date,
                        Glyph = t.Glyph,
                        Position = t.Position,
                        FontSize = t.FontSize,
                        Opacity = t.Opacity,
                    });
                }
            }
        }

        return stamps;
    }

    private static IEnumerable<(int Year, int Month)> EnumerateMonths(DateOnly start, DateOnly end)
    {
        var current = new DateOnly(start.Year, start.Month, 1);
        var last = new DateOnly(end.Year, end.Month, 1);
        while (current <= last)
        {
            yield return (current.Year, current.Month);
            current = current.AddMonths(1);
        }
    }

    private static ScheduleEvent CreateEvent(
        string id, string title, DateOnly start, DateOnly end,
        ScheduleStyle style, Color bg, Color fg, bool underline = false) =>
        new()
        {
            Id = id,
            Title = title,
            StartDate = start,
            EndDate = end,
            Style = style,
            BackgroundColor = bg,
            TextColor = fg,
            Underline = underline,
        };
}
