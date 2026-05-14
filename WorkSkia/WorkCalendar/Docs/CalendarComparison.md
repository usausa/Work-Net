# WorkCalendar 機能比較レポート

`__Calendar/` 配下に配置した既存のカレンダーライブラリと、本プロジェクトの `WorkCalendar.Controls.CalendarView` を比較し、追加すると有用と考えられる機能・改善点を整理する。

## 0. 比較対象ライブラリ

| ライブラリ | プラットフォーム | 特徴の方向性 |
| ---- | ---- | ---- |
| **Plugin.Maui.Calendar** | .NET MAUI | イベントコレクション/ドット表示・ローカライズ・Style 化に強い |
| **Xamarin.Plugin.Calendar** | Xamarin.Forms | 複数レイアウト（Month/Two/Week）・範囲選択・テンプレート（Header/Footer/Bottom）が豊富 |
| **XCalendar** (Core / Forms / Maui) | .NET MAUI / Forms / Core | ナビゲーション制限/ループ、行数自動、Selection モード、DayState、ControlTemplate で全カスタム可能 |
| **Xalendar** | Xamarin.Forms | シンプルな月ビュー＋イベント表示 |
| **XamarinForms.CalendarControl** | Xamarin.Forms | DayControlTemplate / WeekDayHeaderControlTemplate による完全テンプレート差し替え、単一/複数選択、週末・他月表示の ON/OFF |
| **XFXCalendarPluginSample** | Xamarin.Forms | XCalendar の利用サンプル |

## 1. 現状の WorkCalendar 機能（要点）

- 6 行 × 7 列の固定月ビュー（`MonthViewBuilder`）
- 月曜始まり（コンストラクタで切替可能）／日本語曜日固定
- 単日/複数日イベントをスロットアルゴリズムで衝突回避配置（週またぎで分割）
- 当日ハイライト・休日色・月外グレーアウト・週末背景
- スタンプ（絵文字グリフ）をセル内 6 ポジションに配置
- 前/次月/日タップ/イベントタップの `ICommand`
- 多数の色・サイズの `BindableProperty`
- 日本祝日サービス（`HolidayService`）

なし／弱い領域: 選択状態管理、ナビゲーション制限、スワイプ、テンプレート化、ローカライズ、週番号、行数可変、アクセシビリティ、複数選択/範囲選択、Footer/イベントリスト、カルチャ依存の曜日順。

## 2. 他ライブラリから採り入れたい機能（推奨度付き）

凡例: ★★★ 強く推奨 / ★★ 推奨 / ★ 余力があれば

### 2.1 選択機能（Selection）— ★★★
- 影響元: XCalendar (`SelectionAction`, `SelectionType`), Xamarin.Plugin.Calendar (`RangeSelectionCalendar`), XamarinForms.CalendarControl (`SelectionMode`)
- 提案:
  - `SelectionMode`: `None` / `Single` / `Multiple` / `Range` の列挙を新設。
  - `SelectedDate` (`DateOnly?`) / `SelectedDates` (`ObservableCollection<DateOnly>`) / `SelectedStartDate` + `SelectedEndDate` の Bindable。
  - 既存の `OnDayTapped` を選択ロジックに統合し、`SelectedDayBackground` / `SelectedDayTextColor` / `RangeBackground` を色プロパティとして追加。
  - 範囲選択中の中間日は両端と別色（XCalendar の `SelectedDatesRangeBackgroundColor` 相当）。

### 2.2 ナビゲーション拡張 — ★★★
- 影響元: XCalendar (`NavigationLowerBound`, `NavigationUpperBound`, `NavigationLoopMode`, `Navigate()`)
- 提案:
  - `MinDate` / `MaxDate` を追加し、範囲外の `PrevMonthCommand` / `NextMonthCommand` を自動的に `CanExecute=false` に。
  - `NavigationLoopMode` (`None` / `Loop` / `LoopMinimum` / `LoopMaximum`) を追加。
  - 「今日へジャンプ」`GoToTodayCommand` と `GoToDate(DateOnly)` メソッド。
  - 年単位ナビ用に `PrevYearCommand` / `NextYearCommand`（Plugin.Maui.Calendar に類似）。

### 2.3 スワイプジェスチャ — ★★
- 影響元: Xamarin.Plugin.Calendar (`SwipeLeftCommand`, `SwipeRightCommand`, `SwipeUpCommand`, `SwipeToChangeMonthEnabled`)
- 提案: `SwipeView` ではなく `PanGestureRecognizer` をルート Grid に付け、左右で月送り、上下でビュー縮小/拡張（後述）にマップ。`SwipeToChangeMonthEnabled` のような ON/OFF を用意。

### 2.4 ビューレイアウト（Month / Two Weeks / Week） — ★★
- 影響元: Xamarin.Plugin.Calendar (`CalendarLayout`), XCalendar (`Rows`, `AutoRows`)
- 提案:
  - `CalendarLayout`: `Month` / `TwoWeeks` / `Week` を追加し、`MonthViewBuilder` の WeeksPerMonth 定数を可変に。
  - XCalendar の `AutoRows` のように、その月で必要な最小行数（4〜6）で描画する `AutoRows`／`AutoRowsIsConsistent` も検討。

