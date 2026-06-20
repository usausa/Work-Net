# Feature08Telemetry — 可観測性 (OpenTelemetry によるトレース)

> Microsoft Agent Framework 1.0 GA（本サンプルは安定版 **1.10.0**）/ .NET 10 / 題材: PC情報取得
> ドキュメント記載は **2026-06-20 時点**の確認に基づく。

`UseOpenTelemetry()` をパイプラインに足し、エージェントの実行やツール呼び出しを
**OpenTelemetry の Activity（スパン）**として記録する例です。

## このサンプルで分かること

- `AsBuilder().UseOpenTelemetry(sourceName)` で計測を有効化できること
- 実行が GenAI セマンティック規約に沿ったスパン（`invoke_agent`、`chat` など）として記録されること
- 本番では OTLP エクスポーター等で収集するが、ここでは**追加依存なし**で
  `System.Diagnostics.ActivityListener` により購読してコンソール表示していること
- `EnableSensitiveData = true` でプロンプト／応答内容もタグに含められること（機微情報のため本番は取扱注意）

## 中核 API

| API | 役割 |
| --- | --- |
| `AIAgentBuilder.UseOpenTelemetry(sourceName, configure)` | 計測の追加 |
| `OpenTelemetryAgent.EnableSensitiveData` | プロンプト/応答をタグへ含めるか |
| `System.Diagnostics.ActivityListener` | スパンの購読（簡易表示用） |

## 動かし方

接続設定は[リポジトリ直下の README](../README.md#設定全サンプル共通) を参照。

```powershell
dotnet run --project Feature08Telemetry
```

## 期待される動作（実機確認済み）

```
[trace] chat gpt-5.4-mini (3229 ms)
[trace] invoke_agent PcInfoAssistant(…) (3271 ms)
          gen_ai.operation.name = invoke_agent
          …
Agent > （回答）
```

## 参考（一次情報）

- 公式ドキュメント（Observability / Tracing）: <https://learn.microsoft.com/en-us/agent-framework/>
- 公式サンプル（`08.EvaluationAndTracing`）: <https://github.com/microsoft/Agent-Framework-Samples>
