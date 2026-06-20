# PC Info Agent Sample — Microsoft Agent Framework × Microsoft Foundry (C# / .NET 10)

Microsoft Agent Framework **1.0 GA**（本サンプルは安定版 **1.10.0**）を使い、
**Microsoft Foundry** のチャットデプロイメントに接続して **PCの情報を取得する**エージェントのサンプルです。

含まれる要素:

- **Microsoft Foundry**（`services.ai.azure.com`）の chat デプロイメントへ **APIキー認証**で接続
- 関数ツール（`[Description]` を付けた C# メソッド）の登録と LLM による自動呼び出し
- `AgentSession` によるマルチターン対話
- `RunAsync`（一括）と `RunStreamingAsync`（ストリーミング）

## Foundry への2系統の接続方法

| 方法 | クライアント | 認証 | 用途 |
| --- | --- | --- | --- |
| **モデル(chat)へ直接推論**（本サンプル） | `AzureOpenAIClient` + `Microsoft.Agents.AI.OpenAI` | **APIキー** または Entra ID | アカウントendpoint + APIキー + chatデプロイメントが手元にある場合 |
| Foundry Agent Service（サーバー管理） | `AIProjectClient` + `Microsoft.Agents.AI.Foundry` | Entra ID（`DefaultAzureCredential`）+ プロジェクトendpoint | ポータルで管理する versioned エージェントを使う場合 |

今回は「アカウントendpoint + APIキー + chatデプロイメント」なので前者を使用します。

## 登録しているツール（PC情報取得）

| ツール | 取得する情報 |
| --- | --- |
| `GetSystemInfo` | OS・アーキテクチャ・マシン名・ユーザー名・CPUコア数・.NETバージョン・稼働時間 |
| `GetMemoryInfo` | メモリ総量／プロセスのGCヒープ使用量 |
| `GetDriveInfo`  | ドライブごとの空き容量・総容量 |

## 必要なもの

- .NET 10 SDK
- Microsoft Foundry のエンドポイント / APIキー / chatデプロイメント名

## 設定

接続情報は `appsettings.json` と **ユーザーシークレット** / 環境変数から読み込みます
（`appsettings.json` → ユーザーシークレット → 環境変数 の順に上書き）。

`appsettings.json`（エンドポイントとデプロイメント名。APIキーは空のまま）:

```json
{
  "Foundry": {
    "Endpoint": "https://foundry-usausa-resource.services.ai.azure.com",
    "ApiKey": "",
    "ChatDeployment": "gpt-5.4-mini"
  }
}
```

APIキーはソースに置かず、ユーザーシークレットで設定します:

```powershell
dotnet user-secrets set "Foundry:ApiKey" "<your-api-key>"
```

CI などでは環境変数で上書きできます（区切りは `__`）:

```powershell
$env:Foundry__Endpoint       = "https://foundry-usausa-resource.services.ai.azure.com"
$env:Foundry__ApiKey         = "<your-api-key>"
$env:Foundry__ChatDeployment = "gpt-5.4-mini"
```

## 実行手順

```powershell
dotnet run
```

起動するとまず「基本スペックと空きディスク容量」を1回問い合わせ（ツール呼び出しを確認できます）、
続いて対話モードに入ります。`exit` または標準入力を閉じると終了します。
接続情報が未設定の場合はエラーメッセージを表示して終了します。

### 動作確認済みの出力例（抜粋）

```
=== 単発の実行 (Foundry / gpt-5.4-mini) ===
### 基本スペック
- OS: Microsoft Windows 10.0.26200
- 論理プロセッサ数: 24
- .NET ランタイム: .NET 10.0.9
### 空きディスク容量
- C:\: 空き 234.2 GB / 全体 930.4 GB ...
```

## ⚠ セキュリティ上の注意

APIキーは `appsettings.json` に書かず、**ユーザーシークレット**で管理します。実運用では:

- キーはコードや `appsettings.json` に書かず、**ユーザーシークレット / 環境変数 / Azure Key Vault / Managed Identity** を使用する
- `appsettings.json` の `ApiKey` は空にしておき、リポジトリにキーをコミットしない
- 過去にコミット・共有されたキーは**無効化（ローテーション）**する

## 補足: プレビュー版との API 差分

GA 版では preview 時代の一部 API 名が変わっています（古い記事に注意）:

| プレビュー版 | GA 版 (1.x) |
| --- | --- |
| `CreateAIAgent(...)` | `AsAIAgent(...)` |
| `AgentThread` | `AgentSession` |
| `agent.GetNewThread()` | `await agent.CreateSessionAsync()` |
| `AgentRunResponse` / `AgentRunResponseUpdate` | `AgentResponse` / `AgentResponseUpdate` |
