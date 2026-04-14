# Claude Code プロキシ セットアップ手順

## 概要

このプロキシは、Claude Code から Anthropic API へのリクエストを中継し、レート制限の残量・リセット時刻・トークン使用量・コンテキストウィンドウ占有率をコンソールに表示します。

## プロキシの起動

```bash
cd WorkClaudeProxy
dotnet run
```

既定では `http://localhost:5182` でリッスンします（`WorkClaudeProxy/Properties/launchSettings.json` で変更可能）。

## Claude Code の設定

### 方法1: 環境変数（推奨・一時的）

Claude Code を起動する前に環境変数を設定します。

**Windows (PowerShell)**
```powershell
$env:ANTHROPIC_BASE_URL = "http://localhost:5182"
claude
```

**Windows (コマンドプロンプト)**
```cmd
set ANTHROPIC_BASE_URL=http://localhost:5182
claude
```

**Linux / macOS**
```bash
export ANTHROPIC_BASE_URL=http://localhost:5182
claude
```

### 方法2: Claude Code の settings.json（恒久的）

`~/.claude/settings.json` に以下を追加します。

```json
{
  "env": {
    "ANTHROPIC_BASE_URL": "http://localhost:5182"
  }
}
```

> **注意:** `ANTHROPIC_API_KEY` はプロキシに渡す必要はありません。Claude Code が直接 `x-api-key` ヘッダーに設定し、プロキシが Anthropic API へそのまま転送します。

### プロジェクト単位で設定する場合

プロジェクトルートの `.claude/settings.json` に設定します。

```json
{
  "env": {
    "ANTHROPIC_BASE_URL": "http://localhost:5182"
  }
}
```

## コンソール出力例

```
info: ClaudeProxy[0]

  ┌─── POST /v1/messages [200]
  │  Model: claude-opus-4-6-20250514
  │  Token Usage:
  │    Input:      1,234  (cache read: 567 / created: 123)
  │    Output:       456
  │    Context: 1,924 / 200,000 (1.0% of context window used)
  │  Rate Limits:
  │    Requests:       42 / 100  (resets 12:00:00 +09:00)
  │    In tokens:  12,345 / 100,000  (resets 12:05:00 +09:00)
  │    Out tokens:  4,567 / 40,000  (resets 12:05:00 +09:00)
  └────────────────────────────────────────────────
```

## ポートの変更

`WorkClaudeProxy/Properties/launchSettings.json` の `applicationUrl` を変更します。

```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:8080"
    }
  }
}
```

変更後、`settings.json` の `ANTHROPIC_BASE_URL` も合わせて更新してください。
