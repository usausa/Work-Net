namespace WorkML;

using System.Text.Json;

using WorkML.Core;
using WorkML.TimeGen;

public static class Program
{
    public static async Task Main()
    {
        var dataDir = Path.Combine(AppContext.BaseDirectory, "sample-data");
        var devices = CsvLoader.LoadDevices(Path.Combine(dataDir, "devices.csv"))
            .ToDictionary(static d => d.DeviceId);
        var readings = CsvLoader.LoadReadings(Path.Combine(dataDir, "readings.csv"));

        // p.u. 正規化して long 形式 (unique_id, ds, y) に変換
        var points = readings
            .Where(r => devices.ContainsKey(r.DeviceId))
            .Select(r =>
            {
                var spec = devices[r.DeviceId];
                return new SeriesPoint
                {
                    UniqueId = PerUnit.UniqueId(spec, r.ChannelNo),
                    Ds = r.Timestamp,
                    Y = PerUnit.Voltage(r.Value, spec)
                };
            })
            .OrderBy(static p => p.UniqueId)
            .ThenBy(static p => p.Ds)
            .ToList();

        var series = points.Select(static p => p.UniqueId).Distinct().ToList();
        Console.WriteLine($"読み込み: 装置 {devices.Count} 件 / ログ {readings.Count} 点 / 系列点 {points.Count} 件（5分間隔）");
        Console.WriteLine($"系列(unique_id): {string.Join(", ", series)}");

        var request = new ForecastRequest
        {
            Freq = "5min",
            H = 12,                 // 5分 × 12 = 1時間先
            Level = [80, 95],
            Y = points
        };

        // 送信する JSON の確認（Azure 未接続でもここまでは動く）
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
        var sample = new ForecastRequest { Freq = request.Freq, H = request.H, Level = request.Level, Y = [.. points.Take(3)] };
        Console.WriteLine();
        Console.WriteLine("--- TimeGEN へ送るリクエスト(JSON, 先頭3点のみ) ---");
        Console.WriteLine(JsonSerializer.Serialize(sample, jsonOptions));

        // 環境変数が設定されていれば実際に予測を呼び出す
        var endpoint = Environment.GetEnvironmentVariable("TIMEGEN_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("TIMEGEN_APIKEY");
        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine();
            Console.WriteLine("TIMEGEN_ENDPOINT / TIMEGEN_APIKEY が未設定のため、JSON 生成までで終了します。");
            Console.WriteLine("Azure に TimeGEN-1 をデプロイし、両環境変数を設定すると実際に予測を呼び出します。");
            return;
        }

        using var http = new HttpClient();
        var client = new TimeGenClient(http, new TimeGenOptions { Endpoint = new Uri(endpoint), ApiKey = apiKey });
        var result = await client.ForecastAsync(request);

        Console.WriteLine();
        Console.WriteLine($"予測結果: {result.Forecast.Count} 点");
        foreach (var p in result.Forecast.Take(10))
        {
            Console.WriteLine($"  {p.UniqueId} {p.Ds:yyyy-MM-dd HH:mm} -> {p.Value:F3} p.u.");
        }
    }
}
