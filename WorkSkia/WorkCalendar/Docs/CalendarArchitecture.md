# WorkCalendar アーキテクチャ解説

モデル・ビューモデル・コントロール内の各要素について、  
階層構造と役割を記述します。

---

## 1. 全体構成

```
Models/                    ← 純粋なデータ型（UI・MAUI への依存なし）
ViewModels/                ← 表示用データ・ロジック（MonthViewBuilder を含む）
Controls/CalendarView      ← MAUI コントロール（描画・ジェスチャ）
Services/                  ← データ供給インターフェイスと実装
```

---

## 2. Models 層

### 2.1 クラス・列挙型一覧

| 型 | 種別 | 概要 |
|----|------|------|
| `ScheduleEvent` | クラス | カレンダーに表示するイベント（単日・複数日共通） |
| `Stamp` | クラス | 日付セルに貼り付けるグリフ（絵文字など）の装飾 |
| `DayKind` | enum | 平日 / 土曜 / 日曜 / 祝日の区別 |
| `StampPosition` | enum | スタンプをセル内のどの位置に表示するか |
| `ScheduleStyle` | enum | イベントの描画スタイル（塗り `Filled` / テキスト `Text`） |
| `CalendarSelectionMode` | enum | 日付選択モード（なし / 単一 / 複数 / 範囲） |

### 2.2 `ScheduleEvent` フィールド構成

```
ScheduleEvent
├─ Id                string     識別子
├─ Title             string     表示タイトル
├─ StartDate         DateOnly   開始日
├─ EndDate           DateOnly   終了日
├─ Style             ScheduleStyle   Filled=角丸バー / Text=テキストのみ
├─ BackgroundColor   Color      バー背景色（Filled 時）
├─ TextColor         Color      文字色
├─ Underline         bool       タイトル下線
├─ LeadingGlyph      string?    先頭グリフ（未使用枠）
├─ DurationDays      int        (計算プロパティ) 日数
└─ IsMultiDay        bool       (計算プロパティ) 複数日かどうか
```

### 2.3 `Stamp` フィールド構成

```
Stamp
├─ Id           string        識別子
├─ Date         DateOnly      表示日
├─ Glyph        string        表示文字 / 絵文字
├─ Position     StampPosition セル内配置位置（Center, TopLeft, … BottomRight）
├─ FontSize     double        グリフフォントサイズ（既定 28）
└─ Opacity      double        透明度（既定 1.0）
```

---

## 3. ViewModels 層

### 3.1 データ ViewModel（読み取り専用レコード的クラス）

```
MonthViewModel                  ← CalendarView に渡す月全体のスナップショット
├─ Year          int
├─ Month         int
├─ Today         DateOnly
└─ Weeks         IReadOnlyList<WeekViewModel>   (最大 6 要素)

    WeekViewModel               ← 1 週間分のデータ
    ├─ Days            IReadOnlyList<DayViewModel>   (7 要素)
    ├─ EventPlacements IReadOnlyList<EventPlacement> ← この週に表示するイベント配置
    └─ SlotCount       int                          ← 使用スロット数（行数計算に使用）

        DayViewModel            ← 1 日分のデータ
        ├─ Date            DateOnly
        ├─ IsCurrentMonth  bool    表示月に属するか（前後月の日付は false）
        ├─ IsToday         bool
        ├─ Kind            DayKind
        └─ Stamps          IReadOnlyList<Stamp>

        EventPlacement          ← 週グリッド上のイベント描画情報
        ├─ Event                    ScheduleEvent   元データへの参照
        ├─ StartColumn              int             週内の開始列 (0–6)
        ├─ ColumnSpan               int             列スパン
        ├─ Slot                     int             スロット行番号 (0 始まり)
        ├─ ContinuesFromPreviousWeek bool           前週から継続するか
        └─ ContinuesToNextWeek      bool            次週へ継続するか
```

### 3.2 `MonthViewBuilder`（ビルダー）

`MainPageViewModel` が保持し、月データから `MonthViewModel` を生成するクラス。

```
MonthViewBuilder(weekStartDayOfWeek)
├─ GetDisplayRange(year, month) → (Start, End)
│      表示グリッド全体（6 週 × 7 日）の日付範囲を返す。
│      サービス呼び出しの引数に使用。
└─ Build(year, month, today, events, stamps, holidays) → MonthViewModel
       ┌─ CreateStampLookup        stamps を DateOnly キーの辞書に変換
       ├─ CreateWeeklyEventCandidates
       │      全イベントを週ごとにクリッピングし、開始列・終了列を計算
       ├─ (週ループ)
       │   ├─ DetermineKind         祝日・曜日から DayKind を決定
       │   ├─ AssignPlacements      スロット衝突回避（ビットマスクで高速化）
       │   └─ GetSlotCount          最大スロット番号 + 1
       └─ MonthViewModel 生成
```

**スロット割り当てアルゴリズム**

