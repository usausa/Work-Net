# Microsoft Agent Framework GA — 機能別サンプル集 (C# / .NET 10)

Microsoft Agent Framework **1.0 GA**(本サンプル群は安定版 **1.10.0**)の主要機能を、
**機能ごとに1プロジェクト**へ分けて示すサンプル集です。題材はすべて **「PCの情報を取得する」** で統一しています。

- まず動かす最小サンプル … [`BasicAgentSample`](BasicAgentSample/README.md)(接続・ツール・対話の基本)
- 共通基盤 … [`AgentSampleCore`](AgentSampleCore/README.md)(Foundry 接続と PC情報ツールを共有)
- 機能別サンプル … `Feature01`〜`Feature14`(下表。各行から個別 README へ移動できます)
- **機能の全体像 … [📚 機能カタログ & サンプル対応表 (FEATURES.md)](FEATURES.md)**
  — サンプル未実装のものも含む GA 機能の一覧、各機能の「なぜ必要か」「素の ChatClient との違い」「一般的な使い方」

公式サンプル [microsoft/Agent-Framework-Samples](https://github.com/microsoft/Agent-Framework-Samples)
の分類(Tools / Providers / RAG / Workflow / Evaluation & Tracing)を踏まえ、GA の代表機能を選定しています（本サンプル群が扱うのは GA 機能の一部です）。

## 機能別サンプル一覧

各サンプルは独立したコンソールアプリで、**14 本すべて追加環境なし**（既存の Foundry chat 接続のみ）で動きます。

| # | サンプル | 学べること | 主な API |
| --- | --- | --- | --- |
| 01 | [Feature01Tools](Feature01Tools/README.md) | 関数ツール（引数つき・複数ツールの自動選択・呼び出しの観測） | `AsAIAgent`, `AIFunctionFactory.Create` |
| 02 | [Feature02StructuredOutput](Feature02StructuredOutput/README.md) | 構造化出力（回答を型付きで受信／STJ ソース生成併用） | `RunAsync<T>` |
| 03 | [Feature03Sessions](Feature03Sessions/README.md) | セッション永続化（会話状態を JSON 保存し再開） | `Serialize/DeserializeSessionAsync` |
| 04 | [Feature04Middleware](Feature04Middleware/README.md) | ミドルウェア（実行/ツール呼び出しをはさんで計測・加工） | `AsBuilder().Use` |
| 05 | [Feature05ToolApproval](Feature05ToolApproval/README.md) | ツール承認 / HITL（機微なツールに人手承認） | `ApprovalRequiredAIFunction` |
| 06 | [Feature06ContextProvider](Feature06ContextProvider/README.md) | コンテキスト注入（毎ターン動的に文脈を差し込む） | `AIContextProvider` |
| 07 | [Feature07MultiAgent](Feature07MultiAgent/README.md) | マルチエージェント（専門エージェントをツール化して連携） | `AsAIFunction` |
| 08 | [Feature08Telemetry](Feature08Telemetry/README.md) | 可観測性（OpenTelemetry でトレース） | `UseOpenTelemetry` |
| 09 | [Feature09Streaming](Feature09Streaming/README.md) | ストリーミング（応答を断片で逐次受信） | `RunStreamingAsync` |
| 10 | [Feature10Rag](Feature10Rag/README.md) | RAG（外部ナレッジを検索して文脈注入／インメモリ検索） | `TextSearchProvider` |
| 11 🧪 | [Feature11Todo](Feature11Todo/README.md) | TODO 管理（多段作業をエージェントに TODO 管理させる） | `TodoProvider` |
| 12 🧪 | [Feature12FileAccess](Feature12FileAccess/README.md) | ファイルアクセス（保存/読取ツール／インメモリ保存） | `FileAccessProvider` |
| 13 🧪 | [Feature13Compaction](Feature13Compaction/README.md) | 履歴の圧縮（古い会話を間引いて文脈枠を抑える／決定的） | `CompactionProvider` |
| 14 | [Feature14Evaluation](Feature14Evaluation/README.md) | 評価（応答をローカル検査で自動採点） | `LocalEvaluator` |

> 🧪 = GA で実験的指定（`MAAI001`）の API を使用。該当箇所のみ `#pragma warning disable MAAI001` で局所抑制しています（将来変更・削除に注意）。

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

## ⚠ セキュリティ上の注意

- APIキーはコードや `appsettings.json` に書かず、**ユーザーシークレット / 環境変数 / Key Vault / Managed Identity** を使用する
- `Feature06ContextProvider` のように外部情報を文脈へ注入する場合は、**プロンプトインジェクション**対策(検証・サニタイズ)を行う
- `Feature08Telemetry` の `EnableSensitiveData = true` はプロンプト/応答をトレースに含めるため、実運用では取り扱いに注意する

## 参考

機能の網羅一覧・各機能の解説・一次情報リンクは **[FEATURES.md](FEATURES.md)** に集約しています。
