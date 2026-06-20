# AIエージェント風 TUI ライブラリ検証サンプル (.NET 10)

.NET 10 のコンソールアプリで「最近の AI エージェント CLI 風」の TUI を作るにあたり、
主要な TUI ライブラリを **同一の擬似エージェント** で実装し、使い勝手・表現力・実装コストを比較するためのサンプル集です。

> バックエンドは **擬似エージェント (シミュレート)** で API キー不要。
> 思考中表示・ツール呼び出し・トークンのストリーミングを再現します。
> 実 LLM への差し替え口 (`IAgentConversation`) を用意してあります。
>
> ※ 同フォルダの `README.md` は別サンプル群 (Microsoft Agent Framework 機能別) の文書です。本ファイルは TUI 検証用です。
>
> **本書は 2026-06-20 時点**。各パッケージの版数・提供状況は同日に NuGet (api.nuget.org) で確認しています。
> prerelease/beta のものは API が変わり得ます。記載のサンプルコードは規約準拠の雛形です。

## プロジェクト構成

| プロジェクト | ライブラリ | 形態 | 概要 |
|---|---|---|---|
| `TuiAgentSampleCore` | (BCL のみ) | classlib | 擬似エージェント中核・共通モデル・ロゴ等 |
| `TuiSpectreConsoleAgentSample` | Spectre.Console `0.57.0` | exe | 出力ストリーム型。`Live` で 1 応答を更新描画 (基準実装) |
| `TuiTerminalGuiAgentSample` | Terminal.Gui `2.4.7` | exe | フルスクリーン。色分けトランスクリプト + 入力欄 + ステータス |
| `TuiSharpConsoleUiAgentSample` | SharpConsoleUI `2.4.79-rc.5` | exe | マルチウィンドウ (会話 + アクティビティの 2 枚) |
| `TuiConsoloniaAgentSample` | Consolonia `11.3.12.6` (Avalonia 12) | exe | XAML + MVVM + データバインディングで宣言的に構築 |

## 実行方法

```bash
dotnet run --project TuiSpectreConsoleAgentSample
dotnet run --project TuiTerminalGuiAgentSample
dotnet run --project TuiSharpConsoleUiAgentSample
dotnet run --project TuiConsoloniaAgentSample
```

- 操作: メッセージを入力して Enter。`/clear` で消去、`/exit` で終了。
- 「PC のスペックを教えて」等と入力すると `get_system_info` ツール呼び出し (実マシン情報) を再現します。
- **フルスクリーン系 (Terminal.Gui / SharpConsoleUI / Consolonia) は実ターミナルが必要** です
  (パイプ等のリダイレクト環境では起動できません)。SharpConsoleUI / Consolonia は横幅 120 以上を推奨。
- Spectre.Console はリダイレクト時のフォールバックを実装しており、非対話でも 1 応答を確認できます。

## 共通中核 (`TuiAgentSampleCore`)

「最近のエージェント」の表示要素をイベント列としてモデル化し、各 TUI はこれを購読して描画します。

- `IAgentConversation.SendAsync(...)` → `IAsyncEnumerable<AgentEvent>`
- `AgentEvent` 派生: `ThinkingStarted` / `ThinkingDelta` / `ThinkingCompleted` /
  `ToolCallStarted` / `ToolCallCompleted` / `TextDelta` / `ResponseCompleted`
- `SimulatedAgentConversation` … 擬似実装 (思考 → ツール → 本文ストリーム)
- `MarkupFormatter` … 簡易 Markdown を Spectre/SharpConsoleUI 互換マークアップ / 素テキストへ変換
- `AgentBranding` … 起動ロゴ (ASCII アート)・タイトル・ヒント

**実エージェントへの差し替え**: `IAgentConversation` を実装した別クラス
(例: `Microsoft.Extensions.AI.IChatClient` のラッパー) を用意し、各サンプルの `CreateAgent()`
の戻り値を差し替えるだけです (UI 側は無変更)。

## ライブラリ評価

### Spectre.Console 0.57.0
- **形態**: 出力ストリーム型 (画面バッファやイベントループを持たない)。
- **長所**: 導入が最も簡単。`Panel`/`Rule`/`Markup`/`Live`/`Status` でリッチな表示が即書ける。コミュニティ最大。
- **短所**: 本質的に「出力専用」。`TextPrompt` は対話端末必須 (リダイレクト入力で例外)、
  `Live` はカーソル制御が必要 (リダイレクト出力で例外)。Markdown レンダラは内蔵されないため自前変換が必要。
