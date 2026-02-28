# WorkStorage – S3 互換機能一覧

AWS S3 REST API との互換性状況をまとめます。

## 実装済み機能

### バケット操作

| 操作 | メソッド / パス | 説明 |
|---|---|---|
| ListBuckets | `GET /` | 全バケットを一覧取得 |
| CreateBucket | `PUT /{bucket}` | バケットを作成 |
| HeadBucket | `HEAD /{bucket}` | バケットの存在確認 |
| DeleteBucket | `DELETE /{bucket}` | バケットとメタデータを再帰削除 |
| GetBucketLocation | `GET /{bucket}?location` | バケットのリージョン情報を返却（固定値 `us-east-1`） |

### オブジェクト操作

| 操作 | メソッド / パス | 説明 |
|---|---|---|
| PutObject | `PUT /{bucket}/{key}` | オブジェクトをアップロード。Content-Type と `x-amz-meta-*` を保存 |
| GetObject | `GET /{bucket}/{key}` | オブジェクトをダウンロード。保存された Content-Type と `x-amz-meta-*` を返却 |
| HeadObject | `HEAD /{bucket}/{key}` | オブジェクトのメタデータを取得 |
| DeleteObject | `DELETE /{bucket}/{key}` | オブジェクトとメタデータを削除。空ディレクトリを自動クリーンアップ |
| CopyObject | `PUT /{bucket}/{key}` + `x-amz-copy-source` | サーバー側コピー。`x-amz-metadata-directive: COPY\|REPLACE` に対応 |
| DeleteObjects | `POST /{bucket}?delete` | 複数オブジェクトの一括削除。`Quiet` モード対応 |

### ListObjectsV2

| 機能 | クエリパラメータ | 説明 |
|---|---|---|
| プレフィックス絞り込み | `prefix` | 指定プレフィックスに一致するキーのみ返却 |
| デリミタ（階層ブラウジング） | `delimiter` | `CommonPrefixes` によるディレクトリ風の一覧表示 |
| ページネーション | `max-keys`, `continuation-token` | `IsTruncated` / `NextContinuationToken` による結果分割 |
| 開始位置指定 | `start-after` | 辞書順で指定キーより後のオブジェクトのみ返却 |

### マルチパートアップロード

| 操作 | メソッド / パス | 説明 |
|---|---|---|
| CreateMultipartUpload | `POST /{bucket}/{key}?uploads` | アップロード開始。Content-Type と `x-amz-meta-*` を一時保存 |
| UploadPart | `PUT /{bucket}/{key}?partNumber=N&uploadId=ID` | パートをアップロード |
| CompleteMultipartUpload | `POST /{bucket}/{key}?uploadId=ID` | パートを結合して最終オブジェクトを生成。メタデータを復元 |
| AbortMultipartUpload | `DELETE /{bucket}/{key}?uploadId=ID` | アップロードを中止しパートを破棄 |

### 条件付きリクエスト

| ヘッダー | レスポンス | 対象 |
|---|---|---|
| `If-None-Match` | `304 Not Modified` | GET (自動), HEAD (手動評価) |
| `If-Modified-Since` | `304 Not Modified` | GET (自動), HEAD (手動評価) |
| `If-Match` | `412 Precondition Failed` | GET (自動), HEAD (手動評価) |
| `If-Unmodified-Since` | `412 Precondition Failed` | GET (自動), HEAD (手動評価) |

### Range リクエスト

| ヘッダー | レスポンス | 説明 |
|---|---|---|
| `Range: bytes=start-end` | `206 Partial Content` | GetObject で部分ダウンロード対応 |

### メタデータ

| 機能 | 説明 |
|---|---|
| Content-Type 保持 | PutObject / Multipart Upload で送信された Content-Type をサイドカーファイルに保存し、GET/HEAD で返却 |
| ユーザー定義メタデータ (`x-amz-meta-*`) | PutObject / Multipart Upload で送信されたカスタムメタデータを保存し、GET/HEAD で返却 |
| CopyObject メタデータディレクティブ | `x-amz-metadata-directive: COPY` (デフォルト) でソースのメタデータを保持、`REPLACE` でリクエストのメタデータに置換 |

