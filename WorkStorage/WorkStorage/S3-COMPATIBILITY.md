# WorkStorage – S3 互換機能一覧

AWS S3 REST API との互換性状況をまとめます。

> **本プロジェクトの位置づけ**: 実運用向けではなく、AWS SDK を使った開発時のローカルダミー S3 として利用することを目的としています。

## 実装済み機能

### バケット操作

| 操作 | メソッド / パス | 説明 |
|---|---|---|
| ListBuckets | `GET /` | 全バケットを一覧取得 |
| CreateBucket | `PUT /{bucket}` | バケットを作成 |
| HeadBucket | `HEAD /{bucket}` | バケットの存在確認 |
| DeleteBucket | `DELETE /{bucket}` | バケット・メタデータ・タグ・ACL・CORS を再帰削除 |
| GetBucketLocation | `GET /{bucket}?location` | バケットのリージョン情報を返却（固定値 `us-east-1`） |
| GetBucketTagging | `GET /{bucket}?tagging` | バケットのタグセットを取得 |
| PutBucketTagging | `PUT /{bucket}?tagging` | バケットにタグセットを設定 |
| DeleteBucketTagging | `DELETE /{bucket}?tagging` | バケットのタグセットを削除 |
| GetBucketAcl | `GET /{bucket}?acl` | バケットの ACL を取得 |
| PutBucketAcl | `PUT /{bucket}?acl` | バケットの ACL を設定（`x-amz-acl` ヘッダー対応） |
| GetBucketCors | `GET /{bucket}?cors` | バケットの CORS 設定を取得 |
| PutBucketCors | `PUT /{bucket}?cors` | バケットの CORS 設定を保存（ミドルウェアで実行時に適用） |
| DeleteBucketCors | `DELETE /{bucket}?cors` | バケットの CORS 設定を削除 |

### オブジェクト操作

| 操作 | メソッド / パス | 説明 |
|---|---|---|
| PutObject | `PUT /{bucket}/{key}` | オブジェクトをアップロード。Content-Type・StorageClass・ACL・`x-amz-meta-*` を保存 |
| GetObject | `GET /{bucket}/{key}` | オブジェクトをダウンロード。メタデータと `x-amz-storage-class` を返却 |
| HeadObject | `HEAD /{bucket}/{key}` | オブジェクトのメタデータを取得（StorageClass 含む） |
| DeleteObject | `DELETE /{bucket}/{key}` | オブジェクトとメタデータを削除 |
| CopyObject | `PUT /{bucket}/{key}` + `x-amz-copy-source` | サーバー側コピー。`COPY\|REPLACE` ディレクティブ対応（StorageClass も処理） |
| DeleteObjects | `POST /{bucket}?delete` | 複数オブジェクトの一括削除。`Quiet` モード対応 |
| GetObjectTagging | `GET /{bucket}/{key}?tagging` | オブジェクトのタグセットを取得 |
| PutObjectTagging | `PUT /{bucket}/{key}?tagging` | オブジェクトにタグセットを設定 |
| DeleteObjectTagging | `DELETE /{bucket}/{key}?tagging` | オブジェクトのタグセットを削除 |
| GetObjectAcl | `GET /{bucket}/{key}?acl` | オブジェクトの ACL を取得 |
| PutObjectAcl | `PUT /{bucket}/{key}?acl` | オブジェクトの ACL を設定 |

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
| CreateMultipartUpload | `POST /{bucket}/{key}?uploads` | 開始。Content-Type・StorageClass・`x-amz-meta-*` を一時保存 |
| UploadPart | `PUT /{bucket}/{key}?partNumber=N&uploadId=ID` | パートをアップロード |
| CompleteMultipartUpload | `POST /{bucket}/{key}?uploadId=ID` | パートを結合。メタデータを復元 |
| AbortMultipartUpload | `DELETE /{bucket}/{key}?uploadId=ID` | 中止しパートを破棄 |
| ListMultipartUploads | `GET /{bucket}?uploads` | 進行中のマルチパートアップロードを一覧 |
| ListParts | `GET /{bucket}/{key}?uploadId=ID` | パートを一覧 |

### 条件付きリクエスト / Range

| ヘッダー | レスポンス |
|---|---|
| `If-None-Match` | `304 Not Modified` |
| `If-Modified-Since` | `304 Not Modified` |
| `If-Match` | `412 Precondition Failed` |
| `If-Unmodified-Since` | `412 Precondition Failed` |
| `Range: bytes=start-end` | `206 Partial Content` |

### メタデータ・タグ・ACL

