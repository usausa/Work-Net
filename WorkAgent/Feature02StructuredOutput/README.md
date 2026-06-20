# Feature02StructuredOutput — 構造化出力 (Structured Output)

> Microsoft Agent Framework 1.0 GA（本サンプルは安定版 **1.10.0**）/ .NET 10 / 題材: PC情報取得
> ドキュメント記載は **2026-06-20 時点**の確認に基づく。

モデルの回答を「文字列」ではなく「型付きの C# オブジェクト」として受け取る例です。

## このサンプルで分かること

- `RunAsync<T>(...)` を使うと、フレームワークが **T の JSON スキーマを `response_format` としてモデルに渡し**、
  返ってきた JSON を `T` にデシリアライズして返すこと
- 結果は `AgentResponse<T>.Result` から**強く型付け**された値として取り出せること
- ツールで実際の PC 情報を集めさせ、それを指定スキーマへ整形させられること
- **System.Text.Json のソース生成**（`JsonSerializerContext`）を併用すると、リフレクション不要で
  AOT にも適すること（アナライザー CA1812/CA1515 の回避にもなる）

## 中核 API

| API | 役割 |
| --- | --- |
| `AIAgent.RunAsync<T>(質問, serializerOptions)` | 型 `T` を要求して実行 |
| `AgentResponse<T>.Result` | デシリアライズ済みの `T` |
| `[JsonSerializable(typeof(T))] JsonSerializerContext` | STJ ソース生成コンテキスト |

## 動かし方

接続設定は[リポジトリ直下の README](../README.md#設定全サンプル共通) を参照。

```powershell
dotnet run --project Feature02StructuredOutput
```

## 期待される動作（実機確認済み・値はマシン依存）

```
=== 型付きで取得した PC レポート ===
OS              : Microsoft Windows 10.0.x
アーキテクチャ  : X64
マシン名        : <マシン名>
論理プロセッサ数: 24
.NET            : .NET 10.0.x
ドライブ:
  C:\ : 空き … GB / 全体 … GB
  …
```

`SystemReport` / `DriveReport` レコード（`Program.cs` 末尾）がモデル出力の「型」になります。
`[Description]` をプロパティに付けるとスキーマに含まれ、モデルへのヒントになります。

## 参考（一次情報）

- 公式ドキュメント: <https://learn.microsoft.com/en-us/agent-framework/>
- 構造化出力の背景（Microsoft.Extensions.AI）: <https://learn.microsoft.com/en-us/dotnet/ai/>
