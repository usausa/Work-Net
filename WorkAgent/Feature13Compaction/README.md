# Feature13Compaction — 会話履歴の圧縮 (Compaction)

> Microsoft Agent Framework 1.0 GA（本サンプルは安定版 **1.10.0**）/ .NET 10 / 題材: PC情報取得
> ドキュメント記載は **2026-06-20 時点**の確認に基づく。

- **API 区分**: ⚠ **実験的（Experimental・診断 `MAAI001`）** … 「評価目的のみ・将来変更/削除あり」。
  本サンプルは該当箇所のみ `#pragma warning disable MAAI001` で局所抑制しています。本番採用時は提供状況を要確認。
- **追加環境**: 不要（**決定的ストラテジ**を使うため LLM の追加呼び出しなし）
- **追加パッケージ**: なし

会話が長くなったときに、古い履歴を**自動で間引いて**文脈ウィンドウを抑える例です。

## このサンプルで分かること

- `CompactionProvider` に圧縮ストラテジを渡すと、毎回の実行前に履歴を圧縮できること
- `SlidingWindowCompactionStrategy` は古いユーザーターンから順に削る（**LLM 不要・決定的**）こと
- トリガーは `CompactionTriggers`（例: `MessagesExceed(6)`）で指定できること
- 圧縮の効果として、十分古いやり取りは文脈から外れること（＝「最初の質問は?」に正しく答えられなくなる）

> 履歴を消さずに**要約で残したい**場合は `SummarizationCompactionStrategy` を選べます（こちらは LLM を使用）。

## 中核 API

| API | 役割 |
| --- | --- |
| `new SlidingWindowCompactionStrategy(trigger, minTurns)` | 間引きストラテジ（**実験的**） |
| `CompactionTriggers.MessagesExceed(n)` ほか | 圧縮の発火条件（**実験的**） |
| `new CompactionProvider(strategy)` | 圧縮を行うプロバイダー（**実験的**） |

## 動かし方

接続設定は[リポジトリ直下の README](../README.md#設定全サンプル共通) を参照。

```powershell
dotnet run --project Feature13Compaction
```

## 期待される動作（実機確認済み）

5 ターン会話した最後に「一番最初の質問は?」と尋ねると、古い履歴が圧縮で外れているため、
最初の質問ではなく**直近の質問**を答えます（＝間引きが効いている証拠）。

## 参考（一次情報）

- 公式ドキュメント: <https://learn.microsoft.com/en-us/agent-framework/>
