# AgentSampleCore — 機能別サンプル共通ライブラリ

> Microsoft Agent Framework 1.0 GA（本サンプル群は安定版 **1.10.0**）/ .NET 10
> ドキュメント記載は **2026-06-20 時点**の確認に基づく。

`Feature01`〜`Feature08` の各サンプルが共有する**ライブラリ**です（単体では実行しません）。
各サンプルが「その機能の説明」に集中できるよう、定型のボイラープレートをここへ集約しています。

## 提供するもの

| 型 / ファイル | 役割 |
| --- | --- |
| `FoundryOptions` | Foundry 接続設定（Endpoint / ApiKey / ChatDeployment）のバインド先 |
| `SampleHost.TryGetFoundryOptions()` | `appsettings.json` → ユーザーシークレット → 環境変数 の順で設定を読み込む |
| `SampleHost.CreateChatClient(options)` | chat デプロイメントへ接続する `ChatClient` を生成 |
| `PcTools` | PC情報取得ツール（`GetSystemInfo` / `GetMemoryInfo` / `GetDriveInfo` / `GetTopProcesses`） |
| `GlobalSuppressions.cs` | コンソールサンプル共通の警告抑制（各サンプルへリンク参照） |
| `appsettings.json` | 共通の接続設定テンプレート（各サンプルへリンクして出力にコピー） |

各サンプルの `.csproj` はこのプロジェクトを `ProjectReference` し、`GlobalSuppressions.cs` と
`appsettings.json` を `Link` で取り込みます。

## 設定（全サンプル共通）

接続情報の設定方法は[リポジトリ直下の README](../README.md#設定全サンプル共通) を参照してください。
全サンプルは同じ `UserSecretsId`（`agent-sample`）を共有するため、**APIキーの設定は一度で済みます**。
キーはコードや `appsettings.json` に直書きせず、ユーザーシークレット／環境変数から読み込みます。

## 参考（一次情報）

- 公式ドキュメント: <https://learn.microsoft.com/en-us/agent-framework/>
- パッケージ: `Microsoft.Agents.AI`, `Microsoft.Agents.AI.OpenAI`（NuGet, 1.10.0）
