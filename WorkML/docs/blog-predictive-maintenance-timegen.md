# Azureで予防保守をやろうとしたら、時系列基盤モデル TimeGEN-1 に行き着いた話（.NET実装まで）

> 2026-06-20 / 電力・電圧の保安機器ログから予防保守（予知保全）を、Azure と .NET で実装するまでの記録。

## やりたかったこと

複数拠点に設置された電力・電圧の保安機器がある。各装置はチャンネル数が異なり、基本電圧も 100V / 200V とまちまち。これらのログから**故障の予兆を先読みして保守につなげたい**——いわゆる予防保守（Predictive Maintenance）だ。

最初の想定はシンプルだった。「Azure の AutoML 系サービスを .NET から叩けばいいだろう」。ところが調べ始めると、想定はことごとく覆されていく。

## 第一の壁：Azureの時系列異常検知サービスが軒並み終了

予防保守の時系列異常検知といえば、Azure には定番が2つあった。**Anomaly Detector** と **Metrics Advisor** だ。ところが——

- Azure AI **Anomaly Detector**: **2026年10月1日で廃止**。しかも2023年9月以降、新規リソースは作成不可。
- Azure AI **Metrics Advisor**: 同じく**2026年10月1日で廃止**。

つまり「`Azure.AI.◯◯` という名前の、時系列異常検知の専用 SDK」は**もう新規に採用できない**。最初の選択肢が、調査開始から数分で消えた。

> **学び1**: クラウドの AI サービスは廃止が速い。採用候補は、推奨する前に「今も提供されているか・廃止予定はないか」を必ず最新情報で確認する。学習データや過去の記憶を当てにしない。

## 方針転換：基盤モデルという選択肢

残った現役の選択肢は大きく3つだった。

| 方向 | 中身 | 性格 |
|---|---|---|
| ML.NET | `Microsoft.ML.TimeSeries` 等。.NET ネイティブ | ローカル完結・PoC向き |
| Azure ML **AutoML** | 自分のデータで時系列予測を自動学習 | 高機能だが学習の準備が重い |
| **時系列基盤モデル** | 学習済みモデルを叩くだけ | 立ち上がりが速い |

予防保守の現場では「過去の故障ラベルが十分に無い」ことが多い。自分で学習する AutoML はハードルが高い。そこで注目したのが、**学習不要で使える時系列の基盤モデル（Foundation Model）**だった。

LLM の世界で基盤モデルが当たり前になったように、時系列にも基盤モデルが登場している。2026年時点の主要どころはこのあたり：

- **TimeGEN-1 / TimeGPT**（Nixtla）
- **TimesFM**（Google）
- **Chronos-2**（Amazon）
- **Moirai**（Salesforce）
- **Lag-Llama**（OSS）

この中で、**Azure 上でマネージドに使えて、予測と異常検知の両方が標準機能**なのが TimeGEN-1 だった。

## TimeGEN-1 とは（Azureとの関係）

ひとつ混乱しやすい点がある。TimeGEN-1 は **Nixtla 社製**でMicrosoft純正ではない。では Azure と無関係かというと、そうではない。

**TimeGEN-1 は Azure AI Foundry / Azure Machine Learning のモデルカタログに収録され、サーバーレス API（Models-as-a-Service / 従量課金）としてデプロイできる。** これは「OpenAI 製のモデルを Azure 上で Azure OpenAI として使う」のと同じ構図だ。つまり**Azure ML 系のサービスとして正式に使える**。

特徴：

- **zero-shot**：履歴を投げるだけで予測できる（事前学習が不要）
- **予測と異常検知の両方**が標準
- **外生変数（共変量）対応**：温度や負荷などの外部要因も入力できる
- 課金は per-token の従量課金

> **学び2**: サードパーティ製でも、Azure のモデルカタログ経由なら「Azureのサービス」として課金・認証・運用が統合される。純正かどうかより「どう提供されているか」で判断する。

## 設計の肝：異なる電圧と可変チャンネルをどう扱うか

ここが本件で一番頭を使ったところだ。「拠点ごとにバラバラ」をどう吸収するか。

### 異なる基本電圧 → 標幺値（p.u.）正規化

電力分野の定石、**標幺値（per-unit, p.u.）**を使う。実測値を装置の基準値で割って無次元化する。

```
電圧(p.u.) = 実測電圧 / 基本電圧   例: 198V/200V = 0.99,  99V/100V = 0.99
```

100V 系も 200V 系も「1.0 を中心とした同じスケール」に揃う。異常は「定格からの乖離（例: 0.85 への電圧降下）」として表現でき、現場の感覚とも一致する。

```csharp
public static float Voltage(float measured, DeviceSpec spec) => measured / spec.BaseVoltage;
```

### 可変チャンネル / マルチサイト → 「1チャンネル = 1系列」