イベントを開始列でソート後、各列範囲をビットマスクで表現し、  
衝突しない最小スロット番号を `stackalloc` スタックバッファで探索する。

### 3.3 `MainPageViewModel`（ページ VM）

`INotifyPropertyChanged` を実装し、ページと `CalendarView` をつなぐ。

```
MainPageViewModel
├─ builder           MonthViewBuilder    週開始日に合わせて再生成
├─ currentYear/Month int
├─ MonthViewModel    ← CalendarView.ViewModel にバインド
│
├─ FirstDayOfWeek    DayOfWeek   変更時に builder を再生成して再ロード
├─ SelectionMode     CalendarSelectionMode
├─ SelectedDate / SelectedDates / SelectedStartDate / SelectedEndDate
├─ MinDate / MaxDate DateOnly?
├─ Culture           CultureInfo?
│
├─ PrevMonthCommand  ← CalendarView.PrevMonthCommand にバインド
├─ NextMonthCommand  ← CalendarView.NextMonthCommand にバインド
├─ GoToTodayCommand
├─ DayTappedCommand
└─ EventTappedCommand
```

---

## 4. Controls 層 — `CalendarView`

### 4.1 XAML 構造

```
CalendarView (ContentView)
└─ RootGrid  (Grid, 4 行)
   ├─ [Row 0] HeaderGrid           年月ラベル・前後ナビボタン
   │           ├─ PrevButton (◀)
   │           ├─ YearLabel / MonthLabel (VerticalStackLayout)
   │           └─ NextButton (▶)
   ├─ [Row 1] BoxView (水平区切り線)
   ├─ [Row 2] WeekdayHeaderGrid    曜日名ヘッダー（動的生成）
   └─ [Row 3] Grid (IsClippedToBounds=True)  ← スライドアニメーション用クリップ
               └─ WeeksHost (Grid, 6 行等分)
                  ├─ [Row 0] WeekRowVisual.Root
                  ├─ [Row 1] WeekRowVisual.Root
                  ├─ ...
                  └─ [Row 5] WeekRowVisual.Root
```

### 4.2 内部 Visual クラス階層

`WeekRowVisual` と `DayCellVisual` は C# でコード生成される固定ビジュアル ツリーで、  
起動時に 6 行 × 7 列分を一括生成し、月切り替え時は **内容だけ更新**する（再生成しない）。

```
WeekRowVisual
├─ Root (Grid, 7 列 × 可変行)
│   ├─ [各列 Row 0] DayCellVisual.Background (BoxView, RowSpan 全体)
│   │                 日付セル背景色（選択・今日・月外・曜日別）
│   ├─ [各列 Row 0] DayCellVisual.RangeBackground (BoxView, RowSpan 全体)
│   │                 範囲選択時の背景帯
│   ├─ TopDivider  (BoxView, 横一線の上部区切り線)
│   ├─ VerticalDividers[0..5] (BoxView × 6, 列区切り縦線)
│   ├─ [各列 Row 0] DayCellVisual.TapTarget (Border, RowSpan 全体)
│   │                 タップ・パンジェスチャの受け口
│   ├─ [各列 Row 0] DayCellVisual.DateBubble (Border → DateLabel)
│   │                 日付数字の丸バッジ（今日ハイライト等）
│   ├─ [列 c  Row slot] EventBorderVisual.Root (プールから取得)
│   │                     イベントバー（EventPool で再利用）
│   └─ [列 c  Row 0]   StampLabel (Label プールから取得)
│                         スタンプ文字（StampLabelPool で再利用）
│
├─ Days[0..6]      DayCellVisual[]    各日セルのビジュアル
├─ EventPool       List<EventBorderVisual>   イベントバーのプール
├─ ActiveEventCount  int   今サイクルで使用中のプール要素数
└─ ActiveStampCounts int[7] 各列の使用中スタンプ数

DayCellVisual
├─ Background     BoxView            セル全体を覆う背景（選択色など）
├─ RangeBackground BoxView           範囲選択帯
├─ TapTarget      Border             タッチ領域（TapGesture + PanGesture を保持）
├─ DateBubble     Border             日付数字を囲む丸バッジ
│   └─ DateLabel  Label              日付数字テキスト
├─ TapGesture     TapGestureRecognizer
├─ PanGesture     PanGestureRecognizer  スワイプ検出用（各セルに付与）
├─ StampLabelPool List<Label>        スタンプ用再利用 Label プール
└─ DateBubbleShape RoundRectangle    形状を毎回 new せず再利用

EventBorderVisual
├─ Root    Border             イベントバー外枠（角丸・背景色）
├─ Label   Label              イベントタイトル
├─ TapGesture TapGestureRecognizer
└─ (shape) RoundRectangle     角丸形状インスタンス（再利用）
```

### 4.3 描画フロー

