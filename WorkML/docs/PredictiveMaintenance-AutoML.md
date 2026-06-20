# 保安機器ログを用いた予防保守 — Azure AutoML 系サービス × .NET 検討ドキュメント

> 作成日: 2026-06-20 / 対象: 電力・電圧保安機器のログデータによる予防保守（予知保全）
> スコープ: 「Azure の AutoML 系サービスを .NET から使う」ための選択肢整理・アーキテクチャ・サンプルプログラム仕様

---

## 0. 重要な前提（サービス提供状況・2026年6月時点）

機械学習サービスは提供終了が頻繁にあるため、最初に現状を明記する。

| サービス | 状況 | 本検討での扱い |
| --- | --- | --- |
| **Azure Machine Learning（AutoML 機能）** | 現役・推奨 | **主軸**。時系列予測 / 回帰 / 分類の自動学習に対応 |
| **ML.NET（`Microsoft.ML` / `Microsoft.ML.AutoML` / `Microsoft.ML.TimeSeries`）** | 現役 | **.NET ネイティブの補完**。PoC・オンプレ・エッジ推論に最適 |
| **Azure AI Anomaly Detector** | **2026/10/01 廃止**（2023/09 以降、新規リソース作成不可） | **新規採用しない**。代替を提示 |
| **Microsoft Fabric（Real-Time Intelligence / KQL anomaly 関数）** | 現役 | Anomaly Detector の事実上の後継。将来の選択肢として記載 |