TimeGEN（Nixtla）の入力は **`unique_id, ds, y` の long 形式**だ。この `unique_id`（系列ID）が効く。

```
unique_id = "{SiteId}-{DeviceId}-ch{ChannelNo}"   例: site02-dev03-ch1
```

**1チャンネルを1系列として扱う**ので、装置ごとにチャンネル数が違っても破綻しない。複数拠点・複数チャンネルを、1回のリクエストでまとめて投げられる。可変長の悩みが、系列IDの設計だけで消える。

> **学び3**: 「装置ごとにバラバラ」を、固定長ベクトルに押し込もうとすると苦しい。`unique_id` で系列に分解すると素直になる。

## .NETからの実装

TimeGEN-1 に**公式の .NET SDK は無い**（主役は Python の `NixtlaClient`）。なので **`HttpClient` で REST を直接叩く**。

構成はシンプルに、単一のコンソールプロジェクトにフォルダで分けた：

```
WorkML/
  Program.cs         CSV→p.u.正規化→long形式→予測リクエスト
  Core/              ChannelReading / DeviceSpec / PerUnit / CsvLoader
  TimeGen/           TimeGenClient(HttpClient) / TimeGenModels(DTO)
  sample-data/       devices.csv / readings.csv（5分間隔）
```

クライアントの肝はこれだけ：

```csharp
public async Task<ForecastResponse> ForecastAsync(ForecastRequest request, CancellationToken ct = default)
{
    using var message = new HttpRequestMessage(HttpMethod.Post, Combine(options.Endpoint, options.ForecastPath))
    {
        Content = JsonContent.Create(request, options: JsonOptions)
    };
    message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

    using var response = await httpClient.SendAsync(message, ct);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<ForecastResponse>(JsonOptions, ct)
        ?? throw new InvalidOperationException("空のレスポンス");
}
```

エンドポイント URL と API キーは**コードに直書きせず環境変数から**。サンプルデータは指定どおり**5分間隔**にし、1系列だけ終盤に電圧降下（劣化の予兆）を仕込んで、異常が見えるようにした。Azure 未接続でも「リクエスト JSON の生成」までローカルで確認できる作りにしている。

## つまずきと学び

### net9 はもう EOL だった

プロジェクトの初期テンプレートが `net9.0` だったが、確認すると **.NET 9 は STS で、2026年5月12日にサポート終了済み**。一方 **.NET 10 は LTS（2028年11月まで）**。ローカルにも .NET 10 SDK しか入っていなかった。新規プロジェクトを EOL のフレームワークで始める理由はないので、`net10.0` に変更した。

> **学び4**: フレームワークやランタイムの選定も「最新のサポート状況」を確認してから。STS/LTS の区別とEOL日は要チェック。

### ビルド警告は環境起因とコード起因を切り分ける

ビルドで2つの警告が出た。

- **CS1998**（async に await が無い）→ コード側の問題。HTTP 呼び出しの実装で解消。
- **NETSDK1057**（プレビュー版 .NET の通知）→ **環境側**の問題。コードでは直さず、環境で対処する判断に。

警告を一律に潰すのではなく、**原因が環境かコードかを切り分ける**と、無駄な抑制を避けられる。

### 自動化のルールは「盛りすぎない」

作業のルールをプロジェクトの規約ファイルに足す場面があった。最初は9個書いたが、**常時読み込まれるルールが多すぎると、一つひとつへの注意が薄まる**（希釈）。「廃止サービスを推奨しない」のように効果が明確なものに絞り、**3個まで圧縮**した。

> **学び5**: ルール（プロンプト）は足すほど良いわけではない。効果が明確なものだけ残し、状況依存の良識は常時ルールにしない。

## まとめ（Takeaways）

- **予防保守の時系列を Azure でやるなら、専用異常検知サービスはもう終了**。基盤モデル（TimeGEN-1）が現実的な選択肢。
- **TimeGEN-1 は Azure のモデルカタログからサーバーレスで使える**。学習不要で予測＋異常検知。.NET からは `HttpClient` で。
- **異電圧は p.u. 正規化、可変チャンネルは `unique_id` で系列化**。拠点ごとのバラつきは前処理と系列設計で吸収できる。
- **何を採用するにせよ、提供状況・サポート期限を最新情報で確認する**のが、遠回りに見えて一番速い。

---

### 参考リンク
- TimeGEN-1 を Azure ML でデプロイ: <https://learn.microsoft.com/en-us/azure/machine-learning/how-to-deploy-models-timegen-1>
- Anomaly Detector 廃止告知: <https://azure.microsoft.com/en-us/updates/ai-services-anomaly-detector-will-be-retired-on-1-october-2026/>
- .NET サポートライフサイクル: <https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core>
- Nixtla TimeGPT / TimeGEN-1: <https://github.com/Nixtla/nixtla>
