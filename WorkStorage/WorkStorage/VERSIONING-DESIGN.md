# Versioning 実装設計方針

WorkStorage に S3 互換のバケットバージョニングを追加する場合の設計方針をまとめる。

## 概要

S3 のバージョニングでは、同一キーに対する上書き・削除が過去のバージョンを保持したまま行われる。各バージョンにはユニークな `VersionId` が付与され、特定バージョンの取得・削除が可能になる。

## S3 バージョニングの動作仕様

### バケットの状態遷移

```
Unversioned (初期状態)
    ↓ PUT ?versioning (Status=Enabled)
Enabled
    ↓ PUT ?versioning (Status=Suspended)
Suspended
```

- **Unversioned**: 従来動作。VersionId は `null`
- **Enabled**: すべての PUT/DELETE がバージョンを生成
- **Suspended**: 新規 PUT は VersionId=`null` で上書き。既存バージョンは保持

### 必要な API

| 操作 | エンドポイント | 説明 |
|---|---|---|
| GetBucketVersioning | `GET /{bucket}?versioning` | バージョニング状態の取得 |
| PutBucketVersioning | `PUT /{bucket}?versioning` | バージョニング状態の設定 |
| ListObjectVersions | `GET /{bucket}?versions` | 全バージョン + 削除マーカーの一覧 |
| GetObject (versioned) | `GET /{bucket}/{key}?versionId=ID` | 特定バージョンの取得 |
| HeadObject (versioned) | `HEAD /{bucket}/{key}?versionId=ID` | 特定バージョンのメタデータ取得 |
| DeleteObject (versioned) | `DELETE /{bucket}/{key}?versionId=ID` | 特定バージョンの物理削除 |
| DeleteObject (non-versioned) | `DELETE /{bucket}/{key}` | 削除マーカーの挿入 |

## ストレージ設計

### 方針 A: バージョンディレクトリ方式（推奨）

各オブジェクトのバージョンを専用ディレクトリに格納する。

```
{BasePath}/
├── {bucket}/
│   └── key.txt                         # 最新バージョンへのコピー（高速アクセス用）
├── .versions/
│   └── {bucket}/
│       └── key.txt/
│           ├── _manifest.json           # バージョン履歴
│           ├── {versionId1}.data        # バージョン 1 のデータ
│           ├── {versionId1}.meta.json   # バージョン 1 のメタデータ
│           ├── {versionId2}.data        # バージョン 2 のデータ
│           └── {versionId2}.meta.json   # バージョン 2 のメタデータ
└── .meta/
    └── .buckets/
        └── {bucket}-versioning.json     # バケットのバージョニング状態
```

**_manifest.json の構造**:

```json
{
  "CurrentVersionId": "v2a1b3c4d5",
  "Versions": [
    {
      "VersionId": "v2a1b3c4d5",
      "IsLatest": true,
      "IsDeleteMarker": false,
      "LastModified": "2024-01-15T10:30:00.000Z",
      "ETag": "\"d41d8cd98f00b204e9800998ecf8427e\"",
      "Size": 1234
    },
    {
      "VersionId": "v1f6e7d8c9",
      "IsLatest": false,
      "IsDeleteMarker": false,
      "LastModified": "2024-01-14T09:00:00.000Z",
      "ETag": "\"098f6bcd4621d373cade4e832627b4f6\"",
      "Size": 567
    }
  ]
}
```

**メリット**:
- 最新バージョンは従来パス (`{bucket}/key.txt`) にコピーされるため、既存のバージョニング無効コードパスが変更不要
- バージョン履歴がファイルシステム上で明確に分離される
- ListObjectVersions がマニフェストを読むだけで完了

**デメリット**:
- PutObject 時に最新バージョンのコピーが発生（書き込みコスト増）
- マニフェストファイルの一貫性管理が必要

### 方針 B: メタデータ拡張方式

既存のメタデータサイドカーにバージョン情報を追加し、バージョンデータをサフィックス付きで同一ディレクトリに格納する。

```
{BasePath}/
├── {bucket}/
│   ├── key.txt                    # 最新バージョン
│   ├── key.txt@@v1f6e7d8c9        # 過去バージョン
│   └── key.txt@@v2a1b3c4d5        # 過去バージョン
├── .meta/
│   └── {bucket}/
│       ├── key.txt.json           # 最新メタデータ（既存フィールド + VersionId）
│       ├── key.txt@@v1f6e7d8c9.json
│       └── key.txt@@v2a1b3c4d5.json
```

