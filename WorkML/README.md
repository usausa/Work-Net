# WorkML

電力・電圧の保安機器ログを用いた **予防保守（予知保全）** のサンプル。
Azure の時系列基盤モデル **TimeGEN-1** を .NET から呼び、予測（＋将来的に異常検知）を行う。

## 動かし方

### 前提
- .NET 10 SDK
- 実際に予測を呼ぶ場合のみ: Azure にデプロイした TimeGEN-1 のエンドポイント URL と API キー（下記「Azure 側の準備」）

### Azure 側の準備（TimeGEN-1 のデプロイ）
実推論には Azure 上に TimeGEN-1 のエンドポイントを用意する。TimeGEN-1 は Nixtla 製だが、Azure のモデルカタログから **サーバーレス API（従量課金）** として正式に利用できる（Azure OpenAI と同じ構図）。

1. **Azure サブスクリプション**を用意（従量課金が発生）。
2. **Azure AI Foundry**（<https://ai.azure.com>）または **Azure Machine Learning Studio** を開く。
3. **モデルカタログ**で `TimeGEN-1` を検索して選択。
4. **サーバーレス API（Models-as-a-Service）** としてデプロイ。初回は Azure Marketplace 経由の利用規約への同意が必要。
5. デプロイ完了後、**エンドポイント URL** と **API キー**を取得する。
6. 取得した値を環境変数に設定（下記「Azure に接続して予測」）。

> 課金は per-token の従量課金。画面手順の詳細・最新仕様は公式を参照（UI は変わるため最新版を確認すること）:
> - <https://learn.microsoft.com/en-us/azure/machine-learning/how-to-deploy-models-timegen-1>
> - <https://learn.microsoft.com/en-us/azure/ai-studio/how-to/deploy-models-timegen-1>

### ビルド
```
cd WorkML
dotnet build WorkML.slnx
```

### 実行（Azure 未接続でも動く）
```
dotnet run --project WorkML/WorkML.csproj
```
環境変数が未設定なら、**CSV 読込 → p.u. 正規化 → `unique_id, ds, y` 形式への変換 → 送信用 JSON の生成**までを表示して終了する。Azure 未契約でもデータ整形とリクエスト内容を確認できる。

### Azure に接続して予測
PowerShell で環境変数を設定して実行する:
```
$env:TIMEGEN_ENDPOINT = "https://<your-endpoint>"
$env:TIMEGEN_APIKEY   = "<your-key>"
dotnet run --project WorkML/WorkML.csproj
```
TimeGEN-1 エンドポイントへ予測リクエストを送り、系列ごとの予測値を表示する。
※ キー/エンドポイントはコードに直書きせず、環境変数（または User Secrets）で渡す。

### サンプルデータ（`WorkML/sample-data/`）
- `devices.csv`: 装置マスタ（3装置・site01/site02・基本電圧 **100V/200V** 混在・チャンネル数 **2/3/1** と可変）
- **装置ごとの長期間ログ**（**5分間隔 = 1日288件**、**60日分 = 17,280件/チャンネル**）:
  - `dev01.csv`（2ch・34,560行）/ `dev02.csv`（3ch・51,840行）/ `dev03.csv`（1ch・17,280行）
  - `dev03-ch1` は終盤に電圧降下（劣化の予兆）を含む
- `generate-sample-data.ps1`: 上記データの生成スクリプト（固定シードで再現可能）。件数を変えたいときはこれを編集して再生成する。

### 出力の見方
- `unique_id` は `{Site}-{Device}-ch{n}`（例 `site02-dev03-ch1`）。**1チャンネル = 1系列**。
- `y` は p.u.（基本電圧で正規化）。`dev03-ch1` が 1.0 から 0.91 付近へ低下するのが異常の例。

## プロジェクト構成
```
WorkML/                     ソリューションルート
  WorkML.slnx               ソリューション
  WorkML/                   アプリ（コンソール / net10.0）
    WorkML.csproj
    Program.cs              CSV→正規化→JSON→予測
    Core/                   ChannelReading / DeviceSpec / PerUnit(p.u.正規化) / CsvLoader
    TimeGen/                TimeGenClient(HttpClient) / TimeGenModels(DTO)
    sample-data/            装置ごとの長期間 CSV（dev01/02/03.csv）＋ 生成スクリプト
  WorkML.Tests/             xUnit テスト（PerUnit / CsvLoader）
  docs/                     設計・実装メモ・作業まとめ
```

## 方針（決定事項）
- **TimeGEN-1**（Nixtla 製、Azure ML / Foundry のモデルカタログ）を採用。学習不要で予測＋異常検知。
- Azure の **サーバーレス API**（従量課金）としてデプロイし、.NET は **`HttpClient` で REST**（公式 .NET SDK は無し）。
- 異なる基本電圧 → **標幺値(p.u.)正規化**。可変チャンネル/マルチサイト → **1チャンネル = 1系列（`unique_id`）**。

## なぜ TimeGEN-1 か
- Azure 専用の時系列異常検知（Anomaly Detector / Metrics Advisor）は 2026/10 廃止。
- AutoML（自分で学習）より基盤モデル（学習不要）が立ち上がりが速い。
- 基盤モデルの中で Azure ネイティブ MaaS ＋ 異常検知標準なのが TimeGEN-1。

## ドキュメント
- [設計・サンプル仕様](docs/PredictiveMaintenance-AutoML.md)
- [物語版（経緯と学び）](docs/blog-predictive-maintenance-timegen.md)
- [作業まとめ](docs/作業まとめ.md)
