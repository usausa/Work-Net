# WorkCalendar カレンダーUI 実装方針

## 1. 目的

`feature-calendar.png` で示される月間カレンダーUIを、.NET MAUI 上で実装する。
本ドキュメントは、レイアウト方式・拡張ポイント・データ構造の方針をまとめる。

## 2. 画面要件の整理

参照画像から読み取れる機能要素:

| 区分 | 内容 |
| ---- | ---- |
| ヘッダー | 年/月表示、ナビゲーション（カレンダーアイコン＋下向き▼） |
| 曜日ヘッダー | 月〜日の7列（**月曜始まり**）。日=赤、土=青 |
| カレンダー本体 | 6週 × 7日のグリッド |
| 日付セル | 日付数字（休日色／土曜色／平日色を区別）、当日ハイライト、選択ハイライト、月外日付の薄表示 |
| 単日スケジュール | セル内に縦積みのカラーラベル（例: 燃えるゴ、ジム、英会話） |
| 複数日スケジュール | 複数日にまたがる横長バー（例: 30-31「会社研修」、6-8「会社休み」、14-16「友達泊まり」、26-1「海外出張」） |
| スタンプ | 日付セル内に貼り付けられた装飾画像（犬、旗、飛行機、ハート、絵文字など）。位置はセル中央寄りで、ラベルとも重なり得る |
| 月インジケータ | カレンダー中央に薄く「6」と表示される装飾 |
| ボトムバー | カレンダー／ToDo／＋／ノート＆日記／その他 |

## 3. レイアウト方式の検討

### 3.1 検討した選択肢

| 案 | 概要 | 長所 | 短所 |
| ---- | ---- | ---- | ---- |
| A. Grid ベース（採用） | 6行×7列 `Grid` を骨格にし、各日セルに MAUI コントロールを配置 | 標準コントロールでタッチイベントが容易、レスポンシブが自然、テーマ反映が楽 | 複数日バーは工夫が必要（ColumnSpan＋スロット計算） |
| B. AbsoluteLayout | 全要素を絶対座標で配置 | 複数日バーの座標計算が直接できる | レスポンシブ手動、要素ごとの幅計算が必要、コストが高い |
| C. `GraphicsView` / SkiaSharp で自前描画 | 全部を1枚のキャンバスに描画 | 描画パフォーマンス高い、自由度大 | タッチヒットテストを自前実装する必要があり、後述の「タッチイベント拡張」要件と相性が悪い |
| D. Custom Layout | 自前で `Layout` を実装 | 最適化可能 | 実装コストが大きい。サンプル段階では過剰 |

### 3.2 採用方針: 「Grid ベース」

理由:

- 要件「**スケジュール項目はタッチイベントで反応するように拡張できること**」を最優先する。
  自前描画（案C）にすると、項目ごとの当たり判定・状態管理・ハイライト・コンテキストメニュー等の追加コストがすべて自前実装になる。
- 標準コントロール（`Label` / `Image` / `Border` 等）ならば、`TapGestureRecognizer` / `PointerGestureRecognizer` をその場で `GestureRecognizers` に追加するだけで拡張できる。
- `Grid` の `ColumnSpan` / `RowSpan` で複数日バーやスタンプの覆い被せ表現も可能。
- 将来的に「セルだけ Skia 描画にしたい」となったとき、セルを `SKCanvasView` に差し替えるだけで部分採用できる（逃げ道あり）。

## 4. レイアウト構造（採用案の詳細）

```
ContentPage
└─ Grid (RowDefinitions: Auto, Auto, *, Auto)
   ├─ [Row 0] ヘッダー（年月、メニュー）
   ├─ [Row 1] 曜日ヘッダー（7列 Grid）
   ├─ [Row 2] カレンダー本体（VerticalStackLayout または Grid 6行）
   │           └─ WeekRow × 6
   └─ [Row 3] ボトムナビゲーション
```

### 4.1 WeekRow（1週間ぶん）

各週は以下の構造を持つ。

```
WeekRow (Grid)
  ColumnDefinitions: * * * * * * *  (7等分)
  RowDefinitions:    Auto             ← 日付番号 + スタンプエリア（同一セル内に重ねる）
                     Auto             ← スロット1（複数日バーまたは単日項目）
                     Auto             ← スロット2
                     Auto             ← スロット3 … 必要なだけ
                     *                ← 余白吸収
```

- **日付番号**: Row=0、各列に `Label`。色は日/土/平日/月外/今日/選択 で切り替え。
- **スタンプ**: Row=0 と同じ位置に `Image` を重ねる。`HorizontalOptions` / `VerticalOptions` でセル内の配置を制御。`InputTransparent="True"` にして、下のセルタップを邪魔しない（ただしスタンプ自体をタップしたい場合は `False` にして個別ハンドラを付ける）。
- **スケジュール項目**: 「**スロット**」で衝突回避する。
  - 1週間内に存在する全項目（単日／複数日）について、開始日順にソートしながら空いている最小スロット行を割り当てる。
  - 単日項目 → `Grid.Row=slotRow, Grid.Column=dayCol, ColumnSpan=1`
  - 複数日項目 → `Grid.Row=slotRow, Grid.Column=startCol, ColumnSpan=endCol - startCol + 1`
  - 週をまたぐ項目は、週境界で分割して各週に別個の Label として配置（左右の角丸を切り替えて連続感を出す）。
- **スケジュール項目の表現**: `Border`（角丸・背景色）に `Label`（テキスト）を内包する形を 1 ユニットとして再利用。`Border` に `TapGestureRecognizer` をぶら下げてタッチ拡張に備える。

