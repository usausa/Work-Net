# Calendar Performance Improvement Plan

Build 最適化後のログ分析に基づく改善計画。

## 1. 現状サマリー

| 月 | LoadMonth | Build | Render | RebuildWeeksHost | WeekdayLabels |
|---|---:|---:|---:|---:|---:|
| 2026/03 (データなし) | 94.27ms | 0.80ms | 89.95ms | 48.55ms | 37.96ms |
| 2026/04 (データあり) | 283.94ms | 1.23ms | 274.46ms | 235.95ms | 35.41ms |

Build は既に約 1ms 前後で十分な水準。残りの遅延はすべて View 側 (`CalendarView.Render`) の中で発生している。

## 2. ホットスポットの内訳

### データありの週 (週あたり 27〜64ms)

```text
[UpdateWeekRow] 04/27 | DayCells: 1.32ms | ClearDynamic: 0.00ms | Stamps: 0.02ms | Events: 61.31ms | Total: 63.71ms
[UpdateWeekRow] 03/30 | DayCells: 2.82ms | ClearDynamic: 0.01ms | Stamps: 1.45ms | Events: 39.21ms | Total: 45.37ms
```

- **Events が支配的**。`Border + Label` を毎回 `new` してプロパティを設定し、`Grid.Children.Add()` を回している。

### データなしの週 (週あたり 6〜13ms)

```text
[UpdateWeekRow] 02/23 | DayCells: 2.97ms | ClearDynamic: 7.78ms | Stamps: 0.11ms | Events: 0.01ms | Total: 12.53ms
```

- データが無いのに **ClearDynamic が 4〜8ms** 発生。スタンプ Label のプールが毎フレーム `Remove` -> 再 `Add` されているのが原因。

### 共通

- `WeekdayLabels` が約 36ms。月切り替えに不変なのに毎回 `Clear` + `new Label×7`。
- `Header` が約 3ms。FontSize/Color/Width など不変値を毎回再代入。
- `DayCells` が週あたり 1〜3ms。背景・日付バブル・ジェスチャの再設定。

## 3. 改善候補リスト

| # | 項目 | 期待効果 (目安) | 優先度 |
|---|---|---:|---|
| 1 | 曜日ヘッダーを初期化時のみ生成し、月切り替えでは更新不要 | -35ms / 切り替え | 高 |
| 2 | イベント View をプール化 (`Border` + `Label` 再利用) | -150〜200ms / データ月 | 高 |
| 3 | スタンプ Label の毎回 `Remove`/`Add` を撤廃し、可視性のみ切替 | -25〜45ms / 月 | 高 |
| 4 | Header の不変プロパティ設定を初期化時のみ実行 | -3ms / 切り替え | 中 |
| 5 | `DayCell` の `Color` / `IsVisible` の差分更新化（同一値なら代入しない） | -5〜10ms / 月 | 中 |
| 6 | `Border.StrokeShape` の `RoundRectangle` を再利用 (イベントごとに new しない) | GC圧軽減 | 中 |
| 7 | `Stamp Label` のプロパティを差分更新（テキスト・サイズ変化なしならスキップ） | -1〜3ms / 月 | 低 |
| 8 | `Render` の `Math.Max(static w => w.SlotCount)` を Build 時に算出して `MonthViewModel.SlotCount` に保持 | -0.02ms / 月 | 低 |
| 9 | `Border.StrokeThickness = 0` 等の不変設定を XAML 側に移し、コードからの再代入を排除 | わずか | 低 |
| 10 | `Render` 完了後の `GC.Collect()` 抑制のため、ホットパスでの `Color.FromArgb(string)` 等の文字列パース呼び出しを排除（毎回 new せず static フィールド化） | GC回数削減 | 中 |
| 11 | 大量イベント時のみ `BatchBegin/BatchCommit` を Grid に適用してレイアウトパスを集約 | 測定後判断 | 中 |
| 12 | `CalendarView.WeeksHost` の `IsVisible` を一括 off にしてから書き換える | 測定後判断 | 低 |

## 4. 実装の優先順

### Phase A (即効性が高い)

1. **項目1: 曜日ヘッダーの固定化**
   - 初回 + `FirstDayOfWeek`/`Culture` 変更時のみ更新
   - 期待: 約 -35ms / 切り替え

2. **項目2: イベント View プール化**
   - `WeekRowVisual` に `EventBorderPool` を持たせ、`AddEventViews` で再利用
   - `BuildEventView` の中で `new Border` / `new Label` / `new RoundRectangle` をやめ、既存インスタンスのプロパティを上書き
   - 期待: 約 -150〜200ms / データ月

3. **項目3: スタンプの可視性切替化**
   - `AddStampViews` で `Remove` + `Add` をやめ、初期化時に Grid に追加済みの Label を `IsVisible` と `Text` で切り替える
   - 期待: 約 -25〜45ms / 月

### Phase B (中粒度の改善)

4. **項目4: Header 不変プロパティの初期化時固定**
5. **項目5: DayCell の差分更新**
6. **項目6: StrokeShape の使い回し**

### Phase C (微調整・割り当て削減)

7. **項目8: SlotCount 事前計算**
8. **項目10: Color 文字列パースの static 化（CalendarView 内の `Color.FromArgb(...)`）**
9. **項目11/12: Batch / Visibility による集約**（実装後に再計測して判断）

## 7. Phase A 実施結果

実施日: 現在の実装

### 変更内容

| # | 変更 | 対象メソッド |
|---|---|---|
| 1 | 曜日ヘッダーをコンストラクタで一度だけ生成。`FirstDayOfWeek`/`Culture` 変更時のみ再生成 | `UpdateWeekdayHeaderLabels()` を `Render` から削除 |
| 2 | `EventBorderVisual` クラスを導入し `WeekRowVisual.EventPool` で週ごとに保持・再利用 | `AddEventViews`, `BuildEventView` 削除 |
| 3 | スタンプ Label の `Remove`/`Add` を廃止。初回のみ `Grid.Children.Add`、以後 `IsVisible` 切替 | `AddStampViews`, `BuildStampView` 削除 |
| 4 | `ClearDynamicViews` を `HideDynamicViews` に置換（Remove なし、IsVisible のみ） | `WeekRowVisual.HideDynamicViews()` |

### 期待効果

| 指標 | 改善前(推計) | 改善後(期待) |
|---|---:|---:|
| WeekdayLabels (全体) | ~36ms | ~0ms (初回のみ) |
| Events (週・データあり) | ~25〜61ms | ~1〜3ms (プロパティ更新のみ) |
| ClearDynamic (週・データなし) | ~4〜8ms | ~0ms (Remove 不要) |
| データなし月 Render | ~90ms | ~20ms 以下 |
| データあり月 Render | ~280ms | ~60ms 以下 |

実機で `[Render]` / `[UpdateWeekRow]` ログを再採取して目標値と照合すること。


- データなし月: 90ms → **20ms 以下**
- データあり月: 280ms → **60ms 以下**
- 月切り替えごとの GC を 0 回 (現状 3 回)

## 6. 検証手順

1. 各 Phase の最初に既存ベンチマーク (`WorkCalendar.Benchmarks`) に加え、実機ログ (`[Render]` / `[UpdateWeekRow]`) を再採取
2. Phase 完了ごとに以下を記録
   - LoadMonth Total / Build / Render
   - 週ごとの Events / Stamps / DayCells
   - GC ログ件数
3. 目標値到達 or 体感改善で完了
