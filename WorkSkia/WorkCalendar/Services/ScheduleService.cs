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
    private static readonly Color YellowText = Color.FromArgb("#F9A825");
    private static readonly Color Orange = Color.FromArgb("#FB8C00");
    private static readonly Color Blue = Color.FromArgb("#1E88E5");
    private static readonly Color CyanText = Color.FromArgb("#00ACC1");

    private readonly IReadOnlyList<ScheduleEvent> events;
    private readonly IReadOnlyList<Stamp> stamps;

    public ScheduleService()
    {
        events = BuildEvents();
        stamps = BuildStamps();
    }

    public IReadOnlyList<ScheduleEvent> GetEvents(DateOnly start, DateOnly end) =>
        events.Where(e => (e.EndDate >= start) && (e.StartDate <= end)).ToList();

    public IReadOnlyList<Stamp> GetStamps(DateOnly start, DateOnly end) =>
        stamps.Where(s => (s.Date >= start) && (s.Date <= end)).ToList();

    private static IReadOnlyList<ScheduleEvent> BuildEvents() =>
    [
        Filled("e01", "ぶどう狩", new(2019, 5, 26), new(2019, 5, 26), VividMagenta),
        Text  ("e02", "週間報告", new(2019, 5, 27), GreenText),
        Text  ("e03", "英会話",   new(2019, 5, 27), PinkText),
        Filled("e04", "○ジム",    new(2019, 5, 28), new(2019, 5, 28), Cyan),
        Filled("e05", "燃えるゴ", new(2019, 5, 28), new(2019, 5, 28), DarkRed),
        Filled("e06", "会社研修", new(2019, 5, 30), new(2019, 5, 31), Green),
        Filled("e07", "買い物",   new(2019, 5, 31), new(2019, 5, 31), Yellow, textColor: Colors.Black),
        Text  ("e08", "水泳教室", new(2019, 6, 1), CyanText),
        Filled("e09", "遊園地",   new(2019, 6, 2), new(2019, 6, 2), Orange),
        Filled("e10", "燃えるゴ", new(2019, 6, 4), new(2019, 6, 4), DarkRed),
        Filled("e11", "会社休み", new(2019, 6, 6), new(2019, 6, 8), Pink),
        Filled("e12", "ゴルフ",   new(2019, 6, 8), new(2019, 6, 8), Cyan),
        Filled("e13", "会社休み", new(2019, 6, 9), new(2019, 6, 9), Pink),
        Filled("e14", "温泉旅行", new(2019, 6, 9), new(2019, 6, 9), Orange),
        Text  ("e15", "週間報告", new(2019, 6, 10), GreenText),
        Text  ("e16", "英会話",   new(2019, 6, 10), PinkText),
        Filled("e17", "燃えるゴ", new(2019, 6, 11), new(2019, 6, 11), DarkRed),
        TextRange("e18", "大阪出張", new(2019, 6, 12), new(2019, 6, 13), Blue, underline: true),
        Filled("e19", "友達泊まり", new(2019, 6, 14), new(2019, 6, 16), HotPink),
        Text  ("e20", "水泳教室", new(2019, 6, 15), CyanText),
        Filled("e21", "○ジム",    new(2019, 6, 18), new(2019, 6, 18), Cyan),
        Filled("e22", "燃えるゴ", new(2019, 6, 18), new(2019, 6, 18), DarkRed),
        Text  ("e23", "サークル", new(2019, 6, 18), YellowText),
        Text  ("e24", "会社飲み", new(2019, 6, 19), GreenText),
        Text  ("e25", "週間報告", new(2019, 6, 24), GreenText),
        Text  ("e26", "英会話",   new(2019, 6, 24), PinkText),
        Filled("e27", "燃えるゴ", new(2019, 6, 25), new(2019, 6, 25), DarkRed),
        Text  ("e28", "サークル", new(2019, 6, 25), YellowText),
        Filled("e29", "海外出張", new(2019, 6, 26), new(2019, 7, 1), Blue),
        Filled("e30", "燃えるゴ", new(2019, 7, 2), new(2019, 7, 2), DarkRed),
        Text  ("e31", "歓迎会",   new(2019, 7, 3), PinkText),
        Text  ("e32", "会社飲み", new(2019, 7, 11), GreenText),
    ];

    private static IReadOnlyList<Stamp> BuildStamps() =>
    [
        new Stamp { Id = "s01", Date = new(2019, 6, 3),  Glyph = "\U0001F6A9", Position = StampPosition.TopRight, FontSize = 22 },
        new Stamp { Id = "s02", Date = new(2019, 6, 3),  Glyph = "\U0001F941", Position = StampPosition.BottomLeft, FontSize = 24 },
        new Stamp { Id = "s03", Date = new(2019, 6, 8),  Glyph = "\U0001F426", Position = StampPosition.TopRight, FontSize = 22 },
        new Stamp { Id = "s04", Date = new(2019, 6, 13), Glyph = "✈️", Position = StampPosition.Center, FontSize = 26 },
        new Stamp { Id = "s05", Date = new(2019, 6, 16), Glyph = "\U0001F436", Position = StampPosition.Center, FontSize = 32, Opacity = 0.9 },
        new Stamp { Id = "s06", Date = new(2019, 6, 20), Glyph = "\U0001F45B", Position = StampPosition.TopCenter, FontSize = 22 },
        new Stamp { Id = "s07", Date = new(2019, 6, 22), Glyph = "\U0001F43C", Position = StampPosition.TopLeft, FontSize = 22 },
        new Stamp { Id = "s08", Date = new(2019, 6, 26), Glyph = "\U0001F38F", Position = StampPosition.TopCenter, FontSize = 22 },
        new Stamp { Id = "s09", Date = new(2019, 6, 29), Glyph = "\U0001F408", Position = StampPosition.TopRight, FontSize = 24 },
        new Stamp { Id = "s10", Date = new(2019, 7, 1),  Glyph = "\U0001F37B", Position = StampPosition.BottomCenter, FontSize = 24 },
        new Stamp { Id = "s11", Date = new(2019, 7, 3),  Glyph = "\U0001F490", Position = StampPosition.BottomCenter, FontSize = 24 },
        new Stamp { Id = "s12", Date = new(2019, 7, 5),  Glyph = "❤️",  Position = StampPosition.Center, FontSize = 32, Opacity = 0.7 },
    ];

    private static ScheduleEvent Filled(string id, string title, DateOnly start, DateOnly end, Color background, Color? textColor = null) =>
        new()
        {
            Id = id,
            Title = title,
            StartDate = start,
            EndDate = end,
            Style = ScheduleStyle.Filled,
            BackgroundColor = background,
            TextColor = textColor ?? Colors.White,
        };

    private static ScheduleEvent Text(string id, string title, DateOnly date, Color textColor) =>
        new()
        {
            Id = id,
            Title = title,
            StartDate = date,
            EndDate = date,
            Style = ScheduleStyle.Text,
            TextColor = textColor,
        };

    private static ScheduleEvent TextRange(string id, string title, DateOnly start, DateOnly end, Color textColor, bool underline) =>
        new()
        {
            Id = id,
            Title = title,
            StartDate = start,
            EndDate = end,
            Style = ScheduleStyle.Text,
            TextColor = textColor,
            Underline = underline,
        };
}
