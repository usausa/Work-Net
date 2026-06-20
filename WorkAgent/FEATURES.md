# Microsoft Agent Framework GA — 機能カタログ & サンプル対応表

> 対象: Microsoft Agent Framework **1.0 GA**（本リポジトリ参照は安定版 **1.10.0**）/ .NET 10
> 作成日: **2026-06-20**（提供状況・API 仕様は変わり得ます。本書は同日時点の確認に基づきます）
> 確認方法: 参照中の NuGet `Microsoft.Agents.AI` / `.Abstractions` / `.OpenAI` 1.10.0 の公開型（同梱 XML）と
> [公式ドキュメント](https://learn.microsoft.com/en-us/agent-framework/)等の一次情報を突き合わせて記載。

サンプル実装の有無に関わらず GA の主な機能を一覧化したカタログです。
[① 対応表](#-1-機能とサンプル対応表)で全体像を掴み、[② 詳細](#-2-各機能の詳細)で「なぜ必要か／素の ChatClient に何を足すか／使い方」を確認できます。

## 凡例

| 記号 | 意味 |
| :--: | --- |
| 🟢 / 🧪 | **区分**: 安定版 / 実験的（診断 `MAAI001`「評価目的のみ・将来変更/削除あり」。利用に診断抑制が必要） |
| ✅ / 📦 / ☁ | **追加依存**: 不要（本リポジトリの構成だけで動く） / 追加 NuGet が必要 / 追加サービス・インフラが必要 |

> ⚠ 📦・☁ の機能は本リポジトリで**実行検証していません**。型名・概要は一次情報に基づきますが、API 形は変わり得るため、採用時は公式の最新仕様をご確認ください。

---

## 📋 1. 機能とサンプル対応表

| カテゴリ | 機能 | サンプル | 区分 | 依存 |
| --- | --- | --- | :--: | :--: |
| 🚀 基礎・実行 | エージェント生成 | [Basic](BasicAgentSample/README.md) / 全Feature | 🟢 | ✅ |
| | 単発実行 (RunAsync) | [01](Feature01Tools/README.md) | 🟢 | ✅ |
| | ストリーミング | [09](Feature09Streaming/README.md) | 🟢 | ✅ |
| | 構造化出力 | [02](Feature02StructuredOutput/README.md) | 🟢 | ✅ |
| | セッション（会話状態） | [03](Feature03Sessions/README.md) | 🟢 | ✅ |
| 🔧 ツール | 関数ツール | [01](Feature01Tools/README.md) | 🟢 | ✅ |
| | ツール承認 (HITL) | [05](Feature05ToolApproval/README.md) | 🟢 | ✅ |
| | ホスト型ツール | — | 🟢 | ☁ |
| | MCP ツール | — | 🟢 | 📦 |
| 🧠 コンテキスト・メモリ | コンテキストプロバイダー | [06](Feature06ContextProvider/README.md) | 🟢 | ✅ |
| | RAG / テキスト検索 | [10](Feature10Rag/README.md) | 🟢 | ✅ |
| | チャット履歴プロバイダー | — | 一部🧪 | ✅ |
| | ベクトル記憶 | — | 🟢 | ☁ |
| | ファイル記憶 | — | 🧪 | ✅ |
| | 履歴の圧縮 (Compaction) | [13](Feature13Compaction/README.md) | 🧪 | ✅ |
| | TODO 管理 | [11](Feature11Todo/README.md) | 🧪 | ✅ |
| | ファイルアクセス | [12](Feature12FileAccess/README.md) | 🧪 | ✅ |
| | スキル | — | 🧪 | ✅ |
| | エージェントモード | — | 🧪 | ✅ |
| | 秘匿情報マスキング | — | 🟢 | ✅ |
| ⚙️ ミドルウェア | ミドルウェア | [04](Feature04Middleware/README.md) | 🟢 | ✅ |
| | ロギング | [04](Feature04Middleware/README.md)で言及 | 🟢 | ✅ |
| | カスタムエージェント（委譲） | — | 🟢 | ✅ |
| | メッセージ注入 | — | 🟢 | ✅ |
| 🤝 マルチエージェント | エージェントのツール化 | [07](Feature07MultiAgent/README.md) | 🟢 | ✅ |
| | ワークフロー | — | 🟢 | 📦 |
| | バックグラウンドエージェント | — | 🧪 | ✅ |
| 📊 可観測性・評価 | テレメトリ (OpenTelemetry) | [08](Feature08Telemetry/README.md) | 🟢 | ✅ |
| | 評価（ローカル検査） | [14](Feature14Evaluation/README.md) | 🟢 | ✅ |
| | 評価（LLM as judge） | — | 🟢 | 📦 |
| 🔌 相互運用・プロバイダー | OpenAI Responses API | — | 🟢 | ✅ |
| | OpenAI 相互運用 | — | 🟢 | ✅ |
| | A2A（エージェント間連携） | — | 🟢 | 📦 |
| | マルチプロバイダー | — | 🟢 | 📦 |
| | サーバー管理エージェント | — | 🟢 | ☁ |
| | DI 連携 | — | 🟢 | ✅ |

---

## 🧩 2. 各機能の詳細

> **前提 — 素の ChatClient とは**: `ChatClient` / `IChatClient`（Microsoft.Extensions.AI）は「メッセージ列＋オプションを渡すと 1 回分の補完を返す」だけの**ステートレスな部品**です。
> 履歴・ツール実行ループ・文脈・横断処理・エージェント連携は持ちません。各機能は、この素の ChatClient に**何を足してエージェントにするか**を示します。

### 🚀 A. 基礎・実行

| 機能 | なぜ必要か | 素の ChatClient に足すもの | 使い方の例 | 主な API |
| --- | --- | --- | --- | --- |
| **エージェント生成** | 役割・ツール・設定の毎回手組みは煩雑で不整合の元 | 役割＋ツール＋既定設定を束ねた再利用可能な存在にする | 役割ごと（サポート/コード担当）に 1 体作り再利用 | `AsAIAgent`, `ChatClientAgent` |
| **単発実行** | 答えだけでなく「途中で何が起きたか」も知りたい | ツール実行ループ後の最終結果を観測可能な `AgentResponse` で返す | 分類・要約・抽出のバッチ / API 処理 | `RunAsync`, `AgentResponse` |
| **ストリーミング** | 全文生成を待つと体感が悪い | セッション/文脈/ミドルウェアを通した断片を逐次返す | チャット UI の逐次（タイプライター）表示 | `RunStreamingAsync` |
| **構造化出力** | 自由文は後続処理で扱いにくい | スキーマ生成・指定・型へのデシリアライズを自動化 | 問い合わせ文から `{種別, 優先度, 要約}` を抽出しチケット化 | `RunAsync<T>` |
| **セッション** | マルチターンの履歴管理・永続化が必須 | 履歴の保持と JSON 永続化/復元を標準化 | 切断後や別サーバーで会話を再開 | `AgentSession`, `Serialize/DeserializeSessionAsync` |

### 🔧 B. ツール（関数呼び出し）

| 機能 | なぜ必要か | 素の ChatClient に足すもの | 使い方の例 | 主な API |
| --- | --- | --- | --- | --- |
| **関数ツール** | モデル単体は外部取得や副作用ができない | ツール実行ループ（検出→実行→差戻し→再呼出）を内蔵 | 天気照会・DB 検索・チケット作成 | `AIFunctionFactory.Create` |
| **ツール承認 (HITL)** | 破壊的操作をモデル判断だけで走らせるのは危険 | 実行ループに承認の中断点を差し込み往復を標準化 | 削除・送金・本番デプロイ前に人手確認 | `ApprovalRequiredAIFunction` |
| **ホスト型ツール** ☁ | コード実行/文書検索/最新情報をローカル実装なしで使いたい | サービス側の実行環境（Code Interpreter 等）を接続 | サンドボックス計算、文書検索、Web 検索、画像理解 | Foundry Agent Service |
| **MCP ツール** 📦 | ツールを標準プロトコルで共有・再利用したい | 外部 MCP サーバーのツールを標準形で接続 | GitHub・FS・社内 API を MCP 経由で利用 | `ModelContextProtocol` |

### 🧠 C. コンテキスト・メモリ

> 多くは `AIContextProvider`（実行のたびに指示・メッセージ・ツールを動的注入できる拡張点）の実装。素の ChatClient では「毎回どの文脈を渡すか」を呼び出し側が手組みする部分を、**自動化・標準化**します。

| 機能 | なぜ必要か | 素の ChatClient に足すもの | 使い方の例 | 主な API |
| --- | --- | --- | --- | --- |
| **コンテキストプロバイダー** | 毎回変わる情報を手で渡し続けるのは煩雑で漏れやすい | 実行直前に文脈（指示/メッセージ/ツール）を自動合成するフック | 時刻・ロケール・権限の自動付与 | `AIContextProvider`, `AIContext` |
| **RAG / テキスト検索** | モデルは学習時点まで・社内情報を知らない | 検索→文脈注入（出典付き）をプロバイダーとして標準化 | 社内規程を検索し根拠付きで回答 | `TextSearchProvider` |
| **チャット履歴プロバイダー** | 履歴を独自ストアに置く/縮約して渡したい | 履歴の保管と前処理（縮約）を差替え可能な層に分離 | Redis 永続化、長履歴の自動縮約 | `ChatHistoryProvider` |
| **ベクトル記憶** ☁ | 全履歴を毎回渡すのは不可能（コスト/長さ） | 関連する過去だけを意味検索で想起し注入 | 以前述べた好みを後日想起 | `ChatHistoryMemoryProvider` |
| **ファイル記憶** 🧪 | 外部ストアなしで軽量な長期記憶がほしい | ファイルを記憶領域として読み書きするツール群 | 覚書を書き留め後で参照 | `FileMemoryProvider` |
| **履歴の圧縮** 🧪 | 会話が伸びると文脈超過・コスト増・遅延を招く | 圧縮（間引き/要約）を実行前段として自動適用 | 長時間対話の文脈管理 | `CompactionProvider` |
| **TODO 管理** 🧪 | 多段タスクは計画/進捗がないと抜け・迷走する | セッションに TODO を持たせ管理ツールを与える | 調査タスクを分解し順に完了 | `TodoProvider` |
| **ファイルアクセス** 🧪 | 中間生成物の読み書き作業領域がほしい | ファイル操作ツールを安全な抽象（FileStore）越しに提供 | レポート/コードを保存し読み返す | `FileAccessProvider` |
| **スキル** 🧪 | 再利用手順を都度プロンプトに書きたくない | スキル（手順/スクリプト）を必要時に参照/実行 | 「リリースノートの作り方」をスキル化 | `AgentSkillsProvider` |
| **エージェントモード** 🧪 | 「計画→実行」の段階制御を明示状態にしたい | モード状態とそれを操作するツールを付与 | 計画モードで設計→実行モードで実装 | `AgentModeProvider` |
| **秘匿情報マスキング** | PII/キーがログ・文脈に残ると漏えい・違反 | 観測/文脈の経路で機微情報を置換 | トレースや検索文脈から PII を除去 | `ReplacingRedactor` |

> 💡 **圧縮ストラテジの選択**: コスト/決定性重視→決定的間引き（`SlidingWindow`/`Truncation`）、情報保持重視→LLM 要約（`Summarization`、ただし LLM 呼び出しが増える）。サンプル [13](Feature13Compaction/README.md) は決定的（LLM 不要）。

### ⚙️ D. パイプライン・制御（ミドルウェア）

| 機能 | なぜ必要か | 素の ChatClient に足すもの | 使い方の例 | 主な API |
| --- | --- | --- | --- | --- |
| **ミドルウェア** | 横断的関心事を本体に混ぜたくない | 実行とツール呼出の両層にフックを差し込み合成 | ログ/トレース・リトライ・入力ガード | `AsBuilder().Use` |
| **ロギング** | 入出力・ツールを残さないと調査・監査不能 | エージェント実行を既定形式でログ化 | 開発デバッグ・本番監査ログ | `UseLogging` |
| **カスタムエージェント** | 既定ミドルウェアでは表現しきれない独自処理 | 委譲で前後処理/フォールバックを足した独自エージェント | 入力正規化・出力ポリシー・代替先への切替 | `DelegatingAIAgent` |
| **メッセージ注入** | ツール実行の合間に割り込み制御したい | 関数実行ループ内へメッセージを注入 | 連鎖途中の方針補強・安全ガード挿入 | `MessageInjectingChatClient` |

### 🤝 E. マルチエージェント・オーケストレーション

| 機能 | なぜ必要か | 素の ChatClient に足すもの | 使い方の例 | 主な API |
| --- | --- | --- | --- | --- |
| **エージェントのツール化** | 1 体に全役割を詰めると指示肥大で精度低下 | エージェントをツールとして合成し多段構成にする | 調整役が専門エージェントへ委譲し統合 | `AsAIFunction` |
| **ワークフロー** 📦 | 業務フローは再現性・監査・耐障害性が要る | 複数ステップを信頼性高く編成する実行基盤（分岐/並列/復旧） | 抽出→検証→承認→記帳 の固定フロー | `Microsoft.Agents.AI.Workflows` |
| **バックグラウンドエージェント** 🧪 | 長時間処理で本体が止まると応答性が落ちる | 非同期な委譲と結果回収の枠組み | 裏で長時間調査し完了後に取込 | `BackgroundAgentsProvider` |

> 💡 **ツール化 vs ワークフロー**: 柔軟さ・最小依存なら「ツール化」（サンプル [07](Feature07MultiAgent/README.md)）、手順固定・分岐・チェックポイント・耐障害性が要件なら「ワークフロー」（別パッケージ）。

### 📊 F. 可観測性・評価

| 機能 | なぜ必要か | 素の ChatClient に足すもの | 使い方の例 | 主な API |
| --- | --- | --- | --- | --- |
| **テレメトリ** | 本番の遅延・トークン・失敗を追えないと運用不能 | GenAI 規約のトレース/メトリクスを自動付与 | OTLP で App Insights / Jaeger に送信 | `UseOpenTelemetry` |
| **評価（ローカル検査）** | 変更による品質劣化（回帰）を自動検知したい | 実行し応答をローカル検査で採点（追加 API 不要） | CI で禁止語・ツール呼出の有無を検査 | `LocalEvaluator`, `EvalChecks` |
| **評価（LLM as judge）** 📦 | 正解が一意でない出力は機械検査で測れない | LLM 採点をエージェント評価に組込 | 要約/対話の関連性・正確性スコア | `Microsoft.Extensions.AI.Evaluation` |

### 🔌 G. 相互運用・プロバイダー

| 機能 | なぜ必要か | 素の ChatClient に足すもの | 使い方の例 | 主な API |
| --- | --- | --- | --- | --- |
| **OpenAI Responses API** | Chat Completions でなく Responses API を使いたい | 入口を `ResponsesClient` に替えてエージェント化 | ステートフルな応答管理を利用 | `OpenAIResponseClientExtensions.AsAIAgent` |
| **OpenAI 相互運用** | 既存の OpenAI SDK 資産と相互接続したい | 入出力を OpenAI 型と相互変換するブリッジ | 段階的な移行・統合 | `AsOpenAIChatCompletion` |
| **A2A** 📦 | 他社/他フレームワークのエージェントと連携したい | 外部エージェントとの protocol 連携を付与 | 他社の予約/決済エージェントと協調 | A2A 対応パッケージ |
| **マルチプロバイダー** 📦 | コスト/性能/データ所在でモデルを選び替えたい | 各社 ChatClient を共通 `AIAgent` 抽象に載せ差替え自在 | 本番 Foundry / 開発 Ollama / 用途別 Claude | 各 SDK の `AsAIAgent` |
| **サーバー管理エージェント** ☁ | 定義をポータルで集中管理したい | サーバー側で管理される versioned エージェントに接続 | ホスト型ツールの運用 | `AIProjectClient` |
| **DI 連携** | DI で依存注入しサービスとして解決したい | 構築を DI コンテナと統合 | ASP.NET Core でエージェントをサービス登録 | `AIAgentBuilder` + `IServiceProvider` |

> 💡 **プロバイダー選定**: 本サンプル群は「endpoint＋APIキー＋chat デプロイメント」前提で **Foundry（`AzureOpenAIClient`）** を採用。ローカル検証重視なら Ollama / Foundry Local、ポータル集中管理なら「サーバー管理エージェント」を検討。

---

## 参考（一次情報・2026-06-20 確認）

- 公式ドキュメント / 概要: <https://learn.microsoft.com/en-us/agent-framework/> ・ <https://learn.microsoft.com/en-us/agent-framework/overview/>
- フレームワーク本体 / 公式サンプル (GitHub): <https://github.com/microsoft/agent-framework> ・ <https://github.com/microsoft/Agent-Framework-Samples>
- GA リリース (v1.0, 2026-04): <https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/>
- API 表面の根拠: NuGet `Microsoft.Agents.AI` / `.Abstractions` / `.OpenAI` **1.10.0** 同梱の XML ドキュメント

> 収録サンプルの一覧と実行方法は [README.md](README.md) を参照。