### 2.5 ローカライズ / カルチャ — ★★★
- 影響元: Plugin.Maui.Calendar (`Culture`, `UseNativeDigits`, `UseAbbreviatedDayNames`), Xamarin.Plugin.Calendar
- 提案:
  - `Culture` (`CultureInfo`) プロパティを追加し、月/曜日テキストを `DateTimeFormatInfo.MonthNames` / `AbbreviatedDayNames` から取得。
  - 現在の XAML 固定文字列（"月火水木金土日"）を廃し、`WeekdayHeaderGrid` を動的生成。
  - 必要なら `UseNativeDigits`（アラビア数字以外の表示）も追加。
  - これに伴い、土日色も `Culture.DateTimeFormat.FirstDayOfWeek` 基準に列再計算。

### 2.6 週開始日の `BindableProperty` 化 — ★★★
- 影響元: XCalendar (`StartOfWeek`), XamarinForms.CalendarControl (`FirstDayOfWeek`)
- 提案: 現状は `MonthViewBuilder` コンストラクタ引数のみ。`CalendarView.FirstDayOfWeek` を Bindable にして、変更時に再ビルド・再描画。

### 2.7 イベント表現の強化 — ★★
- 影響元: Plugin.Maui.Calendar (`EventCollection`, `EventIndicatorType` = BottomDot / TopDot / Background / BackgroundFull), Xamarin.Plugin.Calendar (`EventTemplate`, `EmptyTemplate`)
- 提案:
  - 現状は「Filled バー」スタイル一本。`EventIndicatorType`（Dot / Bar / Background）を追加して、コンパクト表示を選べるように。
  - イベント数がスロット上限を超える場合の「+N more」インジケータ（Google Calendar 風）。
  - `EventTappedCommand` に加え、`EventLongPressedCommand` を追加（ドラッグ／編集の足場）。
  - `EventTemplate` を `DataTemplate` で差し替え可能に（XCalendar の `DayTemplate` の発想）。

### 2.8 テンプレート差し替え（ControlTemplate / DataTemplate） — ★★★
- 影響元: XamarinForms.CalendarControl (`DayControlTemplate`, `WeekDayHeaderControlTemplate`), XCalendar (`DayTemplate`, `DayNameTemplate`, `NavigationViewTemplate`), Xamarin.Plugin.Calendar (`HeaderSectionTemplate`, `FooterSectionTemplate`, `BottomSectionTemplate`, `EventTemplate`)
- 提案: 現在 `BuildWeekRow` / `BuildDateNumberView` がコードビハインドで View を生成しているため、外部からのカスタム差し替えができない。以下を Bindable に。
  - `DayTemplate` (`DataTemplate`)
  - `DayHeaderTemplate` (`DataTemplate`)
  - `HeaderTemplate` (`ControlTemplate` or `DataTemplate`)
  - `EventTemplate` (`DataTemplate`)
  - 互換性のため、現在の組み込みビルダーは「既定の DataTemplate」として実装。

### 2.9 DayState 概念の導入 — ★★
- 影響元: XCalendar (`DayState`: `CurrentMonth` / `OtherMonth` / `Today` / `Selected` / `Invalid`)
- 提案: `DayViewModel` に `State` を集約し、Style/Template から `DataTrigger` で扱えるようにする。`Invalid`（無効日付）状態の概念を導入すれば、`MinDate`/`MaxDate` 機能と統合できる。

### 2.10 イベント表示と同居する下部リスト — ★
- 影響元: Plugin.Maui.Calendar / Xamarin.Plugin.Calendar の「選択日のイベントリスト」
- 提案: `CalendarView` 直下ではなく、別コンポーネント `CalendarWithAgendaView` として実装（責務分離）。

### 2.11 月インジケータ / 装飾 — ★
- 既存 `CalendarDesign.md` に「中央に薄く月数字を表示」とあるが未実装。装飾レイヤを Template 化することで実装容易。

### 2.12 アニメーション — ★
- 影響元: Plugin.Maui.Calendar (`AnimateCalendar`), XCalendar (Animated Swipable)
- 提案: 月切替時のフェード／スライド。`AnimateMonthChange` プロパティの導入。

### 2.13 週番号表示 — ★
- 影響元: Xamarin.Plugin.Calendar (`WeekViewUnit = WeekNumber`)
- 提案: 左端に ISO 週番号列を表示する `ShowWeekNumbers` プロパティ。

### 2.14 アクセシビリティ — ★★
- 提案: 日付セルに `SemanticProperties.Description`（「2025年11月22日 土曜日 イベント3件」）を設定。`AutomationProperties` 設定。キーボードフォーカス対応（Windows/Mac 利用時）。

