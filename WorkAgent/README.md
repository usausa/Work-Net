# Microsoft Agent Framework GA — 機能別サンプル集 (C# / .NET 10)

Microsoft Agent Framework **1.0 GA**(本サンプル群は安定版 **1.10.0**)の主要機能を、
**機能ごとに1プロジェクト**へ分けて示すサンプル集です。題材はすべて **「PCの情報を取得する」** で統一しています。

- まず動かす最小サンプル … [`BasicAgentSample`](BasicAgentSample/README.md)(接続・ツール・対話の基本)
- 共通基盤 … [`AgentSampleCore`](AgentSampleCore/README.md)(Foundry 接続と PC情報ツールを共有)
- 機能別サンプル … `Feature01`〜`Feature14`(下表。各行から個別 README へ移動できます)
- **機能の全体像 … [📚 機能カタログ & サンプル対応表 (FEATURES.md)](FEATURES.md)**
  — サンプル未実装のものも含む GA 機能の一覧、各機能の「なぜ必要か」「素の ChatClient との違い」「一般的な使い方」

公式サンプル [microsoft/Agent-Framework-Samples](https://github.com/microsoft/Agent-Framework-Samples)
の分類(Tools / Providers / RAG / Workflow / Evaluation & Tracing)を踏まえ、GA の代表機能を選定しています。

> 本サンプル群が扱うのは GA の**主要機能の一部**です。GA の全機能の一覧は [FEATURES.md](FEATURES.md) を参照してください。

### 凡例（区分・追加環境）

- **区分**: 🟢 安定版 / 🧪 実験的(`MAAI001`。「評価目的のみ・将来変更/削除あり」。該当箇所のみ `#pragma` で局所抑制)
- **追加環境**: ✅ 不要（既存の Foundry chat 接続のみ。外部 DB・別サービス・実ディスク書き込みは無し）
  — 本サンプル群は **14 本すべて「追加環境 ✅ 不要」** です。

## 機能別サンプル一覧

| # | プロジェクト | 機能 | 区分 | 追加環境 | 使う主な API |
| --- | --- | --- | :---: | :---: | --- |
| 01 | [Feature01Tools](Feature01Tools/README.md) | **関数ツール** … 引数つきツール・複数ツールの自動選択・呼び出しの観測 | 🟢 | ✅ | `AsAIAgent`, `AIFunctionFactory.Create`, `FunctionCallContent` |
| 02 | [Feature02StructuredOutput](Feature02StructuredOutput/README.md) | **構造化出力** … 回答を型付きレコードで受け取る(STJ ソース生成併用) | 🟢 | ✅ | `RunAsync<T>`, `AgentResponse<T>.Result` |
| 03 | [Feature03Sessions](Feature03Sessions/README.md) | **セッション永続化** … 会話状態を JSON 保存し、再起動後に再開 | 🟢 | ✅ | `SerializeSessionAsync`, `DeserializeSessionAsync` |
| 04 | [Feature04Middleware](Feature04Middleware/README.md) | **ミドルウェア** … 実行/ツール呼び出しをはさんで計測・加工 | 🟢 | ✅ | `AsBuilder`, `AIAgentBuilder.Use`, `Build` |
| 05 | [Feature05ToolApproval](Feature05ToolApproval/README.md) | **ツール承認(HITL)** … 機微なツールに人間の承認を要求 | 🟢 | ✅ | `ApprovalRequiredAIFunction`, `ToolApprovalRequestContent` |
| 06 | [Feature06ContextProvider](Feature06ContextProvider/README.md) | **コンテキスト注入** … 毎ターン動的に文脈(指示等)を差し込む | 🟢 | ✅ | `AIContextProvider`, `AIContext`, `AIContextProviders` |
| 07 | [Feature07MultiAgent](Feature07MultiAgent/README.md) | **マルチエージェント** … 専門エージェントをツール化して連携 | 🟢 | ✅ | `AsAIFunction`, `AIFunctionFactoryOptions` |
| 08 | [Feature08Telemetry](Feature08Telemetry/README.md) | **可観測性** … 実行を OpenTelemetry でトレース | 🟢 | ✅ | `UseOpenTelemetry`, `ActivityListener` |
| 09 | [Feature09Streaming](Feature09Streaming/README.md) | **ストリーミング** … 応答を断片で逐次受け取る | 🟢 | ✅ | `RunStreamingAsync`, `AgentResponseUpdate` |
| 10 | [Feature10Rag](Feature10Rag/README.md) | **RAG** … 外部ナレッジを検索して文脈注入(インメモリ検索) | 🟢 | ✅ | `TextSearchProvider`, `AIContextProviders` |
| 11 | [Feature11Todo](Feature11Todo/README.md) | **TODO管理** … 多段作業をエージェントに TODO 管理させる | 🧪 | ✅ | `TodoProvider`, `GetAllTodosAsync` |
| 12 | [Feature12FileAccess](Feature12FileAccess/README.md) | **ファイルアクセス** … 保存/読取ツール(インメモリ保存) | 🧪 | ✅ | `FileAccessProvider`, `InMemoryAgentFileStore` |
| 13 | [Feature13Compaction](Feature13Compaction/README.md) | **履歴の圧縮** … 古い会話を間引いて文脈枠を抑える(決定的) | 🧪 | ✅ | `CompactionProvider`, `SlidingWindowCompactionStrategy` |
| 14 | [Feature14Evaluation](Feature14Evaluation/README.md) | **評価** … 応答をローカル検査で自動採点(追加API呼び出しなし) | 🟢 | ✅ | `LocalEvaluator`, `EvalChecks`, `EvaluateAsync` |

> 🧪 の 3 本（11/12/13）は GA パッケージで実験的指定のため、該当箇所のみ `#pragma warning disable MAAI001` で
> 局所的に診断を抑制しています（将来の変更・削除に注意）。詳細は各 README を参照。

## 必要なもの

- .NET 10 SDK
- Microsoft Foundry のエンドポイント / APIキー / chat デプロイメント名

## 設定(全サンプル共通)

接続情報は各プロジェクトの `appsettings.json`(`AgentSampleCore` のファイルをリンク共有)と
**ユーザーシークレット** / 環境変数から、`appsettings.json` → ユーザーシークレット → 環境変数 の順で読み込みます。

全サンプルは同じ `UserSecretsId`(`agent-sample`)を共有しているため、**APIキーの設定は一度で済みます**:

```powershell
dotnet user-secrets --project AgentSampleCore set "Foundry:ApiKey" "<your-api-key>"
```

エンドポイントとデプロイメント名は `AgentSampleCore/appsettings.json` に設定します
(APIキーは空のままにし、コミットしないでください):

```json
{
  "Foundry": {
    "Endpoint": "https://<your-resource>.services.ai.azure.com",
    "ApiKey": "",
    "ChatDeployment": "gpt-5.4-mini"
  }
}
```

CI などでは環境変数で上書きできます(区切りは `__`):

```powershell
$env:Foundry__Endpoint       = "https://<your-resource>.services.ai.azure.com"
$env:Foundry__ApiKey         = "<your-api-key>"
$env:Foundry__ChatDeployment = "gpt-5.4-mini"
```

## 実行

各サンプルは独立したコンソールアプリです。プロジェクトを指定して実行します:

```powershell
dotnet run --project Feature01Tools
dotnet run --project Feature02StructuredOutput
# ... 03〜14 も同様
```

接続情報が未設定の場合は、案内メッセージを表示して終了します。

> `Feature03Sessions` は **2回実行** する想定です。1回目で会話を保存し、2回目で復元して続きから再開します。

## 未収録の主な機能（本サンプル群の対象外）

GA の機能は本 14 サンプルでも尽きていません。以下は代表的な「未収録」機能です（**2026-06-20 時点**で、
本リポジトリ参照の `Microsoft.Agents.AI` 1.10.0 の公開型、および公式情報で確認）。
要望があれば追加サンプル化できます。

### A. 同梱パッケージ（`Microsoft.Agents.AI` 1.10.0）に含まれる（追加依存なしで作れる）

| 機能 | 概要 | 主な型 | 区分 |
| --- | --- | --- | :---: |
| ロギング | 実行を構造化ログへ（Feature04 で言及のみ） | `UseLogging`, `LoggingAgent` | 🟢 |
| チャット履歴プロバイダー | 履歴の保存先・検索の差し替え | `InMemoryChatHistoryProvider`, `ChatHistoryMemoryProvider`, `FileMemoryProvider` | 一部 🧪 |
| 要約による履歴圧縮 | Feature13 は決定的圧縮。LLM 要約版は未収録 | `SummarizationCompactionStrategy` | 🧪 |
| スキル | スキルファイル/スクリプトの読み込み | `AgentSkill`, `AgentFileSkill`, `AgentSkillsProvider`, `AgentFileStore` | 🧪 |
| バックグラウンド実行 | 長時間タスクの非同期実行・継続トークン | `BackgroundAgentsProvider`, `AgentRunOptions.AllowBackgroundResponses`, `ContinuationToken` | 🧪 |
| 秘匿情報のマスキング | メッセージ中の機微情報を置換 | `ReplacingRedactor` | 🟢 |
| OpenAI Responses API | Chat Completions ではなく Responses API を使用 | `ResponsesClient.AsAIAgent(...)` | 🟢 |
| DI 連携 | エージェントを DI コンテナに登録 | `AIAgentBuilder` + `IServiceProvider` | 🟢 |

> 🧪 は実験的指定（`MAAI001`）。採用には `#pragma`/`NoWarn` での診断抑制が必要で、将来変更・削除の可能性があります。
> （**収録済み**: ストリーミング→09 / RAG→10 / TODO→11 / ファイルアクセス→12 / 履歴圧縮(決定的)→13 / 評価→14）

### B. 別パッケージ／別サービスが必要（＝**追加環境が必要**）

| 機能 | 概要 | 必要なもの |
| --- | --- | --- |
| ワークフロー | 逐次/並列/分岐のグラフ型オーケストレーション | `Microsoft.Agents.AI.Workflows`（未導入） |
| MCP | 外部 MCP ツールサーバー連携 | `ModelContextProtocol` 系パッケージ |
| A2A | エージェント間（異プロセス/異ベンダー）連携 | A2A プロトコル対応 |
| ホスト型ツール | Code Interpreter / File Search / Web 検索 / Vision など | Foundry Agent Service（`AIProjectClient` + Entra ID） |
| サーバー管理エージェント | ポータルで管理する versioned エージェント | Foundry Agent Service |
| 他プロバイダー | Azure OpenAI / OpenAI / Anthropic Claude / Amazon Bedrock / Google Gemini / Ollama / Foundry Local | 各 SDK の `AsAIAgent` 入口 |

> 注: B 群の API 形は本リポジトリで実行検証していないため、導入時は公式ドキュメント/サンプルで最新仕様を確認してください。

## 補足: GA 版での API 名の変更

GA 版では preview 時代の一部 API 名が変わっています(古い記事に注意):

| プレビュー版 | GA 版 (1.x) |
| --- | --- |
| `CreateAIAgent(...)` | `AsAIAgent(...)` |
| `AgentThread` | `AgentSession` |
| `agent.GetNewThread()` | `await agent.CreateSessionAsync()` |
| `AgentRunResponse` / `AgentRunResponseUpdate` | `AgentResponse` / `AgentResponseUpdate` |

## ⚠ セキュリティ上の注意

- APIキーはコードや `appsettings.json` に書かず、**ユーザーシークレット / 環境変数 / Key Vault / Managed Identity** を使用する
- `Feature06ContextProvider` のように外部情報を文脈へ注入する場合は、**プロンプトインジェクション**対策(検証・サニタイズ)を行う
- `Feature08Telemetry` の `EnableSensitiveData = true` はプロンプト/応答をトレースに含めるため、実運用では取り扱いに注意する

## 参考（一次情報・2026-06-20 確認）

- 公式ドキュメント: <https://learn.microsoft.com/en-us/agent-framework/>
- フレームワーク本体（GitHub）: <https://github.com/microsoft/agent-framework>
- 公式サンプル（GitHub）: <https://github.com/microsoft/Agent-Framework-Samples>
- GA リリース（v1.0, 2026-04）: <https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/>
- NuGet: `Microsoft.Agents.AI`, `Microsoft.Agents.AI.OpenAI`（本リポジトリは 1.10.0）

> 提供状況・API 仕様は変わり得ます。上記は **2026-06-20 時点**の確認に基づきます。
