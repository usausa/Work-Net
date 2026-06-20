# Feature01Tools — 関数ツール (Function Tools)

> Microsoft Agent Framework 1.0 GA（本サンプルは安定版 **1.10.0**）/ .NET 10 / 題材: PC情報取得
> ドキュメント記載は **2026-06-20 時点**の確認に基づく。

Agent Framework の最も基本的な機能。通常の C# メソッドをそのままツールとしてエージェントに渡し、
モデルに自動で呼ばせる例です。

## このサンプルで分かること

- `[Description]` を付けた C# メソッドが、そのままツールになること
- 複数のツールを登録すると、**モデルが質問に応じて必要なものだけを選ぶ**こと
- **引数つきツール** `GetTopProcesses(int topCount)` では、引数もモデルが文脈から決めて渡すこと
- ツールの呼び出しは応答メッセージ列（`FunctionCallContent`）に残るため、**後から観測**できること

## 中核 API

| API | 役割 |
| --- | --- |
| `ChatClient.AsAIAgent(instructions, name, tools)` | チャットクライアントからエージェントを生成 |
| `AIFunctionFactory.Create(メソッド)` | C# メソッドをツール（`AIFunction`）に変換 |
| `AIAgent.RunAsync(質問)` | 1 回実行して `AgentResponse` を取得 |
| `AgentResponse.Messages` → `FunctionCallContent` | 呼ばれたツール名・引数の観測 |

## 動かし方

接続設定は[リポジトリ直下の README](../README.md#設定全サンプル共通) を参照（APIキーはユーザーシークレット／環境変数で設定。コードには書きません）。

```powershell
dotnet run --project Feature01Tools
```

## 期待される動作（実機確認済み・値はマシン依存）

```
You   > メモリ使用量が多い上位3つのプロセスと、空きディスク容量を教えて。
  [tool] GetTopProcesses(topCount=3)
  [tool] GetDriveInfo((引数なし))
Agent > メモリ使用量が多い上位3つのプロセスは… / 空きディスク容量は…

You   > このPCのOSと .NET のバージョンは?
  [tool] GetSystemInfo((引数なし))
Agent > OS は Microsoft Windows …、.NET のバージョンは .NET 10.0.x です。
```

## 参考（一次情報）

- 公式ドキュメント: <https://learn.microsoft.com/en-us/agent-framework/>
- 公式サンプル（`04.Tools`）: <https://github.com/microsoft/Agent-Framework-Samples>