- **本サンプルの方針**: 応答は**枠 (Panel) なしのプレーン描画**。装飾は ASCII (`>` `-` `|`) で桁ずれを回避。
- **エージェント適性**: 「1 応答を流す」型の表示に最適。Claude Code 風の逐次表示が最小コストで作れる。

### Terminal.Gui 2.4.7
- **形態**: フルスクリーン・イベントループ型。成熟した定番。
- **注意点 (v2)**: 名前空間が再編 (`Terminal.Gui.App` / `.Views` / `.ViewBase` / `.Drawing`)、
  静的 `Application` は **`[Obsolete]`** → インスタンス API (`Application.Create()` / `IApplication`) を使用。
  配色は `ColorScheme` 廃止 → `Scheme` / `View.SetScheme(...)` / `SchemeName` に刷新。
- **長所**: コントロール群が最も充実。入力編集・スクロール・複数ペインに強い。
- **短所**: `TextView` は単一配色のため、行ごとに色を変えるには色付き `Label` を積む等の工夫が必要
  (本サンプルは色付きブロックを縦積みし手動スクロール)。
- **エージェント適性**: 本格的な対話画面・ダッシュボードに向く。

### SharpConsoleUI 2.4.79-rc.5 (prerelease)
- **形態**: ターミナルを描画面とする「レトインドモード GUI フレームワーク」。
- **長所**: **マルチウィンドウ + ウィンドウ毎の非同期スレッド**が最大の差別化点。
  Spectre 風マークアップが全コントロールで使え、fluent ビルダーが充実。
  本サンプルは「会話」ウィンドウと「アクティビティ (思考/ツール)」ウィンドウを並置し、
  後者は専用スレッドでスピナーを回す。
- **短所**: prerelease で API 流動的・コミュニティ小規模。コントロールは `IDisposable`。
  `Color("名前")` は使えず **16 進 (`#RRGGBB`) 必須** (色名を渡すと実行時例外) 等、API に癖がある。
- **CJK での注意 (実機検証で判明)**:
  - **枠ずれ**: `◆ ⚙ ✓ … —` 等の「East Asian Ambiguous / 絵文字幅」文字は、CJK 端末では幅 2 で描画される一方、
    ライブラリ内部の幅計算は幅 1 とみなすことがあり、その行だけ右枠がずれる。曖昧幅を吸収する設定は無いため、
    **装飾は ASCII (`>` `+` `-` `...`) に寄せる**のが確実 (本サンプルは対応済み。ロゴのブロックアートとブライユのスピナーは幅 1 で安全なため保持)。
  - **灰文字が背景に埋もれる**: 既定テーマの背景色に依存。`WithColors(前景, 背景)` で**暗背景を明示**すると解決
    (本サンプルは黒背景 + シルバー前景 + スティールブルー枠に設定済み)。
- **エージェント適性**: 複数ペインを並べる監視ダッシュボード型に好適。ただし日本語中心の用途では上記の調整が前提。

### Consolonia 11.3.12.6 (Avalonia 12 / beta)
- **形態**: AvaloniaUI をコンソールへ。**XAML + データバインディング + MVVM** がそのまま使える。
- **長所**: 宣言的にレイアウト・スタイル・テーマ・バインディングを記述。WPF/Avalonia の知見を活かせる。
  本サンプルは `CommunityToolkit.Mvvm` で ViewModel を組み、役割ごとのブラシで配色。
- **短所**: beta。`FluentTheme` は非推奨 (→ `ModernTheme`)。ピクセル指向の描画 (変形・複雑図形) は不可。
  厳格アナライザ下では型を `internal` にする等の対応が要る (CA1515)。
- **エージェント適性**: XAML/MVVM 資産・知見があるチームに最適。状態とビューの分離が綺麗。

## 比較まとめ

| 観点 | Spectre.Console | Terminal.Gui | SharpConsoleUI | Consolonia |
|---|---|---|---|---|
| 形態 | 出力ストリーム | フルスクリーン | マルチウィンドウ GUI | XAML/MVVM |
| 導入難度 | ◎ 易 | ○ | △ (prerelease) | △ (XAML 学習) |
| 入力/編集 | △ (簡易) | ◎ | ○ | ○ (TextBox) |
| 行単位の配色 | ◎ (markup) | △ (要工夫) | ◎ (markup) | ◎ (bind) |
| 複数ペイン | △ | ○ | ◎ | ○ |
| 成熟度 | ◎ | ◎ | △ | ○ |
| ヘッドレス動作 | 一部可 | 不可 | 不可 | 不可 |