### その他

| 機能 | 説明 |
|---|---|
| `x-amz-request-id` ヘッダー | 全レスポンスに一意のリクエストIDを付与（ミドルウェア） |
| ETag (MD5) | オブジェクトの MD5 ハッシュを ETag として返却 |
| S3 XML レスポンス | S3 標準の XML 名前空間 (`http://s3.amazonaws.com/doc/2006-03-01/`) に準拠 |
| パストラバーサル防止 | バケット名・キーのバリデーションとパス正規化による安全性確保 |
| 階層キー構造 | `/` を含むキーをファイルシステムのディレクトリ構造にマッピング |

---

## 未実装の機能

### 認証・認可

| 機能 | 説明 |
|---|---|
| AWS Signature V4 | リクエスト署名の検証 |
| Bucket Policy | JSON ポリシーによるアクセス制御 |
| ACL (Access Control List) | バケット/オブジェクト単位の ACL |
| Presigned URL | 署名付き URL の生成・検証 |

### バケット機能

| 機能 | 説明 |
|---|---|
| Bucket Versioning | 同一キーの複数バージョン管理 |
| Bucket Lifecycle | オブジェクトの自動削除・ストレージクラス遷移ルール |
| Bucket Notification | オブジェクト作成/削除時のイベント通知 (SNS/SQS/Lambda) |
| Bucket CORS | バケット単位の CORS 設定 |
| Bucket Logging | アクセスログの記録 |
| Bucket Tagging | バケットへのタグ付与 |
| Bucket Encryption | デフォルト暗号化設定 (SSE-S3, SSE-KMS) |
| Bucket Replication | クロスリージョンレプリケーション |

### オブジェクト機能

| 機能 | 説明 |
|---|---|
| Object Versioning | バージョニング有効時のオブジェクトバージョン管理 |
| Object Tagging | オブジェクトへのタグ付与 (`GET/PUT/DELETE /{bucket}/{key}?tagging`) |
| Object Lock / Legal Hold | オブジェクトの変更不可ロック |
| Server-Side Encryption | SSE-S3, SSE-KMS, SSE-C による暗号化 |
| Storage Class | STANDARD 以外のストレージクラス (GLACIER, INTELLIGENT_TIERING 等) |
| Object Restore | Glacier からの復元 |
| Select Object Content | SQL による CSV/JSON/Parquet のクエリ |

### リスト・検索

| 機能 | 説明 |
|---|---|
| ListObjectVersions | バージョン付きオブジェクトの一覧 |
| ListMultipartUploads | 進行中のマルチパートアップロード一覧 |
| ListParts | マルチパートアップロードのパート一覧 |

### 転送・パフォーマンス

| 機能 | 説明 |
|---|---|
| Transfer Acceleration | CloudFront エッジ経由の高速転送 |
| Byte-Range Fetch (Multipart) | 並列 Range リクエストによる高速ダウンロード（サーバー側対応は済み、クライアント側のみ） |

---

## メタデータストレージの構成

```
{BasePath}/
├── {bucket}/                  # オブジェクトデータ
│   ├── key1.txt
│   └── folder/
│       └── key2.txt
├── .meta/                     # メタデータ (JSON サイドカー)
│   └── {bucket}/
│       ├── key1.txt.json
│       └── folder/
│           └── key2.txt.json
└── .multipart/                # マルチパート一時ファイル
    └── {uploadId}/
        ├── .info              # bucket\nkey
        ├── .meta.json         # 開始時のメタデータ
        ├── 1                  # パート 1
        └── 2                  # パート 2
```

各メタデータファイルの形式:

```json
{
  "ContentType": "application/json",
  "UserMetadata": {
    "project": "work-storage",
    "version": "1.0"
  }
}
```
