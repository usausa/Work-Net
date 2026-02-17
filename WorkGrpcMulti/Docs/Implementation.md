# 実装解説

## アーキテクチャ概要

本サンプルは、Client - Proxy - Server の多段構成で双方向gRPC通信を行います。
Proxy と Server はイベント駆動型のマルチプレクサパターンで実装されています。

## Proxy の実装

### 旧実装の問題点

旧実装では4つの独立した `Task` が並行動作していました：

1. `clientReceiveTask`: Clientからのメッセージ受信
2. `serverSendTask`: Serverへのメッセージ送信
3. `serverReceiveTask`: Serverからのメッセージ受信
4. `clientSendTask`: Clientへのメッセージ送信

**スレッドセーフ性の問題:**
- `serverResponseReceived` などの共有変数に複数スレッドから同時アクセスする可能性
- ClientとServerからのメッセージが同時に処理され、処理順序が不定になる
- `Channel<T>` はスレッドセーフだが、メッセージ処理ロジック自体は保護されていない

### 新実装: シングルスレッド・マルチプレクサ方式

```
┌─────────────────────────────────────────────────────────────┐
│                      Event Channel                           │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐        │
│  │ Client  │  │ Server  │  │Timeout  │  │ Cancel  │        │
│  │ Message │  │ Message │  │  Event  │  │  Event  │        │
│  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘        │
│       │            │            │            │              │
│       └────────────┴────────────┴────────────┘              │
│                          │                                   │
│                    ┌─────▼─────┐                            │
│                    │  Single   │                            │
│                    │  Thread   │                            │
│                    │  Loop     │                            │
│                    └─────┬─────┘                            │
│                          │                                   │
│              ┌───────────┴───────────┐                      │
│              ▼                       ▼                      │
│     Send to Server            Send to Client                │
└─────────────────────────────────────────────────────────────┘
```

**設計のポイント:**

1. **統一イベントチャネル**: すべてのイベント（Client/Serverからのメッセージ）を `ProxyEvent` として単一のチャネルに集約

2. **シングルスレッド処理**: メインループが1つのスレッドでイベントを順次処理
   - 処理のシリアライズが自然に保証される
   - 共有状態へのアクセスが安全

3. **非同期受信タスク**: gRPCストリームからの受信は別タスクで行い、受信したメッセージをイベントチャネルに投入

```csharp
// イベント種別
enum ProxyEventType { ClientMessage, ServerMessage }

// 統一イベント
record ProxyEvent(ProxyEventType Type, object Message);

// メインループ（シングルスレッド）
await foreach (var evt in eventChannel.Reader.ReadAllAsync(ct))
{
    switch (evt.Type)
    {
        case ProxyEventType.ClientMessage:
            // Client -> Server への転送処理
            break;
        case ProxyEventType.ServerMessage:
            // Server -> Client への転送処理
            break;
    }
}
```

## Server の実装

### 旧実装の問題点

旧実装では `receiveTask` と メイン処理が並行動作し、`SemaphoreSlim` で同期していました：

**問題点:**
- `cancelled` や `currentRequestId` への並行アクセス
- `SemaphoreSlim.WaitAsync` はタイムアウトとキャンセルを別々に処理する必要がある
- 状態管理が複雑になりやすい

### 新実装: シングルスレッド・マルチプレクサ方式

```
┌─────────────────────────────────────────────────────────────┐
│                      Event Channel                           │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐                     │
│  │ Process │  │ Control │  │ Cancel  │                     │
│  │ Request │  │Response │  │ Request │                     │
│  └────┬────┘  └────┬────┘  └────┬────┘                     │
│       │            │            │                           │
│       └────────────┴────────────┘                           │
│                    │                                        │
│              ┌─────▼─────┐                                 │
│              │  Single   │                                 │
│              │  Thread   │                                 │
│              │   Loop    │                                 │
│              └─────┬─────┘                                 │
│                    │                                        │
│    ┌───────────────┼───────────────┐                       │
│    ▼               ▼               ▼                       │
│ Setting      Control Req     Process Resp                  │
│ Notification   (loop)          (final)                     │
└─────────────────────────────────────────────────────────────┘
```

**設計のポイント:**

1. **状態マシン**: Serverの処理を明確な状態遷移として表現
   - `WaitingForProcessRequest`: 処理要求待ち
   - `ProcessingControls`: 制御要求/応答処理中
   - `Completed`: 完了

2. **タイムアウト処理**: `Task.WhenAny` を使用して、以下を同時に待機
   - イベントチャネルからのメッセージ
   - タイムアウト用の `Task.Delay`
   - キャンセレーショントークン

```csharp
// 制御応答待ち（タイムアウト・キャンセル対応）
using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

try
{
    var evt = await eventChannel.Reader.ReadAsync(linkedCts.Token);
    // イベント処理
}
catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
{
    // タイムアウト処理
}
```

3. **キャンセル処理の統合**: キャンセル要求もイベントとして処理され、状態遷移を引き起こす

## まとめ

| 観点 | 旧実装 | 新実装 |
|------|--------|--------|
| スレッドモデル | マルチスレッド（4-5タスク） | シングルスレッド＋受信タスク |
| 同期機構 | Channel + SemaphoreSlim | Channel のみ |
| 状態管理 | 共有変数 | ローカル変数（シングルスレッド） |
| 処理順序 | 不定（並行処理） | シリアライズ保証 |
| 複雑度 | 高（同期が必要） | 低（自然なフロー） |

このマルチプレクサパターンは、複数のイベントソースを単一のスレッドで処理する必要がある場合に有効です。特に双方向通信のように、両方向からのメッセージを順序立てて処理する必要がある場合に適しています。
