# Feature10Rag — 検索拡張生成 (RAG / TextSearchProvider)

> Microsoft Agent Framework 1.0 GA（本サンプルは安定版 **1.10.0**）/ .NET 10 / 題材: PC情報取得
> ドキュメント記載は **2026-06-20 時点**の確認に基づく。

- **API 区分**: 安定版
- **追加環境**: 不要（**検索はインメモリ配列**。外部 DB・検索サービスは使わない）
- **追加パッケージ**: なし

外部ナレッジを検索して文脈に注入し、その根拠込みで回答させる RAG の例です。

## このサンプルで分かること

- `TextSearchProvider` に**検索処理**（`Func<query, ct, Task<IEnumerable<TextSearchResult>>>`）を渡すと、
  毎回の実行前に関連情報を検索し、結果を文脈へ自動注入できること
- モデルが「PC の実値（ツール）」＋「ナレッジ（検索結果）」の両方を根拠に回答できること
- 本サンプルは**インメモリ配列**を検索するだけで RAG を完結できること（外部環境不要）

> 実運用ではこの検索処理を、ベクタ DB や検索サービスへの問い合わせに置き換えます。
> 本サンプルの検索は語句一致の簡易実装で、意味検索ではありません。

## 中核 API

| API | 役割 |
| --- | --- |
| `new TextSearchProvider(検索デリゲート, options)` | 検索→文脈注入のプロバイダー |
| `TextSearchProvider.TextSearchResult { SourceName, Text }` | 検索結果 1 件 |
| `ChatClientAgentOptions.AIContextProviders` | エージェントへの登録 |

## 動かし方

接続設定は[リポジトリ直下の README](../README.md#設定全サンプル共通) を参照。

```powershell
dotnet run --project Feature10Rag
```

## 期待される動作（実機確認済み）

「空きディスクとメモリは社内基準に照らして問題ないか」を、注入された社内基準（ディスク空き容量ポリシー／推奨メモリ）を根拠に判定して回答します。

## ⚠ セキュリティ上の注意

外部／非信頼ソースを検索結果として注入する場合は、**プロンプトインジェクション**対策（検証・サニタイズ）を行ってください。

## 参考（一次情報）

- 公式ドキュメント / 公式サンプル（`06.RAGs`）: <https://learn.microsoft.com/en-us/agent-framework/> / <https://github.com/microsoft/Agent-Framework-Samples>