| 機能 | 説明 |
|---|---|
| Content-Type 保持 | PutObject / Multipart Upload で保存し GET/HEAD で返却 |
| Storage Class | `x-amz-storage-class` ヘッダーを保存し GET/HEAD/ListObjects で返却 |
| ユーザー定義メタデータ | `x-amz-meta-*` ヘッダーの保存と返却 |
| メタデータディレクティブ | CopyObject の `x-amz-metadata-directive: COPY\|REPLACE` |
| Object Tagging | オブジェクト単位のタグセット管理 |
| Bucket Tagging | バケット単位のタグセット管理 |
| Bucket/Object ACL | Canned ACL の保存と XML 応答生成（`private`, `public-read`, `public-read-write`, `authenticated-read`） |

### Bucket CORS

| 機能 | 説明 |
|---|---|
| CORS 設定の保存/取得/削除 | `GET/PUT/DELETE /{bucket}?cors` で S3 標準の CORS XML を管理 |
| ミドルウェアによる実行時適用 | `Origin` ヘッダーを検出し、保存された CORS ルールに基づいて `Access-Control-*` ヘッダーを自動付与 |
| OPTIONS プリフライト | `Access-Control-Request-Method` に基づくプリフライト応答 |

### その他

| 機能 | 説明 |
|---|---|
| `x-amz-request-id` | 全レスポンスに一意のリクエストIDを付与（ミドルウェア） |
| ETag (MD5) | MD5 ハッシュを ETag として返却 |
| S3 XML 名前空間準拠 | `http://s3.amazonaws.com/doc/2006-03-01/` |
| パストラバーサル防止 | バリデーションとパス正規化 |
| 階層キー構造 | `/` をディレクトリ構造にマッピング |

---

## 未実装の機能

### 実装を検討する価値がある機能

| 機能 | 必要性 | 難易度 | 備考 |
|---|---|---|---|
| **Versioning** | ★★★ | ★★★★ | バージョニングを利用するアプリでは必須。ストレージ設計の大幅変更が必要。設計方針は [VERSIONING-DESIGN.md](./VERSIONING-DESIGN.md) を参照 |

### 開発ダミーとしての実装が不要な機能

以下の機能はローカル開発ダミーとしての利用時には実質的に意味を持たない、あるいは既に動作しているため、実装の必要性は低いと判断しています。

| 機能 | 必要性 | 難易度 | 不要な理由 |
|---|---|---|---|
| AWS Signature V4 | ★ | ★★★★★ | SDK はダミー認証情報で署名を送るが、サーバーが無視すれば全 API が動作する |
| Presigned URL | ★ | ★★★★ | 認証を検証しないため **既に動作する**。SDK が生成する URL はそのまま本サーバーを指す |
| Bucket Policy | ★ | ★★★★ | ローカル開発でアクセス制御は不要 |
| Bucket Lifecycle | ★ | ★★★★ | バックグラウンドでの自動削除/遷移であり開発・テスト時は不要 |
| Bucket Notification | ★★ | ★★★★ | イベント駆動テストに有用だが通常アプリ側でモックする |
| Bucket Logging | ★ | ★★ | 監査用途で開発時は不要 |
| Bucket Encryption | ★ | ★★★ | ローカルストレージに暗号化は無意味 |
| Bucket Replication | ★ | ★★★★★ | ローカル環境で意味を持たない |
| Object Versioning / ListObjectVersions | ★★★ | ★★★★ | Versioning 実装に依存（上記参照） |
| Object Lock / Legal Hold | ★ | ★★★ | コンプライアンス向け。開発時は不要 |
| Server-Side Encryption | ★ | ★★★ | ローカルでの暗号化は無意味。ヘッダー受理のみなら容易だが実用性なし |
| Object Restore | ★ | ★★★ | Glacier 復元のシミュレーション。ステータスタイマー管理が必要 |
| Select Object Content | ★ | ★★★★★ | SQL パーサー + 実行エンジンが必要。非常に複雑で用途も限定的 |
| Transfer Acceleration | ★ | ★ | ローカルでは意味がない |

---

## ストレージ構成

```
{BasePath}/
├── {bucket}/                       # オブジェクトデータ
│   └── key.txt
├── .meta/
│   ├── {bucket}/                   # オブジェクトメタデータ
│   │   └── key.txt.json
│   └── .buckets/                   # バケットレベルの設定
│       ├── {bucket}-tags.json      # バケットタグ
│       ├── {bucket}-acl.json       # バケット ACL
│       └── {bucket}-cors.json      # バケット CORS
└── .multipart/                     # マルチパート一時ファイル
    └── {uploadId}/
        ├── .info
        ├── .meta.json
        └── 1, 2, ...
```

オブジェクトメタデータ (`*.json`):

```json
{
  "ContentType": "application/json",
  "StorageClass": "STANDARD_IA",
  "Acl": "public-read",
  "UserMetadata": { "author": "demo" },
  "Tags": { "env": "dev" }
}
```