### 推奨の指針
- **手軽に “流れる” エージェント表示**: Spectre.Console
- **本格的な対話画面・編集/スクロール重視**: Terminal.Gui
- **複数ウィンドウのダッシュボード型**: SharpConsoleUI
- **XAML/MVVM で宣言的に**: Consolonia

## 使いやすさの総合判定 (ランキング)

「.NET 10 で最近の AI エージェント風 TUI を作る」目的での、導入〜実装の容易さの判定です。

1. **Spectre.Console — 最も使いやすい (本命)**
   学習コスト・記述量が最小。`Live`/`Panel`/`Markup` が「1 応答を逐次表示する」エージェント UI に素直に合う。
   レイアウト計算・破棄・スレッド marshaling の苦労がほぼ無く、幅計算 (CJK/絵文字) も比較的素直。
   弱点は完全なアプリシェルではない点 (入力編集・スクロールバック・複数ペインは弱い)。
2. **Terminal.Gui — 本格対話アプリ向け (学習コスト中)**
   コントロール最多・成熟。入力編集/スクロール/複数ペインに強い。v2 の API 刷新 (インスタンス API・`Scheme`) と
   行単位配色の自作、厳格アナライザ対応で記述量は増える。
3. **Consolonia — XAML/MVVM 経験者に好適**
   宣言的でビューとロジックの分離が綺麗。ただし Avalonia-on-console は beta、XAML 知識前提、非推奨テーマ等の注意点あり。
4. **SharpConsoleUI — 見た目は最も魅力的、現状は最も要注意**
   マルチウィンドウ + ウィンドウ毎スレッドは唯一無二で見栄えも良い。一方で prerelease であることに加え、
   **日本語環境では「枠ずれ (曖昧幅)」「灰文字の視認性」の調整が必須** (上節参照)。動かし込むまでの手間が最も大きい。

**結論**: 使いやすさ最優先なら **Spectre.Console**。リッチな見た目を最優先し調整を許容できるなら SharpConsoleUI
(CJK では装飾を ASCII へ・暗背景を明示)。本格的な対話アプリは Terminal.Gui、XAML 派は Consolonia。

## 参考にした実装

- [RazorConsole](https://github.com/RazorConsole/RazorConsole) — Razor + Spectre.Console で
  「Claude Code 風」チャット (`examples/LLMAgentTUI`)。`Microsoft.Extensions.AI` 連携・逐次更新・パネル構成を参考。
- [SharpConsoleUI (ConsoleEx)](https://github.com/nickprotop/ConsoleEx) — 公式ドキュメント / 比較ページ。
- [Consolonia](https://github.com/Consolonia/Consolonia) — 公式サンプル (`src/Tests/Sandbox`) のブートストラップを参照。
- [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui) — v2 ドキュメント。

## 備考 (コーディング規約)

ルート `Directory.Build.props` が全サンプルに StyleCop + `AnalysisMode=All` + `WarningsAsErrors=nullable` を強制します。
全プロジェクトは **警告ゼロ** でビルドされます (`dotnet build WorkAgent.slnx`)。
コンソール用途として `CA2007`/`CA1303` のみ各 exe の `GlobalSuppressions.cs` で抑制しています
(既存 `BasicAgentSample` と同方針)。それ以外の警告はコード側で解消しています。

**CJK 端末での桁ずれ対策**: East Asian Ambiguous / 絵文字幅の文字 (`◆ ⚙ ✓ … — · • │ › ▌ →`) は
日本語端末で幅 2 で描画されライブラリの幅計算 (幅 1) と食い違うため、全サンプルで装飾記号を
**ASCII (`> - | -> ...`)** に統一しています (起動ロゴのブロックアートとブライユのスピナーは幅 1 で安全なため保持)。
全角の中点 `・` や `「」` 等は確定幅 2 で食い違わないためそのまま使用しています。

**配色の役割分担 (暗背景前提)**: 応答本文とアプリ出力を色で区別します。
- **応答の文章 = 白** (見出しは bold white)。**コードは aqua**。
- **アプリ的な出力 = 白以外**: トークン/時間・ツール結果・タグライン・tips・記号は **silver** (明るめグレー)、
  役割見出し (you / assistant / tool) は green / aqua / blue、思考・状態は yellow。
- `grey` (#808080) は暗背景で埋もれるため不使用 (silver #C0C0C0 を使用)。
- Terminal.Gui はアシスタントを「見出し (シアン) + 本文 (白)」の 2 ブロックに分割。配色は ANSI パレット名で、
  明るい `Gray` (idx 7) を使用し暗い `DarkGray` (idx 8) は不使用。SharpConsoleUI は背景を `#0F1117` に明示。
