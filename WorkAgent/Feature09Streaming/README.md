# Feature09Streaming — ストリーミング応答 (RunStreamingAsync)

> Microsoft Agent Framework 1.0 GA（本サンプルは安定版 **1.10.0**）/ .NET 10 / 題材: PC情報取得
> ドキュメント記載は **2026-06-20 時点**の確認に基づく。

- **API 区分**: 安定版
- **追加環境**: 不要（既存の Foundry chat 接続のみ）
- **追加パッケージ**: なし

回答を一括ではなく、生成されたそばから**断片で逐次受け取る**例です。

## このサンプルで分かること

- `RunStreamingAsync(...)` が `AgentResponseUpdate` を逐次返すこと（各 `ToString()` がテキスト断片）
- 長い回答でも、最初の断片が届いた時点から表示し始められること
- `update.Contents` にツール呼び出し（`FunctionCallContent`）等も流れてくること

## 中核 API

| API | 役割 |
| --- | --- |
| `AIAgent.RunStreamingAsync(質問)` | 応答断片の非同期ストリーム |
| `AgentResponseUpdate` | 1 断片（テキスト／ツール呼び出し等） |

## 動かし方

接続設定は[リポジトリ直下の README](../README.md#設定全サンプル共通) を参照。

```powershell
dotnet run --project Feature09Streaming
```

## 期待される動作（実機確認済み）

回答が少しずつ書き出され、ツール呼び出し時に `[tool]` マーカーが挿入されます。

## 参考（一次情報）

- 公式ドキュメント: <https://learn.microsoft.com/en-us/agent-framework/>
