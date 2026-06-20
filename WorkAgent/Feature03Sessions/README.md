# Feature03Sessions — セッションの永続化 (会話状態の保存と復元)

> Microsoft Agent Framework 1.0 GA（本サンプルは安定版 **1.10.0**）/ .NET 10 / 題材: PC情報取得
> ドキュメント記載は **2026-06-20 時点**の確認に基づく。

`AgentSession` が保持する会話履歴などの状態を JSON にして保存し、**プロセスを終了しても続きから再開**する例です。

## このサンプルで分かること

- `AgentSession` が会話の状態（履歴）を保持すること
- `SerializeSessionAsync(session)` で状態を `JsonElement` にして保存できること
- `DeserializeSessionAsync(json)` で状態を復元し、**前の会話を覚えたまま継続**できること
- マルチターンの文脈（前ターンの回答を踏まえた追質問）が引き継がれること

## 中核 API

| API | 役割 |
| --- | --- |
| `AIAgent.CreateSessionAsync()` | 新しいセッションを作成 |
| `AIAgent.RunAsync(質問, session)` | セッションを使って実行（履歴が更新される） |
| `AIAgent.SerializeSessionAsync(session)` | セッション状態を `JsonElement` 化 |
| `AIAgent.DeserializeSessionAsync(jsonElement)` | `JsonElement` からセッションを復元 |

## 動かし方

接続設定は[リポジトリ直下の README](../README.md#設定全サンプル共通) を参照。
**2 回実行**する想定です。

```powershell
dotnet run --project Feature03Sessions   # 1回目: 会話して保存
dotnet run --project Feature03Sessions   # 2回目: 復元して記憶を確認
```

セッション状態は出力フォルダの `pc-agent-session.json` に保存され、2 回目の最後に自動削除されます。

## 期待される動作（実機確認済み）

```
=== 1回目: 新しいセッションで会話します ===
You   > このPCのOS名だけ教えて。
Agent > Microsoft Windows 10.0.x
You   > それは何ビットのアーキテクチャ?
Agent > 64ビット（X64）です。
セッションを保存しました: …\pc-agent-session.json

=== 2回目: 保存済みセッションを復元して継続します ===
You   > さっき教えてくれたOS名をもう一度言って。
Agent > Microsoft Windows 10.0.x     ← 復元した履歴から想起
```

## 参考（一次情報）

- 公式ドキュメント: <https://learn.microsoft.com/en-us/agent-framework/>