> ⚠️ **Azure AI Anomaly Detector は採用不可**。Microsoft は OSS の [`microsoft/anomaly-detector`](https://github.com/microsoft/anomaly-detector) を内包する **Microsoft Fabric** または OSS への移行を案内している。本ドキュメントでは Azure ML AutoML と ML.NET を中心に据える。

---

## 1. ゴールとデータの整理

### 1.1 やりたいこと
電力・電圧の保安機器ログから、**故障・劣化の予兆を事前に検知**して保守につなげる（＝予防保守 / Predictive Maintenance）。

### 1.2 データと環境の特徴（難所）
| 特徴 | 内容 | 設計上の論点 |
| --- | --- | --- |
| マルチサイト | 保安機器が複数拠点に設置 | 拠点・装置をまたいだモデルの一般化、拠点固有差の吸収 |
| 可変チャンネル数 | 装置ごとに接続チャンネル数が異なる | 固定長ベクトルを前提とする手法をそのまま使えない |
| 異なる基本電圧 | 100V / 200V など定格が混在 | 絶対値のまま学習すると装置横断で比較不能 |
| 時系列 | 電力・電圧の時間変化ログ | 季節性・トレンド・周期、欠測、サンプリング間隔の扱い |

この 3 つ（マルチサイト・可変チャンネル・異電圧）をどう正規化・モデル化するかが本件の技術的な肝。**→ 詳細は §4。**

---

## 2. 予防保守で「何ができるか」── ML タスクの4類型

予防保守は単一の手法ではなく、目的とラベルの有無で手法を選ぶ。

| # | タスク | 問い | 必要なラベル | 代表手法 | 向くサービス |
| --- | --- | --- | --- | --- | --- |
| A | **異常検知** | 「今、普段と違う挙動か？」 | 不要（正常データのみで可） | SR-CNN / SSA / IID スパイク・変化点 | ML.NET TimeSeries / Fabric |
| B | **故障予測（分類）** | 「N日以内に故障するか？」 | 過去の故障履歴（教師あり） | 二値分類（LightGBM 等） | **Azure ML AutoML** / ML.NET AutoML |
| C | **残存有効寿命（RUL）回帰** | 「あと何時間/サイクルで限界か？」 | 故障までの経過ラベル | 回帰 | **Azure ML AutoML** / ML.NET AutoML |
| D | **時系列予測（予兆）** | 「将来値が閾値を超えるか？」 | 不要〜弱（履歴のみ） | Forecasting（AutoML）/ SSA 予測 | **Azure ML AutoML Forecasting** / ML.NET |

### 推奨の進め方（段階的）
1. **まず A（異常検知）から**。故障ラベルが無くても「正常からの逸脱」を検知でき、すぐ価値が出る。ML.NET でローカル完結が可能。
2. 故障/保守の**履歴が貯まったら B・C** に進み、Azure ML AutoML で「いつ壊れるか」を学習。
3. **D（予測）**で電圧降下トレンド等の先読みを併用。

> 多くの現場で「ラベル（故障履歴）が十分に無い」ため、**異常検知 → ラベル蓄積 → 故障予測** の順が現実的。

---

## 3. サービス選択肢の比較と .NET 連携方式

### 3.1 比較表

| 観点 | Azure ML AutoML | ML.NET（+AutoML/TimeSeries） | Microsoft Fabric |
| --- | --- | --- | --- |
| 実行場所 | クラウド（学習）/ 推論はクラウド or ローカル | **ローカル/.NET 完結**（学習も推論も） | クラウド |
| .NET からの学習 | SDK/REST でジョブ投入（学習計算はクラウド） | **C# で直接学習** | 不可（KQL/Spark 中心） |
| .NET からの推論 | ① ONNX をローカル推論 ② オンライン エンドポイント REST | **C# で直接推論** | REST/KQL |
| 自動化対象 | 回帰・分類・**時系列予測**・特徴量選択・HPO | 回帰・分類・レコメンド（AutoML API）、時系列は TimeSeries で | 異常検知・予測関数 |
| 強み | 大規模・高精度・MLOps・複数系列(grain) | 依存最小・オフライン・低レイテンシ・組込み | リアルタイム・大量ストリーム |
| 弱み | クラウド運用コスト・学習は Python 寄り | 大規模分散学習は不得手 | .NET 学習不可 |
| 本件での役割 | **本番の学習基盤** | **PoC・エッジ推論・常駐監視** | 将来のストリーム処理 |

### 3.2 .NET 連携の3パターン

```mermaid
flowchart LR
    subgraph Pattern A: AutoML 学習 → ONNX ローカル推論
      A1[Azure ML AutoML<br/>学習ジョブ] -->|ONNX 出力| A2[(model.onnx)]
      A2 --> A3[.NET アプリ<br/>ML.NET + ONNX Runtime]
    end
    subgraph Pattern B: AutoML 学習 → REST 推論
      B1[Azure ML AutoML] -->|デプロイ| B2[オンライン<br/>エンドポイント]
      B3[.NET アプリ<br/>HttpClient] -->|HTTPS/JSON| B2
    end
    subgraph Pattern C: ML.NET 完結
      C1[.NET アプリ<br/>ML.NET で学習] --> C2[.zip モデル]
      C2 --> C3[.NET アプリで推論]
    end
```

| パターン | 学習 | 推論 | 推奨用途 |
| --- | --- | --- | --- |
| **A. AutoML → ONNX → ML.NET** | クラウド(AutoML) | **ローカル/.NET**（低レイテンシ・オフライン可） | 装置近傍・常駐監視・本命の本番構成 |
| **B. AutoML → オンライン エンドポイント** | クラウド(AutoML) | クラウド(REST) | サーバ集約・モデル更新を頻繁にしたい場合 |
| **C. ML.NET 完結** | **ローカル/.NET** | **ローカル/.NET** | PoC・小規模・ラベル不要な異常検知 |

> **本件の推奨**: PoC は **C（ML.NET 異常検知）** で素早く立ち上げ、本番は **A（AutoML 学習 + ONNX をエッジ/サーバで ML.NET 推論）** へ移行。B は集中管理が要件のときに選ぶ。

---

## 4. データ設計 —— 可変チャンネル・異電圧・マルチサイトの吸収（最重要）

### 4.1 異なる基本電圧 → 標幺値（per-unit, p.u.）正規化
電力分野の定石。**実測値を装置の基準値で割って無次元化**する。

```
電圧(p.u.)  = 実測電圧 / 基本電圧(定格)      例: 198V / 200V = 0.99,  99V / 100V = 0.99
電流(p.u.)  = 実測電流 / 定格電流
電力(p.u.)  = 実測電力 / 定格容量
```

- 100V 系も 200V 系も **1.0 を中心とした同一スケール**になり、装置横断で学習・比較できる。
- 異常は「定格からの乖離（例: 0.85 p.u. への電圧降下）」として表現でき、現場の感覚とも一致。
- マスタに **基本電圧・定格** を保持し、取り込み時に p.u. 変換する。

### 4.2 可変チャンネル数 → 2つの設計
| 方式 | 考え方 | 長所 | 短所 | 適合タスク |
| --- | --- | --- | --- | --- |
| **(1) チャンネル＝系列(grain)** | 1チャンネル=1時系列として独立に扱い、装置・チャンネルを系列ID（grain）に | チャンネル数が違っても破綻しない。AutoML Forecasting が複数 grain を1モデルで学習可 | チャンネル間の相関は別途特徴量で補う | A 異常検知 / D 予測 |
| **(2) 装置単位の固定長ベクトル化** | チャンネル群を**統計量に集約**（平均・分散・最小・最大・歪度・相関 等）して固定長に | チャンネル数非依存。装置全体の状態を1レコードで扱える | チャンネル個別の異常は埋もれやすい | B 故障予測 / C RUL |

> 実務では **(1) と (2) の併用**が有効: チャンネル単位で異常検知しつつ、装置単位の集約特徴で故障予測する。

### 4.3 メタデータ（マスタ）設計
```
Site(拠点)        : SiteId, 名称, 地域, ...
Device(装置)      : DeviceId, SiteId, 機種, 設置日, 基本電圧(V), 定格電流(A), 定格容量(VA), チャンネル数
Channel(チャンネル): DeviceId, ChannelNo, 用途, 基準値(p.u.算出用), 物理量種別(電圧/電流/電力)
Reading(ログ)     : DeviceId, ChannelNo, Timestamp, Value(実測), ValuePu(正規化後)
MaintenanceEvent  : DeviceId, Timestamp, 種別(故障/点検/交換), 内容  ← タスクB/Cの教師ラベル源
```

### 4.4 特徴量エンジニアリングの定石
- **時間特徴**: 移動平均/分散（短期・長期）、傾き（トレンド）、前回値との差分、変化率
- **周波数/形状**: ピーク数、ゼロクロス、リップル、（必要なら）FFT 由来のスペクトル特徴
- **集約**（装置単位）: チャンネル横断の平均・ばらつき・相関・不均衡度
- **コンテキスト**: 時刻/曜日/季節、負荷状態、温度等の外部変数（あれば）
- **欠測・外れ値処理**: 補間方針、サンプリング間隔の統一（リサンプリング）

---

## 5. サンプルプログラムの仕様

### 5.1 全体構成（`WorkML/` 配下）

```
WorkML/
  docs/
    PredictiveMaintenance-AutoML.md   ← 本書
  WorkML.slnx                          ← ソリューション（任意）
  PdM.Core/                            ← 共通ライブラリ（データモデル・p.u.正規化・特徴量）
  PdM.LocalAnomaly/                    ← サンプル1: ML.NET ローカル異常検知（タスクA, パターンC）
  PdM.OnnxInference/                   ← サンプル2: AutoML 出力 ONNX を ML.NET で推論（パターンA）
  PdM.EndpointClient/                  ← サンプル3: オンライン エンドポイント REST 呼び出し（パターンB）
  PdM.AutoMLTrain/                     ← サンプル4: ML.NET AutoML で故障予測/回帰を学習（タスクB/C, パターンC）
```

> 共通規約: `net9.0`、`ImplicitUsings`/`Nullable` 有効、file-scoped namespace、メンバ変数に `_` プレフィックスを付けない、ビルド警告ゼロ（.editorconfig 準拠）。

### 5.2 共通データモデル（`PdM.Core`）

```csharp
namespace PdM.Core;

// 1サンプリング点（チャンネル単位の生ログ）
public sealed class ChannelReading
{
    public string DeviceId { get; init; } = default!;
    public int ChannelNo { get; init; }
    public DateTime Timestamp { get; init; }
    public float Value { get; init; }          // 実測値
}

// 装置マスタ（p.u. 正規化と特徴量に必要なメタデータ）
public sealed class DeviceSpec
{
    public string DeviceId { get; init; } = default!;
    public string SiteId { get; init; } = default!;
    public float BaseVoltage { get; init; }    // 基本電圧 100 / 200 など
    public float RatedCurrent { get; init; }
    public int ChannelCount { get; init; }
}

// p.u. 正規化ユーティリティ（異電圧の吸収）
public static class PerUnit
{
    // 電圧を基本電圧で正規化（100V系/200V系を 1.0 中心に統一）
    public static float Voltage(float measured, DeviceSpec spec) => measured / spec.BaseVoltage;

    public static float Current(float measured, DeviceSpec spec) => measured / spec.RatedCurrent;
}
```

### 5.3 サンプル1: `PdM.LocalAnomaly`（ML.NET ローカル異常検知）

**目的**: 故障ラベルが無くても動く、最初の一歩。p.u. 正規化した1チャンネルの時系列に対して **SR-CNN（バッチ）** と **SSA スパイク/変化点（逐次）** で異常を検出する。

- 参照パッケージ: `Microsoft.ML`, `Microsoft.ML.TimeSeries`
- 入力: `ChannelReading[]`（または CSV）＋ `DeviceSpec`
- 出力: 各点の `IsAnomaly / Score / Magnitude`（SR-CNN）, `Alert / Score / P-Value`（SSA）
- 処理フロー: ① 読込 → ② p.u. 正規化 → ③ `IDataView` 化 → ④ 検出器適用 → ⑤ アラート出力

```csharp
namespace PdM.LocalAnomaly;

using Microsoft.ML;
using Microsoft.ML.Data;

public sealed class VoltagePoint
{
    public float ValuePu { get; set; }   // p.u. 正規化済み電圧
}

public sealed class SrCnnResult
{
    // [0]=IsAnomaly(0/1), [1]=RawScore, [2]=Magnitude
    [VectorType(3)]
    public double[] Prediction { get; set; } = default!;
}

public static class SrCnnAnomalyDetector
{
    // バッチ一括の SR-CNN 異常検知（全期間をまとめて評価）
    public static void Detect(MLContext mlContext, IDataView data)
    {
        var transformed = mlContext.AnomalyDetection.DetectEntireAnomalyBySrCnn(
            data,
            outputColumnName: nameof(SrCnnResult.Prediction),
            inputColumnName: nameof(VoltagePoint.ValuePu),
            new Microsoft.ML.TimeSeries.SrCnnEntireDetectOptions
            {
                Threshold = 0.3,
                Sensitivity = 90.0,
                DetectMode = Microsoft.ML.TimeSeries.SrCnnDetectMode.AnomalyAndMargin
            });

        var results = mlContext.Data.CreateEnumerable<SrCnnResult>(transformed, reuseRowObject: false);
        foreach (var (r, i) in results.Select((r, i) => (r, i)))
        {
            if (r.Prediction[0] == 1)
            {
                Console.WriteLine($"異常: index={i}, score={r.Prediction[1]:F3}, magnitude={r.Prediction[2]:F3}");
            }
        }
    }
}
```

> 逐次（ストリーミング）監視が必要なら `DetectSpikeBySsa` / `DetectChangePointBySsa` と `CreateTimeSeriesEngine`（`TimeSeriesPredictionEngine`）でオンライン検出に切り替える。常駐監視サービスはこちら。

### 5.4 サンプル2: `PdM.OnnxInference`（AutoML 出力の ONNX を ML.NET で推論）

**目的**: Azure ML AutoML でクラウド学習したモデルを **ONNX で書き出し**、`.NET` 側で**ローカル推論**（パターンA／本番本命）。ネットワーク遅延なし・オフライン可。

- 参照パッケージ: `Microsoft.ML`, `Microsoft.ML.OnnxRuntime`, `Microsoft.ML.OnnxTransformer`
- 入力: 装置単位の固定長特徴ベクトル（§4.2 (2)）
- 出力: スコア（故障確率 / 予測値）
- ポイント: 入出力カラム名は **AutoML が生成した ONNX のスキーマに一致**させる（`[ColumnName]` で合わせる）

```csharp
namespace PdM.OnnxInference;

using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Onnx;

public sealed class FeatureVector
{
    // 装置単位の集約特徴（例: 平均/分散/最小/最大/相関 … 固定長）
    [ColumnName("input")]
    [VectorType(8)]
    public float[] Features { get; set; } = default!;
}

public sealed class OnnxScore
{
    [ColumnName("output")]
    [VectorType(1)]
    public float[] Score { get; set; } = default!;
}

public static class OnnxModelRunner
{
    public static PredictionEngine<FeatureVector, OnnxScore> Build(MLContext mlContext, string onnxPath)
    {
        var pipeline = mlContext.Transforms.ApplyOnnxModel(
            modelFile: onnxPath,
            outputColumnNames: ["output"],
            inputColumnNames: ["input"]);

        var empty = mlContext.Data.LoadFromEnumerable(Array.Empty<FeatureVector>());
        var model = pipeline.Fit(empty);
        return mlContext.Model.CreatePredictionEngine<FeatureVector, OnnxScore>(model);
    }
}
```

### 5.5 サンプル3: `PdM.EndpointClient`（オンライン エンドポイント REST 呼び出し）

**目的**: AutoML モデルを Azure ML の**オンライン エンドポイント**にデプロイし、`.NET` から **HTTPS/JSON** で推論（パターンB）。集中管理・頻繁なモデル更新に向く。

- 認証: エンドポイントのキー、または Microsoft Entra ID（Bearer トークン）
- 送信ボディ: エンドポイントの scoring スキーマに合わせる（例の `input_data` 形式は AutoML 既定の一例）
- 設定（Key/Endpoint）は引数・環境変数・User Secrets 等で外出し（コードに直書きしない）

```csharp
namespace PdM.EndpointClient;

using System.Net.Http.Headers;
using System.Net.Http.Json;

public sealed class EndpointClient(HttpClient httpClient, string endpoint, string apiKey)
{
    public async Task<string> ScoreAsync(IReadOnlyList<string> columns, IReadOnlyList<float[]> rows, CancellationToken ct = default)
    {
        var payload = new { input_data = new { columns, data = rows } };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
}
```

### 5.6 サンプル4: `PdM.AutoMLTrain`（ML.NET AutoML で故障予測を学習）

**目的**: 故障履歴ラベルが貯まった段階で、**`.NET` だけで** 自動学習（タスクB 二値分類 / タスクC 回帰、パターンC）。Azure を使わず PoC を回せる。

- 参照パッケージ: `Microsoft.ML`, `Microsoft.ML.AutoML`
- 入力: 装置単位の固定長特徴ベクトル ＋ ラベル（N日以内故障の 0/1、または RUL）
- 出力: 学習済み `.zip` モデル ＋ 評価指標（AUC / RMSE 等）
- フロー: ① 特徴量+ラベル生成 → ② `AutoMLExperiment` で探索 → ③ ベストモデル保存

```csharp
namespace PdM.AutoMLTrain;

using Microsoft.ML;
using Microsoft.ML.AutoML;

public static class FailureClassifierTrainer
{
    public static async Task TrainAsync(MLContext mlContext, IDataView trainData, string labelColumn)
    {
        var experiment = mlContext.Auto()
            .CreateBinaryClassificationExperiment(new BinaryExperimentSettings
            {
                MaxExperimentTimeInSeconds = 300,
                OptimizingMetric = BinaryClassificationMetric.AreaUnderRocCurve
            });

        var result = experiment.Execute(trainData, labelColumnName: labelColumn);
        Console.WriteLine($"Best: {result.BestRun.TrainerName}, AUC={result.BestRun.ValidationMetrics.AreaUnderRocCurve:F3}");

        mlContext.Model.Save(result.BestRun.Model, trainData.Schema, "failure-model.zip");
        await Task.CompletedTask;
    }
}
```

> Azure ML AutoML 側（クラウド学習・時系列予測 grain 対応・ONNX 出力）は、学習ジョブの定義が Python/CLI/Studio 寄り。`.NET` は「データ準備・ジョブ投入(REST/SDK)・成果物(ONNX)の取り込み・推論」を担当する役割分担が現実的。

---

## 6. 進め方（ロードマップ）

| フェーズ | 内容 | 使う物 | 成果 |
| --- | --- | --- | --- |
| 0. データ整備 | マスタ整備・p.u. 正規化・リサンプリング・欠測処理 | `PdM.Core` | 学習可能なデータ基盤 |
| 1. PoC（異常検知） | 1〜数装置で SR-CNN/SSA を試す | `PdM.LocalAnomaly` | 予兆検知の手応え・閾値感 |
| 2. ラベル蓄積 | 検知結果と実故障/点検を突き合わせて教師データ化 | `MaintenanceEvent` | タスクB/C 用ラベル |
| 3. 故障予測 | AutoML で「N日以内故障」を学習・評価 | `PdM.AutoMLTrain` or Azure ML | 故障確率モデル |
| 4. 本番化 | ONNX をエッジ/サーバで常駐推論、または REST | `PdM.OnnxInference` / `PdM.EndpointClient` | 運用監視 |
| 5. 拡張 | 多拠点展開・予測(D)併用・Fabric でストリーム化 | Azure ML / Fabric | スケール |

---

## 7. 注意点・制約

- **Azure AI Anomaly Detector は新規採用不可（2026/10 廃止）**。既存リソースがあっても移行前提。
- **ラベル不足が最大のボトルネック**。故障は稀少イベントのため、まず異常検知（教師なし）で立ち上げる。
- **データ品質**: サンプリング間隔の不揃い・欠測・時刻ずれは精度を大きく損なう。前処理を最優先。
- **p.u. 正規化を取り込み段で確定**。後工程すべてが正規化済み前提になるよう `PdM.Core` に集約。
- **不均衡データ**: 故障クラスが極端に少ない。AUC/PR-AUC・再現率重視、必要ならリサンプリング/重み付け。
- **クラウドコスト/レイテンシ**: 常時監視はローカル推論（パターンA）が有利。集中管理ならREST（B）。
- **機密/接続性**: 拠点のネットワーク制約があるなら ONNX ローカル推論でオフライン化。
- **警告ゼロ・規約準拠**: 既存リポジトリ規約（.editorconfig / AGENTS.md）に従う。警告抑制が要るときは事前確認。

---

## 8. 参考リンク

- Azure Machine Learning AutoML（時系列予測の設定）: https://learn.microsoft.com/en-us/azure/machine-learning/how-to-auto-train-forecast?view=azureml-api-2
- AutoML ONNX モデルを .NET で推論: https://learn.microsoft.com/en-us/azure/machine-learning/how-to-use-automl-onnx-model-dotnet?view=azureml-api-2
- オンライン エンドポイントを REST で呼ぶ: https://learn.microsoft.com/en-us/azure/machine-learning/how-to-deploy-with-rest?view=azureml-api-2
- ML.NET 時系列異常検知チュートリアル: https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/sales-anomaly-detection
- ML.NET で時系列異常検知（DevBlogs）: https://devblogs.microsoft.com/dotnet/detect-anomalies-time-series-mlnet/
- Anomaly Detector 廃止告知: https://azure.microsoft.com/en-us/updates/ai-services-anomaly-detector-will-be-retired-on-1-october-2026/
- ML.NET サンプル集: https://github.com/dotnet/machinelearning-samples
