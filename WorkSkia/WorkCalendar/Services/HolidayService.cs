namespace WorkCalendar.Services;

using System;
using System.Collections.Generic;

public sealed class HolidayService : IHolidayService
{
    public IReadOnlyList<DateOnly> GetHolidays(DateOnly start, DateOnly end)
    {
        // [DEBUG] 前々月より前なら空を返す（性能検証用）
        var twoMonthsAgo = DateOnly.FromDateTime(DateTime.Today).AddMonths(-2);
        if (start < new DateOnly(twoMonthsAgo.Year, twoMonthsAgo.Month, 1))
            return Array.Empty<DateOnly>();

        var holidays = new List<DateOnly>();
        for (var year = start.Year; year <= end.Year; year++)
        {
            foreach (var d in GetYearHolidays(year))
            {
                if (d >= start && d <= end)
                    holidays.Add(d);
            }
        }
        holidays.Sort();
        return holidays;
    }

    private static IEnumerable<DateOnly> GetYearHolidays(int year)
    {
        var holidays = new HashSet<DateOnly>();

        // 元日
        holidays.Add(new(year, 1, 1));
        // 成人の日: 1月第2月曜
        holidays.Add(NthWeekday(year, 1, DayOfWeek.Monday, 2));
        // 建国記念の日
        holidays.Add(new(year, 2, 11));
        // 天皇誕生日 (2020年以降は2/23)
        if (year >= 2020)
            holidays.Add(new(year, 2, 23));
        // 春分の日
        holidays.Add(new(year, 3, SpringEquinox(year)));
        // 昭和の日
        holidays.Add(new(year, 4, 29));
        // 憲法記念日
        holidays.Add(new(year, 5, 3));
        // みどりの日
        holidays.Add(new(year, 5, 4));
        // こどもの日
        holidays.Add(new(year, 5, 5));
        // 海の日: 7月第3月曜
        holidays.Add(NthWeekday(year, 7, DayOfWeek.Monday, 3));
        // 山の日 (2016年以降)
        if (year >= 2016)
            holidays.Add(new(year, 8, 11));
        // 敬老の日: 9月第3月曜
        holidays.Add(NthWeekday(year, 9, DayOfWeek.Monday, 3));
        // 秋分の日
        holidays.Add(new(year, 9, AutumnEquinox(year)));
        // スポーツの日: 10月第2月曜
        holidays.Add(NthWeekday(year, 10, DayOfWeek.Monday, 2));
        // 文化の日
        holidays.Add(new(year, 11, 3));
        // 勤労感謝の日
        holidays.Add(new(year, 11, 23));
        // 天皇誕生日 (2018年以前)
        if (year <= 2018)
            holidays.Add(new(year, 12, 23));

        // 振替休日: 祝日が日曜の場合、翌月曜が振替休日
        var substitutes = new List<DateOnly>();
        foreach (var h in holidays)
        {
            if (h.DayOfWeek == DayOfWeek.Sunday)
            {
                var substitute = h.AddDays(1);
                if (!holidays.Contains(substitute))
                    substitutes.Add(substitute);
            }
        }
        foreach (var s in substitutes)
            holidays.Add(s);

        // 国民の祝日: 前後を祝日に挟まれた平日
        var sandwiched = new List<DateOnly>();
        foreach (var h in holidays)
        {
            var candidate = h.AddDays(1);
            if (!holidays.Contains(candidate) &&
                candidate.DayOfWeek != DayOfWeek.Sunday &&
                candidate.DayOfWeek != DayOfWeek.Saturday &&
                holidays.Contains(candidate.AddDays(1)))
            {
                sandwiched.Add(candidate);
            }
        }
        foreach (var s in sandwiched)
            holidays.Add(s);

        return holidays;
    }

    private static DateOnly NthWeekday(int year, int month, DayOfWeek dow, int n)
    {
        var first = new DateOnly(year, month, 1);
        var offset = ((int)dow - (int)first.DayOfWeek + 7) % 7;
        return first.AddDays(offset + (n - 1) * 7);
    }

    private static int SpringEquinox(int year)
    {
        var x = year - 1980;
        return (int)(20.69115 + 0.242194 * x - Math.Floor(x / 4.0));
    }

    private static int AutumnEquinox(int year)
    {
        var x = year - 1980;
        return (int)(23.09 + 0.242194 * x - Math.Floor(x / 4.0));
    }
}