### 4.2 日付セル背景

- セル背景は `BoxView` または `Border` を `RowSpan` で敷き、`Grid.Column=dayCol, Grid.Row=0, Grid.RowSpan="<最終スロット>"` で全行をカバー。
- 当日ハイライト・選択ハイライトはこの背景の色・枠線を切り替える。
- 背景 `Border` 自体にも `TapGestureRecognizer` を付け、空き領域タップで「日付選択」が動作するようにする。

## 5. 拡張ポイント（タッチイベント）

| 対象 | 標準コントロール | タッチ拡張の付け方 |
| ---- | ---- | ---- |
| 日付セル空白部 | 背景 `Border` | `Border.GestureRecognizers` に `TapGestureRecognizer` |
| 単日スケジュール | `Border` + `Label` | 同上。CommandParameter にイベントID |
| 複数日スケジュール | 横長 `Border` + `Label` | 同上 |
| スタンプ | `Image` | `Image.GestureRecognizers` に `TapGestureRecognizer`（必要なら） |

**ポイント**: 自前描画にせず、上記のように MAUI コントロールに 1:1 でモデルを対応付けることで、「あとでタップ／長押し／ドラッグを足す」が容易。
逆に `GraphicsView` / SkiaSharp で 1 枚絵にすると、項目1つにつき矩形リストを持って `Touch` イベント内で hit-test → ヒット項目に対応するハンドラ起動、という配管を自前で書く必要があり、サンプル段階の負担が大きいため不採用。

## 5.5 週の開始

- **月曜始まり**（`DayOfWeek.Monday`）を採用する。
- 列インデックスは `((date.DayOfWeek - Monday + 7) % 7)` で算出する（月=0 … 日=6）。
- 月の最初の表示行は、その月の 1 日を含む直近の月曜から開始する。
- `MonthViewBuilder` のコンストラクタ引数で切り替え可能にしておく（後で日曜始まりに戻したい場合に備える）。

## 6. データ構造

```
MonthViewModel
  Year, Month
  Weeks: WeekViewModel[6]
    Days: DayViewModel[7]
      Date, IsCurrentMonth, IsToday, IsHoliday, DayKind (Sun/Sat/Weekday)
      Stamps: StampViewModel[]   ← 画像パス、配置オプション
      SingleEvents: EventViewModel[]   ← セル内に縦並びする単日項目
    SpanningEvents: SpanEventViewModel[]   ← 週内に存在する複数日項目（startCol, span, slot）
```

- スロット番号 (`slot`) は、ViewModel 生成時にレイアウトロジックで計算してから格納する。
- 複数日項目が週境界をまたぐ場合は、各週の `SpanningEvents` に分割した断片として登録する。

## 7. 今回サンプルで実装するスコープ

サンプル実装としては以下を含める。除外したものは後段で追加可能な構造にしておく。

**含める**

- 月固定（画像と同じ 2019/6 をハードコードまたは初期表示）
- 月名ヘッダーと曜日ヘッダー
- 6×7 のグリッド
- 日付の色分け（日/土/平日/月外/今日）
- 単日スケジュール（縦積みカラーラベル）
- 複数日スケジュール（同一週内＋週またぎ分割）
- スタンプ画像配置（プレースホルダで OK）
- 各スケジュールに `TapGestureRecognizer` を仕込んでおく（ハンドラは `Debug.WriteLine` 程度）

**今回スコープ外（拡張ポイントだけ確保）**

- 月切り替えナビゲーション
- スケジュール追加・編集ダイアログ
- スタンプの実画像差し替え／配置エディタ
- ボトムタブの遷移（ガワだけ置く）

## 8. プロジェクト構成（追加予定ファイル）

```
WorkCalendar/
├─ Docs/
│  └─ CalendarDesign.md          ← 本ファイル
├─ Models/
│  ├─ ScheduleEvent.cs           ← 単日/複数日共通のイベントモデル
│  ├─ Stamp.cs                   ← スタンプモデル
│  └─ DayKind.cs                 ← 曜日/休日種別
├─ ViewModels/
│  ├─ MonthViewModel.cs
│  ├─ WeekViewModel.cs
│  ├─ DayViewModel.cs
│  └─ Layout/WeekLayoutBuilder.cs   ← スロット計算
├─ Views/
│  ├─ CalendarPage.xaml(.cs)        ← MainPage を差し替え or 別ページ
│  ├─ Controls/WeekRowView.xaml(.cs)
│  └─ Controls/DayCellView.xaml(.cs)
└─ Services/
   └─ SampleDataProvider.cs       ← 画像と同じデータを返すモック
```

## 9. 留意点

- **コーディングスタイル**: `.editorconfig` に従う。インスタンスフィールドの `_` プレフィックス禁止（AGENTS.md の規約）。
- **ビルド警告ゼロ**: 警告抑制が必要になったら事前確認する。
- **Nullable**: csproj で有効。プロパティ初期化や `required` を活用。
- **XAML ソースジェネレータ**: csproj で `MauiXamlInflator=SourceGen` 有効。命名・部分クラスに注意。

## 10. 次ステップ

1. 本方針について確認をもらう。
2. Models / ViewModels / SampleDataProvider を実装。
3. `WeekRowView` / `DayCellView` の XAML を実装。
4. `CalendarPage` を組み立て、`MainPage` を差し替え（または Shell ルートを更新）。
5. 動作確認用にビルド／実行。