```
ViewModel プロパティ変更
└─ OnViewModelChanged
   └─ Render(MonthViewModel)
      ├─ 方向検出 (lastNavDirection)
      ├─ ヘッダー更新 (YearLabel / MonthLabel)  ← 初回のみスタイル適用
      ├─ slotCount 計算 (全週の最大スロット数)
      ├─ RebuildWeeksHost(month, slotCount)
      │   └─ (週ループ) UpdateWeekRow(row, week, slotCount)
      │        ├─ UpdateRows       行定義・RowSpan を更新（slotCount 変化時のみ）
      │        ├─ UpdateDayCell    各セルの背景・日付・タップ状態を更新
      │        ├─ HideDynamicViews プールの使用済みビューを非表示化
      │        ├─ AddStampViews    スタンプを描画（プールを拡張しながら再利用）
      │        └─ AddEventViews    イベントバーを描画（プールを拡張しながら再利用）
      └─ AnimateSlideAsync(direction)  スライドアニメーション（非同期・fire-and-forget）
```

### 4.4 スワイプ・アニメーション設定値

`CalendarView.xaml.cs` の **Swipe & animation tuning** セクションに定数が集約されています。

| 定数 | 既定値 | 説明 |
|------|--------|------|
| `SwipeThreshold` | 36 dp | 月切り替えが発動する最小水平移動距離。小さいほど反応が速い |
| `SwipeFlickThreshold` | 20 dp | フリック（短い素早いスワイプ）を検知する最小ステップ幅 |
| `SwipeHorizontalBias` | 1.25 | 水平移動が垂直移動の何倍以上でないとスワイプと見なさない |
| `SwipeAnimationDurationMs` | 180 ms | スライドアニメーション時間。短いほど素早く切り替わる |

### 4.5 BindableProperty 一覧

| プロパティ | 型 | 用途 |
|-----------|-----|------|
| `ViewModel` | `MonthViewModel?` | 表示データ。変更で全再描画 |
| `PrevMonthCommand` | `ICommand?` | 前月ナビゲーション |
| `NextMonthCommand` | `ICommand?` | 次月ナビゲーション |
| `GoToTodayCommand` | `ICommand?` | 今日の月へジャンプ |
| `DayTappedCommand` | `ICommand?` | 日付タップ通知 |
| `EventTappedCommand` | `ICommand?` | イベントタップ通知 |
| `SelectionMode` | `CalendarSelectionMode` | 選択モード |
| `SelectedDate` | `DateOnly?` | 単一選択日 (TwoWay) |
| `SelectedDates` | `ObservableCollection<DateOnly>?` | 複数選択日 (TwoWay) |
| `SelectedStartDate` | `DateOnly?` | 範囲選択開始 (TwoWay) |
| `SelectedEndDate` | `DateOnly?` | 範囲選択終了 (TwoWay) |
| `SelectedDayBackground` | `Color` | 選択日の背景色 |
| `SelectedDayTextColor` | `Color` | 選択日の文字色 |
| `RangeBackground` | `Color` | 範囲選択帯の背景色 |
| `MinDate` | `DateOnly?` | 選択可能な最小日付 |
| `MaxDate` | `DateOnly?` | 選択可能な最大日付 |
| `Culture` | `CultureInfo?` | 月名・曜日名の言語設定 |
| `DateRowHeight` | `double` (26) | 日付行の高さ |
| `SlotRowHeight` | `double` (17) | イベントスロット行の高さ |
| `DateNumberFontSize` | `double` (13) | 日付数字のフォントサイズ |
| `EventFontSize` | `double` (11) | イベントタイトルのフォントサイズ |
| `FirstDayOfWeek` | `DayOfWeek` (Monday) | 週の開始曜日 |
| `SwipeEnabled` | `bool` (true) | スワイプによる月切り替えの有効/無効 |
| `GridLineColor` | `Color` | 区切り線の色 |
| (その他カラー系) | `Color` | 曜日・今日・月外・選択等の各色 |

---

## 5. Services 層

```
IScheduleService
├─ GetEvents(start, end)  → IReadOnlyList<ScheduleEvent>
└─ GetStamps(start, end)  → IReadOnlyList<Stamp>

IHolidayService
└─ GetHolidays(start, end) → IReadOnlyList<DateOnly>
```

`ScheduleService` / `HolidayService` はサンプル実装。  
前々月より前の範囲が指定された場合は 0 件を返す（パフォーマンス計測用）。

---

## 6. データフロー全体像

```
Services
  ScheduleService ──→┐
  HolidayService  ──→┤
                     ↓
             MainPageViewModel.LoadMonth()
                     ↓
             MonthViewBuilder.Build()
                     ↓
             MonthViewModel（スナップショット）
                     ↓ (INotifyPropertyChanged)
             CalendarView.Render()
                     ↓
             WeekRowVisual × 6（固定ビジュアルツリー更新）
                     ↓
             画面描画
```

スワイプ・ナビボタン操作は `MainPageViewModel` のコマンド経由で `LoadMonth` を再呼び出しし、  
新しい `MonthViewModel` を `ViewModel` プロパティにセットすることで再描画が始まる。