**メリット**:
- 追加のディレクトリ構造が不要
- 既存のメタデータインフラを流用可能

**デメリット**:
- ListObjects でバージョンファイル (`@@`) をフィルタリングする必要がある
- ファイル名にバージョン ID を含むため、キー名に `@@` を含むオブジェクトとの衝突リスク
- バージョン一覧取得時にディレクトリスキャンが必要

### 推奨: 方針 A

方針 A は既存コードへの影響が最も少なく、バージョニング無効時の動作が完全に従来互換になる。

## 実装ステップ

### Phase 1: バケットバージョニング状態管理

1. `GET/PUT /{bucket}?versioning` の実装
2. バージョニング状態の永続化 (`.meta/.buckets/{bucket}-versioning.json`)
3. 状態: `Unversioned` → `Enabled` → `Suspended` の遷移

### Phase 2: バージョン付き PutObject

1. バージョニング有効時、PutObject でバージョン ID (ULID or UUID) を生成
2. `.versions/{bucket}/{key}/` にデータとメタデータを保存
3. `_manifest.json` を更新
4. 最新バージョンを `{bucket}/{key}` にコピー
5. レスポンスヘッダーに `x-amz-version-id` を追加

### Phase 3: バージョン付き GetObject / HeadObject

1. `?versionId=ID` クエリパラメータの処理
2. 指定バージョンのデータ/メタデータを `.versions/` から読み取り
3. 削除マーカーの場合は `405 Method Not Allowed` を返却
4. `x-amz-version-id` レスポンスヘッダー

### Phase 4: バージョン付き DeleteObject

1. `DELETE /{bucket}/{key}` (VersionId なし): 削除マーカーを挿入
2. `DELETE /{bucket}/{key}?versionId=ID`: 特定バージョンの物理削除
3. 削除マーカーが最新の場合、`{bucket}/{key}` のファイルを削除
4. マニフェストの更新

### Phase 5: ListObjectVersions

1. `GET /{bucket}?versions` の実装
2. マニフェストファイルからバージョン一覧を集約
3. `prefix`, `delimiter`, `key-marker`, `version-id-marker` による絞り込み

### Phase 6: Suspended 状態

1. Suspended 時の PutObject: VersionId=`null` で上書き（バージョン生成なし）
2. 既存バージョンは保持（削除されない）

## VersionId の生成

ULID (Universally Unique Lexicographically Sortable Identifier) を推奨:
- タイムスタンプ内蔵で時系列ソートが可能
- UUID v4 よりも可読性が高い
- `Ulid.NewUlid().ToString()` で生成（NuGet: `Ulid`）

代替: `Guid.NewGuid().ToString("N")` でも可。ソート順が保証されない点に注意。

## 影響範囲

| 既存機能 | 影響 |
|---|---|
| PutObject | バージョニング有効時にバージョン保存ロジックを追加 |
| GetObject | `?versionId` パラメータの処理を追加 |
| HeadObject | 同上 |
| DeleteObject | 削除マーカー挿入 or 物理削除の分岐を追加 |
| ListObjects | 変更なし（最新バージョンのみ = 従来パスのファイル） |
| CopyObject | ソースの `?versionId` 対応 |
| DeleteObjects | 各キーに対してバージョン付き削除ロジックを適用 |
| Object Tagging | バージョン別タグ管理（将来） |

## 工数見積もり

| Phase | 推定工数 | 内容 |
|---|---|---|
| Phase 1 | 小 | API 2本 + JSON 保存 |
| Phase 2 | 大 | PutObject 改修 + バージョンストレージ + マニフェスト |
| Phase 3 | 中 | GetObject/HeadObject の条件分岐 |
| Phase 4 | 大 | 削除マーカー + 物理削除 + マニフェスト更新 |
| Phase 5 | 中 | XML レスポンス生成 + フィルタリング |
| Phase 6 | 小 | Suspended 時の条件分岐 |

**合計**: Phase 1–6 で中～大規模の変更。特に Phase 2 と Phase 4 がコア部分であり、マニフェストの一貫性管理が実装上の最大の課題。
