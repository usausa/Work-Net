# Feature04Middleware — ミドルウェア (エージェントのパイプライン)

> Microsoft Agent Framework 1.0 GA（本サンプルは安定版 **1.10.0**）/ .NET 10 / 題材: PC情報取得
> ドキュメント記載は **2026-06-20 時点**の確認に基づく。

`AsBuilder()` でエージェントをパイプライン化し、`Use(...)` で処理を差し込む例です
（Microsoft.Extensions.AI と同じビルダー方式）。

## このサンプルで分かること

- **実行ミドルウェア**（引数 5 個のオーバーロード）で `RunAsync` 全体をはさみ、所要時間などを計測できること
- **関数呼び出しミドルウェア**（引数 4 個のオーバーロード）で**ツール 1 回ごと**の呼び出しをはさんで観測／加工できること
- 複数のミドルウェアが入れ子に合成されること（実行ミドルウェアが外側、関数ミドルウェアが内側で発火）
- 同じ仕組みで組み込みの `UseLogging()` / `UseOpenTelemetry()`（→ Feature08）も差し込めること

## 中核 API

| API | 役割 |
| --- | --- |
| `AIAgent.AsBuilder()` | エージェントを `AIAgentBuilder` に |
| `AIAgentBuilder.Use((messages, session, options, next, ct) => …)` | 実行ミドルウェア |
| `AIAgentBuilder.Use((agent, context, next, ct) => …)` | 関数呼び出しミドルウェア |
| `AIAgentBuilder.Build()` | パイプライン化した `AIAgent` を生成 |

## 動かし方

接続設定は[リポジトリ直下の README](../README.md#設定全サンプル共通) を参照。

```powershell
dotnet run --project Feature04Middleware
```

## 期待される動作（実機確認済み）

```
[run] エージェント実行を開始します
  [tool] GetSystemInfo -> 6 ms
  [tool] GetDriveInfo -> 8 ms
  [tool] GetTopProcesses -> 24 ms
[run] 実行完了 (2673 ms)
Agent > （統合された回答）
```

## 参考（一次情報）

- 公式ドキュメント（ミドルウェア）: <https://learn.microsoft.com/en-us/agent-framework/>
