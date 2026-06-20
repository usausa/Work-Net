namespace WorkML.TimeGen;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

// 接続設定（エンドポイント/キーは環境変数や User Secrets から渡す。コードに直書きしない）
public sealed class TimeGenOptions
{
    public required Uri Endpoint { get; init; }

    public required string ApiKey { get; init; }

    public string ForecastPath { get; init; } = "/forecast";

    public string AnomalyPath { get; init; } = "/anomaly_detection";
}

// TimeGEN-1（Azure サーバーレス エンドポイント）を呼ぶ最小クライアント。公式 .NET SDK が無いため HttpClient を直接使う。
public sealed class TimeGenClient(HttpClient httpClient, TimeGenOptions options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ForecastResponse> ForecastAsync(ForecastRequest request, CancellationToken ct = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, Combine(options.Endpoint, options.ForecastPath))
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        using var response = await httpClient.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ForecastResponse>(JsonOptions, ct);
        return result ?? throw new InvalidOperationException("空のレスポンスを受信しました。");
    }

    // エンドポイントのベースパスを保持したままパスを結合する（先頭スラッシュによるベースパス置換を防ぐ）
    private static Uri Combine(Uri baseUri, string path)
    {
        var b = baseUri.AbsoluteUri.TrimEnd('/');
        var p = path.TrimStart('/');
        return new Uri($"{b}/{p}");
    }
}
