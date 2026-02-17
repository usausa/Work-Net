# 概要

多段構成のgRPCのサンプルを実装します。
多段構成でバイディレクショナルなgRPC通信のサンプルです。

# 構成

Client - Proxy - Server の構成で通信を行ないます。

## Client

- Proxyへ接続し、以降はProxyからの指示により動作します

## Proxy

- ClientとServer間を仲介します
- Client側からみた時にはgRPCのサーバー、Serverに対してはgRPCのクライアントとして動作し、どちらかのコネクションからの受信をトリガーとして他方へリクエストの送信を行ないます
- このサンプルの肝となる部分です、Proxyが2つのgRPCコネクションを使用して、イベントドリブンで動作するところが知りたいというのがこのサンプルを作る目的です

## Server

- 制御の主体で、Proxyを通じてClientからのリクエスト受信後、Proxyを通じてClientに要求を送信/応答を受信し、最後に完了応答を返します

# プロトコル

なお、Client - Proxy間とProxy - Server間に同じ名前のコマンドがあっても、gRPC上の定義は別ものとして扱ってください。

## Client - Proxy

### 処理要求 Client -> Proxy

接続後、最初に送信する処理要求です。

### 処理応答 Client <- Proxy

通信完了を意味する処理応答で、Clientはこれを受信したら通信を終了します。

### キャンセル要求 Client -> Proxy

任意のタイミングで、Clientはキャンセル要求を送信できる形とします。

### 設定通知 Client <- Proxy

任意のタイミングでClientは設定通知を受信し、その内容で自身の状態を更新します。

### 制御要求 Client <- Proxy

制御要求を受信したら対応する制御応答を返します。

### 制御応答 Client -> Proxy

制御要求に対するレスポンスです。

## Proxy - Server

### 処理要求 Proxy -> Server

Clientからの処理要求を受信したら、ProxyはServerに処理要求を送信します。

### 処理応答 Proxy <- Server

Serverからの処理応答を受信したら、ProxyはClientに処理要求を送信します。

### キャンセル要求 Proxy -> Server

Clientからのキャンセル要求を受信したら、ProxyはServerにキャンセル要求を送信します。

### 設定通知 Proxy <- Server

Serverからの設定通知要求を受信したら、ProxyはClientに設定通知要求を送信します。

### 制御要求 Proxy <- Server

Serverからの制御要求を受信したら、ProxyはClientに制御要求要求を送信します。

### 制御応答 Proxy -> Server

Clientからの制御応答を受信したら、ProxyはServerに制御応答要求を送信します。

# 処理シーケンス

以下の表記で、CはClient、PはProxy、SはServerです。

```
C -> P : 処理要求
P -> S : 処理要求

(ここから処理の主体はServer)

S -> P : 設定通知
P -> C : 設定通知

(処理要求の内容に応じて、Serverは以下を複数回実行)

S -> P : 制御要求
P -> C : 制御要求
(Clientはここで少しだけウエイト)
C -> P : 制御応答
P -> C : 制御応答

(複数回の制御要求応答が終わった後に)

S -> P : 処理応答
P -> C : 処理応答

(Cはコネクション終了)
```

また、C->Pの処理要求送信後、Cは処理応答受信前までの任意のタイミングでキャンセル要求を送信できる形とします。
Proxy、Serverでキャンセル要求を受信した時の動きは以下とします。

## Proxy

Serverからの処理応答受信前であれば、Serverに対してキャンセル要求を送信。
処理応答受信後であればなにもしない。

## Server

制御要求送信・制御応答受信待ちのシーケンス中であれば、それを中断して処理応答を送信する。