### 2.15 Style ベースのカスタマイズ — ★★
- 影響元: Plugin.Maui.Calendar V2 が個別プロパティを `Style` に集約
- 提案: 現在 30 個以上ある色/サイズ系 `BindableProperty` を、用途ごとに `Style` プロパティに整理。
  - `DayLabelStyle`, `OutsideMonthDayLabelStyle`, `TodayLabelStyle`, `WeekendLabelStyle`, `HeaderStyle`, `EventLabelStyle` 等。
  - 既存プロパティは互換のため残しつつ、Style 経由を推奨に。

### 2.16 イベントコレクションのモデル化 — ★★
- 影響元: Plugin.Maui.Calendar (`EventCollection`, `DayEventCollection<T>`)
- 提案: 現状は `IReadOnlyList<ScheduleEvent>` を `MonthViewBuilder.Build` に渡す静的構造。
  - イベント差分通知ができないため、`ObservableCollection<ScheduleEvent>` を購読する `Events` Bindable を追加し、変更時に該当月だけ再構築。
  - 大量イベント対策として、月単位インデックス（Dictionary<YearMonth, List<...>>）でフィルタ。

## 3. 設計/コード上の改善点

### 3.1 ViewModel 駆動描画の限界
- 現状 `OnRenderPropertyChanged` のたびに `WeeksHost.Children.Clear()` し、全 View を作り直す。
- スロット数や色など些細な変更でも全破棄になるため、月ナビ時のフリッカーやアロケーションが増える。
- 改善: WeekRow を再利用する仮想化、もしくは `BindableLayout` + `DataTemplate` 化（§2.8 と連動）。

### 3.2 サイズ/色プロパティの肥大化
- 現状 30 個以上。`Style` で集約（§2.15）し、`BindableProperty` 自体を減らす。

### 3.3 入力イベントの粒度
- `DayTappedCommand` のみで「日付タップ」「セル空白タップ」「イベントタップ」を区別する仕組みがない（現実装では `EventTappedCommand` のみ別）。
- 改善: `DayDoubleTappedCommand`, `DayLongPressCommand`, `EmptyAreaTappedCommand`（追加ダイアログ用）など分離。

### 3.4 文字列リソース化
- "月" などラベル文字列がコード/XAML にハードコード。`Resources/Strings/AppResources.resx` に集約し、`Culture` 連動で多言語化。

### 3.5 NULL/範囲安全
- `Render` で `month.Weeks.Max(...)` を呼ぶが `Weeks` が空なら例外。`Math.Max(2, ...)` の前に空チェックを追加すべき（堅牢化）。

### 3.6 テスト容易性
- `MonthViewBuilder` は純粋ロジックでテスト容易。今後の機能（選択、ナビ制限、レイアウト切替）に向けて、`WorkCalendar.Tests` プロジェクトを用意し XCalendar の `Core.Tests` を参考に単体テストを整備。

### 3.7 描画パスの分離
- 現在 `CalendarView.xaml.cs` が約 500 行で「BindableProperty 宣言」「View ビルド」「色決定」「タップ処理」をすべて持つ。
- 改善: 部分クラス分割（`CalendarView.Properties.cs` / `CalendarView.Render.cs` / `CalendarView.Gestures.cs`）。

### 3.8 ジオメトリ計算の堅牢化
- `OnRenderPropertyChanged` がコンストラクタ初期化中の InitializeComponent 前に発火する可能性（BindableProperty の `defaultValue` 経由）。`if (ViewModel is null) return;` のガードを早期に確認。

## 4. 優先順位付きロードマップ（提案）

| Phase | 取り組み | 主要参考 |
| ---- | ---- | ---- |
| 1 | 選択機能 (Single/Multi/Range)、`FirstDayOfWeek` Bindable 化、`MinDate`/`MaxDate`、`GoToToday` | XCalendar / Xamarin.Plugin.Calendar |
| 2 | ローカライズ（`Culture` プロパティ）、曜日ヘッダー動的生成、リソース化 | Plugin.Maui.Calendar |
| 3 | `DayTemplate` / `EventTemplate` / `HeaderTemplate` などテンプレート化、Style 集約 | XamarinForms.CalendarControl / Plugin.Maui.Calendar V2 |
| 4 | `CalendarLayout` (Month/TwoWeeks/Week)、スワイプ、アニメーション、+N more インジケータ | Xamarin.Plugin.Calendar |
| 5 | アクセシビリティ、テストプロジェクト、描画最適化（仮想化） | XCalendar.Core.Tests |

## 5. 採用しない／優先度を下げる項目

- **完全な Skia 自前描画への移行**: 既設計（`CalendarDesign.md` §3.2）の方針に反する。タッチ拡張容易性を損なうため不採用。
- **Xamarin.Forms 専用 API（`StackOrientation` 等）の直輸入**: .NET MAUI / .NET 10 に合わせて型を選定。
- **イベント編集 UI 一式の内製**: ライブラリ責務外。サンプル `MainPage` 側で実装する。

---

以上、各ライブラリの強みを参考に、WorkCalendar が「テンプレート差し替え可能・選択/範囲対応・多言語・ナビ制限」のあるリッチな MAUI カレンダーへ進化するための機能・改善点をまとめた。
