# Feature14Evaluation — エージェントの評価 (LocalEvaluator)

> Microsoft Agent Framework 1.0 GA（本サンプルは安定版 **1.10.0**）/ .NET 10 / 題材: PC情報取得
> ドキュメント記載は **2026-06-20 時点**の確認に基づく。

- **API 区分**: 安定版
- **追加環境**: 不要（**検査はローカル**。評価のための追加 API 呼び出しなし。エージェント実行には既存の Foundry 接続を使用）
- **追加パッケージ**: なし

エージェントの応答が期待どおりかを、**ローカルの検査関数だけ**で自動採点する例です。

## このサンプルで分かること

- `LocalEvaluator` がローカルの検査だけで判定するため、**評価のための追加 API 呼び出しが不要**なこと
- `EvalChecks` に組み込みの検査があること（キーワード／ツール呼び出し／非空 など）
- `agent.EvaluateAsync(質問列, evaluator)` でエージェントを実行し、応答を採点できること
- `Passed` / `Total` は**項目（質問）単位**で、1 項目内のいずれかの検査に落ちるとその項目は不合格になること

> より高度な「LLM as judge」型の評価は、別パッケージ `Microsoft.Extensions.AI.Evaluation` を使います（本サンプルは不使用）。

## 中核 API

| API | 役割 |
| --- | --- |
| `EvalChecks.NonEmpty / KeywordCheck / ToolCalledCheck` | 組み込みのローカル検査 |
| `new LocalEvaluator(checks)` | ローカル検査で構成した評価器 |
| `AIAgent.EvaluateAsync(質問列, evaluator)` | 実行＋採点 |
| `AgentEvaluationResults { Passed, Total, AllPassed }` | 集計結果（項目単位） |

## 動かし方

接続設定は[リポジトリ直下の README](../README.md#設定全サンプル共通) を参照。

```powershell
dotnet run --project Feature14Evaluation
```

## 期待される動作（実機確認済み）

```
=== 評価結果 ===
合格した項目数: 2 / 2
全項目が合格  : はい
```

## 参考（一次情報）

- 公式ドキュメント / 公式サンプル（`08.EvaluationAndTracing`）: <https://learn.microsoft.com/en-us/agent-framework/> / <https://github.com/microsoft/Agent-Framework-Samples>
