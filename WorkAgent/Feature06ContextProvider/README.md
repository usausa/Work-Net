# Feature06ContextProvider — コンテキストプロバイダー (動的な文脈注入)

> Microsoft Agent Framework 1.0 GA（本サンプルは安定版 **1.10.0**）/ .NET 10 / 題材: PC情報取得
> ドキュメント記載は **2026-06-20 時点**の確認に基づく。

`AIContextProvider` を使い、実行のたびに「追加の文脈」を**動的に差し込む**例です。

## このサンプルで分かること

- `ProvideAIContextAsync(...)` をオーバーライドして `AIContext` を返すと、その回の入力に**マージ**されること
- `AIContext.Instructions` / `Messages` / `Tools` を通じて、指示・メッセージ・ツールを動的に渡せること
- 例: 現在時刻やマシン名など**毎回変化する／その時点の情報**を、ツール呼び出しなしで自動的に渡せること
- `InvokedAsync` 側をオーバーライドすれば、実行結果から記憶を蓄積する用途にも使えること

## 中核 API

| API | 役割 |
| --- | --- |
| `AIContextProvider`（抽象基底） | 文脈プロバイダーの実装元 |
| `ProvideAIContextAsync(context, ct)` → `AIContext` | 追加文脈を返すオーバーライド点 |
| `AIContext { Instructions, Messages, Tools }` | 注入する文脈 |
| `ChatClientAgentOptions.AIContextProviders` | エージェントへの登録 |

## 動かし方

接続設定は[リポジトリ直下の README](../README.md#設定全サンプル共通) を参照。

```powershell
dotnet run --project Feature06ContextProvider
```

## 期待される動作（実機確認済み・値はマシン依存）

```
You   > 今の時刻と、あなたが把握しているこのPCのマシン名・ログインユーザーを教えて。
Agent > 現在時刻: 2026-06-20 14:53:34 +09:00
        マシン名: <マシン名>
        ログインユーザー: <ユーザー>
```

ツールを呼ばずに回答できているのは、プロバイダーがこれらを文脈として注入しているためです。

## ⚠ セキュリティ上の注意

外部／非信頼ソースの情報を文脈へ注入する場合は、**プロンプトインジェクション**対策（検証・サニタイズ）を行ってください。

## 参考（一次情報）

- 公式ドキュメント（Context providers / Memory）: <https://learn.microsoft.com/en-us/agent-framework/>
