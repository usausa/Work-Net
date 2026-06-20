# Feature12FileAccess — ファイルアクセス (FileAccessProvider)

> Microsoft Agent Framework 1.0 GA（本サンプルは安定版 **1.10.0**）/ .NET 10 / 題材: PC情報取得
> ドキュメント記載は **2026-06-20 時点**の確認に基づく。

- **API 区分**: ⚠ **実験的（Experimental・診断 `MAAI001`）** … 「評価目的のみ・将来変更/削除あり」。
  本サンプルは該当箇所のみ `#pragma warning disable MAAI001` で局所抑制しています。本番採用時は提供状況を要確認。
- **追加環境**: 不要（**保存先はインメモリ**。実ディスクへは書き込まない）
- **追加パッケージ**: なし

エージェントに**ファイルの保存・読み取り**などのツールを与える例です。

## このサンプルで分かること

- `FileAccessProvider`（`AIContextProvider` の一種）が、保存・読み取り・一覧・検索・削除のファイルツールを与えること
- 保存先は `AgentFileStore` で差し替えられること（本サンプルは `InMemoryAgentFileStore`＝実ファイルを作らない）
- 「保存→読み返し」の往復を、同じセッション内でエージェントが行えること

> 実ディスクに保存したい場合は `FileSystemAgentFileStore` に差し替えます（その場合は実ファイルが作られる点に注意）。

## 中核 API

| API | 役割 |
| --- | --- |
| `new InMemoryAgentFileStore()` | インメモリの保存先（**実験的**） |
| `new FileAccessProvider(store, options)` | ファイルツールを与えるプロバイダー（**実験的**） |
| `ChatClientAgentOptions.AIContextProviders` | エージェントへの登録 |

## 動かし方

接続設定は[リポジトリ直下の README](../README.md#設定全サンプル共通) を参照。

```powershell
dotnet run --project Feature12FileAccess
```

## 期待される動作（実機確認済み）

`pc-info.txt` に保存→次のターンでその内容を読み返して表示します（インメモリのため実ファイルは残りません）。

## 参考（一次情報）

- 公式ドキュメント: <https://learn.microsoft.com/en-us/agent-framework/>
