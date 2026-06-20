# Feature11Todo — TODO 管理 (TodoProvider)

> Microsoft Agent Framework 1.0 GA（本サンプルは安定版 **1.10.0**）/ .NET 10 / 題材: PC情報取得
> ドキュメント記載は **2026-06-20 時点**の確認に基づく。

- **API 区分**: ⚠ **実験的（Experimental・診断 `MAAI001`）** … 「評価目的のみ・将来変更/削除あり」。
  本サンプルは該当箇所のみ `#pragma warning disable MAAI001` で局所抑制しています。本番採用時は提供状況を要確認。
- **追加環境**: 不要（既存の Foundry chat 接続のみ）
- **追加パッケージ**: なし

複数ステップの作業を、エージェント自身に **TODO で管理**させながら進める例です。

## このサンプルで分かること

- `TodoProvider`（`AIContextProvider` の一種）を登録するだけで、エージェントに TODO 管理ツールと指示が与えられること
- エージェントが点検項目を TODO 化し、各項目の完了を自分で更新すること
- 実行後に `GetAllTodosAsync(session)` で TODO の最終状態を取得できること

## 中核 API

| API | 役割 |
| --- | --- |
| `new TodoProvider(TodoProviderOptions)` | TODO 管理プロバイダー（**実験的**） |
| `TodoProvider.GetAllTodosAsync(session)` | TODO 一覧の取得 |
| `TodoItem { Title, IsComplete, … }` | TODO 1 件 |

## 動かし方

接続設定は[リポジトリ直下の README](../README.md#設定全サンプル共通) を参照。

```powershell
dotnet run --project Feature11Todo
```

## 期待される動作（実機確認済み）

```
=== エージェントが管理した TODO ===
  [x] OS情報の確認
  [x] メモリ情報の確認
  [x] ディスク空き容量の確認
  [x] メモリ上位プロセスの確認
```

## 参考（一次情報）

- 公式ドキュメント: <https://learn.microsoft.com/en-us/agent-framework/>
