# Feature05ToolApproval — ツールの承認 (Human-in-the-Loop)

> Microsoft Agent Framework 1.0 GA（本サンプルは安定版 **1.10.0**）/ .NET 10 / 題材: PC情報取得
> ドキュメント記載は **2026-06-20 時点**の確認に基づく。

破壊的・機微なツールを、**実行前に人間が承認**してから動かす例です。

## このサンプルで分かること

- `AIFunction` を `ApprovalRequiredAIFunction` でラップすると「承認が必要なツール」になること
- モデルがそのツールを呼ぼうとすると**実行されず**、応答に `ToolApprovalRequestContent` が入って一旦止まること
- 人間が可否を判断し、`request.CreateResponse(approved)` で応答を作って**同じセッションで再実行**すると、
  承認時はツールが実行され、却下時はスキップされること
- 広いポリシーで一括制御したい場合は `AIAgentBuilder.UseToolApproval(...)` も使えること

## 中核 API

| API | 役割 |
| --- | --- |
| `new ApprovalRequiredAIFunction(AIFunction)` | ツールを「要承認」にラップ |
| `AgentResponse.Messages` → `ToolApprovalRequestContent` | 承認要求の検出 |
| `ToolApprovalRequestContent.CreateResponse(approved, reason)` | 承認／却下の応答を作成 |
| `AIAgent.RunAsync(承認応答メッセージ, session)` | 承認を渡して継続 |

## 動かし方

接続設定は[リポジトリ直下の README](../README.md#設定全サンプル共通) を参照。

```powershell
dotnet run --project Feature05ToolApproval
```

> 安全のため、機微ツール `CleanTemporaryFiles` は**実際には削除しません**（対象の集計のみ）。

## 期待される動作（実機確認済み）

```
You   > 一時ファイルを削除して空き容量を増やして。
  [承認要求] ツール 'CleanTemporaryFiles' の実行許可を求めています。 -> 承認(approved: true)
Agent > 一時フォルダの不要ファイル … 個、約 … MB を削除しました。
        ※このサンプルでは安全のため実際の削除は行われていません。
```

## 参考（一次情報）

- 公式ドキュメント（Human-in-the-loop）: <https://learn.microsoft.com/en-us/agent-framework/>
- 承認コンテンツ型は `Microsoft.Extensions.AI`（MEAI）由来: <https://learn.microsoft.com/en-us/dotnet/ai/>
