# Feature07MultiAgent — マルチエージェント連携 (エージェントのツール化)

> Microsoft Agent Framework 1.0 GA（本サンプルは安定版 **1.10.0**）/ .NET 10 / 題材: PC情報取得
> ドキュメント記載は **2026-06-20 時点**の確認に基づく。

専門エージェントを `AsAIFunction()` でツール化し、別の「調整役」エージェントから呼ばせる例です。

## このサンプルで分かること

- `AIAgent.AsAIFunction(options)` でエージェントを**ツール（`AIFunction`）に変換**できること
- 調整役が、専門エージェント（＝ツール）を必要に応じて呼び、回答を統合できること
- 各専門エージェントは**自分のツールだけ**を持ち、関心を分離できること
- 追加パッケージなしで多段構成を作れること

## 中核 API

| API | 役割 |
| --- | --- |
| `AIAgent.AsAIFunction(AIFunctionFactoryOptions)` | エージェントをツール化（Name/Description が振り分け判断に使われる） |
| `ChatClient.AsAIAgent(instructions, name, tools)` | 調整役にツール化したエージェントを渡す |

## 補足: Workflows との違い

本サンプルは**追加依存なし**でエージェント連携を示します。逐次／並列／分岐などの
**グラフ型ワークフロー**を厳密に組みたい場合は、別パッケージ `Microsoft.Agents.AI.Workflows`
を使います（本リポジトリには未導入。導入版が必要なら追加可能）。

## 動かし方

接続設定は[リポジトリ直下の README](../README.md#設定全サンプル共通) を参照。

```powershell
dotnet run --project Feature07MultiAgent
```

## 期待される動作（実機確認済み）

```
You         > このPCのハードウェア概要(OS・CPU・メモリ)と、ディスクの空き状況・メモリ上位プロセスをまとめて。
Coordinator > （ask_hardware_specialist と ask_storage_specialist に委譲し、結果を統合した回答）
```

## 参考（一次情報）

- 公式ドキュメント: <https://learn.microsoft.com/en-us/agent-framework/>
- 公式サンプル（`07.Workflow`）: <https://github.com/microsoft/Agent-Framework-Samples>
