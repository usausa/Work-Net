namespace WorkCalendar.Models;

/// <summary>カレンダーの日付選択モードを表します。</summary>
public enum CalendarSelectionMode
{
    /// <summary>選択なし。</summary>
    None,

    /// <summary>一度に 1 日だけ選択できます。</summary>
    Single,

    /// <summary>複数の日付を個別に選択できます。</summary>
    Multiple,

    /// <summary>開始日と終了日の範囲を選択できます。</summary>
    Range,
}
